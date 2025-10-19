using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowerBI;

public class Joins
{
    public IDictionary<LabelledTable, string> Aliases { get; } =
        new Dictionary<LabelledTable, string>();

    private Schema Schema => Aliases.Keys.First().Value.Schema;

    private IEnumerable<string> JoinLabels => Aliases.Select(x => x.Key.JoinLabel).Distinct();

    private IEnumerable<LabelledTable> Tables =>
        JoinLabels.SelectMany(j => Schema.Tables.Select(t => LabelledTable.From(j, t))).Distinct();

    public string GetAlias(Table table, string join) => GetAlias(LabelledTable.From(join, table));

    public string GetAlias(LabelledTable table)
    {
        if (!Aliases.TryGetValue(table, out var alias))
        {
            Aliases[table] = alias = $"t{Aliases.Count}";
        }

        return alias;
    }

    public string Aliased(LabelledColumn column, ISqlFormatter sql) =>
        sql.IdentifierPair(
            this.GetAlias(column.Value.Table, column.JoinLabel),
            column.Value.DbName
        );

    private record LabelledArrow(IForeignKey Key, LabelledTable Table, bool Reverse);

    private class Referrers
    {
        public IReadOnlyDictionary<Table, IEnumerable<IForeignKey>> ByTable { get; }

        public Referrers(IEnumerable<Table> tables)
        {
            var byTable = new Dictionary<Table, List<IForeignKey>>();

            foreach (var table in tables)
            {
                var fks = table
                    .Columns.OfType<IForeignKey>()
                    .GroupBy(x => x.To.Table)
                    .Select(x => (x.First(), x.Count() == 1));

                foreach (var (fk, isUnique) in fks)
                {
                    if (!byTable.TryGetValue(fk.To.Table, out var tableReferrers))
                    {
                        byTable[fk.To.Table] = tableReferrers = new List<IForeignKey>();
                    }

                    tableReferrers.Add(fk);
                }
            }

            ByTable = byTable.ToDictionary(x => x.Key, x => x.Value.AsEnumerable());
        }
    }

    private class TableSubset
    {
        private readonly IReadOnlySet<LabelledTable> _tables;
        private readonly Referrers _referrers;
        private readonly IEnumerable<string> _joinLabels;

        public TableSubset(
            IEnumerable<LabelledTable> tables,
            Referrers referrers,
            IEnumerable<string> joinLabels
        )
        {
            _tables = tables.ToHashSet();
            _referrers = referrers;
            _joinLabels = joinLabels;
        }

        private IEnumerable<(IForeignKey Key, Table Table, bool Reverse)> GetArrows(Table table)
        {
            // Forward arrows - FKs from this table to other tables
            foreach (var key in table.Columns.OfType<IForeignKey>())
            {
                yield return (key, key.To.Table, false);
            }

            // Reverse arrows - FKs to this table from other tables
            if (_referrers.ByTable.TryGetValue(table, out var referrersToThisTable))
            {
                foreach (var referrer in referrersToThisTable)
                {
                    yield return (referrer, referrer.Table, true);
                }
            }
        }

        public IEnumerable<LabelledArrow> GetLabelledArrows(LabelledTable table)
        {
            foreach (var arrow in GetArrows(table.Value))
            {
                if (arrow.Table.Conjoint && !table.Value.Conjoint)
                {
                    foreach (var label in _joinLabels)
                    {
                        var toTable = LabelledTable.From(label, arrow.Table);

                        if (_tables.Contains(toTable))
                        {
                            yield return new LabelledArrow(arrow.Key, toTable, arrow.Reverse);
                        }
                    }
                }
                else
                {
                    var toTable = LabelledTable.From(table.JoinLabel, arrow.Table);

                    if (_tables.Contains(toTable))
                    {
                        yield return new LabelledArrow(arrow.Key, toTable, arrow.Reverse);
                    }
                }
            }
        }

        public IReadOnlyList<LabelledTable> GetReachableTablesInJoinOrder(LabelledTable from)
        {
            var visited = new HashSet<LabelledTable>();

            void Recurse(LabelledTable from)
            {
                if (visited.Add(from))
                {
                    foreach (var arrow in GetLabelledArrows(from))
                    {
                        Recurse(arrow.Table);
                    }
                }
            }

            Recurse(from);

            return visited.ToList();
        }

        public IReadOnlyList<LabelledTable> GetReachableTablesMostDistantFirst(LabelledTable from)
        {
            var visited = new Dictionary<LabelledTable, int>();

            void Recurse(LabelledTable from, int depth)
            {
                if (!visited.TryGetValue(from, out int previousDepth) || previousDepth > depth)
                {
                    visited[from] = depth;

                    foreach (var arrow in GetLabelledArrows(from))
                    {
                        Recurse(arrow.Table, depth + 1);
                    }
                }
            }

            Recurse(from, 0);

            return visited.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
        }
    }

    public string ToSql(ISqlFormatter sql, bool fullJoins) => ToSqlAndTables(sql, fullJoins).Sql;

    public (string Sql, IEnumerable<LabelledTable> Tables) ToSqlAndTables(
        ISqlFormatter sql,
        bool fullJoins
    )
    {
        var needed = Aliases.OrderBy(x => x.Value).Select(x => x.Key).ToList();

        var referrers = new Referrers(Schema.Tables);

        var root = needed.First();
        var output = new List<string> { $"from {root.Value.ToSql(sql)} {GetAlias(root)}" };

        bool CanReachAllNeeded(IEnumerable<LabelledTable> reached) =>
            needed.All(n => n == root || reached.Contains(n));

        var tables = new TableSubset(Tables, referrers, JoinLabels);
        var reachable = tables.GetReachableTablesMostDistantFirst(root);

        if (!CanReachAllNeeded(reachable))
        {
            throw new FlowerBIException($"Could not connect tables: {string.Join(",", needed)}");
        }

        for (var repeat = true; repeat; )
        {
            repeat = false;

            foreach (var candidateForRemoval in reachable)
            {
                // No need to try removing a table if we already know it's directly needed
                if (needed.Contains(candidateForRemoval))
                    continue;

                // Try removing table t
                var without = reachable.Where(x => x != candidateForRemoval).ToList();

                var reducedTables = new TableSubset(without, referrers, JoinLabels);
                var reducedReachable = reducedTables.GetReachableTablesMostDistantFirst(root);
                if (CanReachAllNeeded(reducedReachable))
                {
                    tables = reducedTables;
                    reachable = reducedReachable;
                    repeat = true;
                    break;
                }
            }
        }

        reachable = tables.GetReachableTablesInJoinOrder(root);

        // Now add back any associative tables that associate with two or more of our surviving tables
        for (var repeat = true; repeat; )
        {
            repeat = false;

            foreach (
                var candidateForAddition in Tables
                    .Where(x => x.Value.Associative.Count() >= 2)
                    .Except(reachable)
            )
            {
                var expandedTables = new TableSubset(
                    reachable.Append(candidateForAddition),
                    referrers,
                    JoinLabels
                );
                if (
                    expandedTables
                        .GetLabelledArrows(candidateForAddition)
                        .Count(x =>
                            !x.Reverse
                            && candidateForAddition.Value.Associative.Contains(x.Key)
                            && reachable.Contains(x.Table)
                        ) >= 2
                )
                {
                    tables = expandedTables;
                    reachable = expandedTables.GetReachableTablesInJoinOrder(root);
                    repeat = true;
                    break;
                }
            }
        }

        var joinedSoFar = new HashSet<LabelledTable> { root };

        var joinType = fullJoins ? "full join" : "join";

        foreach (var table in reachable.Where(x => x != root))
        {
            var availableArrows = tables.GetLabelledArrows(table);

            var arrowsToAlreadyJoined = availableArrows
                .Where(x => joinedSoFar.Contains(x.Table))
                .ToList();

            var criteria = new List<string>();

            foreach (var arrow in arrowsToAlreadyJoined)
            {
                var (leftColumn, rightColumn) = ((IColumn)arrow.Key, arrow.Key.To.Table.Id);
                if (arrow.Reverse)
                {
                    (leftColumn, rightColumn) = (rightColumn, leftColumn);
                }

                var leftPair = sql.IdentifierPair(GetAlias(table), leftColumn.DbName);
                var rightPair = sql.IdentifierPair(GetAlias(arrow.Table), rightColumn.DbName);

                criteria.Add($"{leftPair} = {rightPair}");
            }

            output.Add(
                $"{joinType} {table.Value.ToSql(sql)} {GetAlias(table)} on {string.Join(" and ", criteria)}"
            );

            joinedSoFar.Add(table);
        }

        return (string.Join(Environment.NewLine, output), joinedSoFar);
    }
}
