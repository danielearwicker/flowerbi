using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlowerBI
{
    public class Joins
    {
        public IDictionary<LabelledTable, string> Aliases { get; } = new Dictionary<LabelledTable, string>();

        private Schema Schema => Aliases.Keys.First().Value.Schema;

        private IEnumerable<string> JoinLabels => Aliases.Select(x => x.Key.JoinLabel).Distinct();

        private IEnumerable<LabelledTable> Tables => JoinLabels
                .SelectMany(j => Schema.Tables.Select(t => LabelledTable.From(j, t)))
                .Distinct();

        public string GetAlias(Table table, string join) => GetAlias(LabelledTable.From(join, table));

        public string GetAlias(LabelledTable table)
        {
            if (!Aliases.TryGetValue(table, out var alias))
            {
                var suffix = table.JoinLabel == null ? string.Empty : $"_{table.JoinLabel}";
                Aliases[table] = alias = $"tbl{Aliases.Count}{suffix}";
            }

            return alias;
        }

        public string Aliased(LabelledColumn column, ISqlFormatter sql)
            => sql.IdentifierPair(this.GetAlias(column.Value.Table, column.JoinLabel), column.Value.DbName);

        private record Join(IForeignKey Key, string JoinLabel, IReadOnlyCollection<Join> Joins, bool Reverse)
        {
            public int Depth => 1 + (Joins.Count == 0 ? 0 : Joins.Max(x => x.Depth));
            public int Count => 1 + Joins.Sum(x => x.Count);
            public int Reversed => (Reverse ? 1 : 0) + Joins.Sum(x => x.Reversed);

            public IEnumerable<(string Description, int Level)> Describe(int level)
            {
                var arrow = Reverse ? "<--" : "-->";
                yield return ($"{arrow} {Key.Table.RefName}.{Key.RefName}", level);

                foreach (var join in Joins)
                {
                    foreach (var item in join.Describe(level + 1))
                    {
                        yield return item;
                    }
                }
            }

            public void ToSql(ISqlFormatter sql, Joins aliases, List<string> output)
            {                
                var (leftTable, rightTable) = (Key.To.Table, Key.Table);
                var (leftKey, rightKey) = (Key.To.Table.Id, (IColumn)Key);

                if (Reverse)
                {
                    (leftTable, rightTable) = (rightTable, leftTable);
                    (leftKey, rightKey) = (rightKey, leftKey);
                }

                var left = sql.IdentifierPair(aliases.GetAlias(leftTable, JoinLabel), leftKey.DbName);
                var right = sql.IdentifierPair(aliases.GetAlias(rightTable, JoinLabel), rightKey.DbName);

                output.Add($"join {leftTable.ToSql(sql)} {aliases.GetAlias(leftTable, JoinLabel)} on {left} = {right}");

                foreach (var join in Joins)
                {
                    join.ToSql(sql, aliases, output);
                }
            }
        }

        private record JoinTree(LabelledTable Root, IReadOnlyCollection<Join> Joins, bool Complete)
        {
            public int Depth => Joins.Count == 0 ? 0 : Joins.Max(x => x.Depth);

            public int Count => Joins.Sum(x => x.Count);

            public int Reversed => Joins.Sum(x => x.Reversed);

            public IEnumerable<string> ToSqlLines(ISqlFormatter sql, Joins aliases)
            {
                var output = new List<string> { $"from {Root.Value.ToSql(sql)} {aliases.GetAlias(Root)}" };

                foreach (var join in Joins)
                {
                    join.ToSql(sql, aliases, output);
                }

                return output;
            }

            public string ToSql(ISqlFormatter sql, Joins aliases)
                => string.Join(Environment.NewLine, ToSqlLines(sql, aliases));
        }
        
        // Generates the arrows from and to a table
        private IEnumerable<(IForeignKey Key, Table Table, bool Reverse)> GetArrows(Table table)
        {
            // Forward arrows - FKs from this table to other tables
            foreach (var key in table.Columns.OfType<IForeignKey>())
            {
                yield return (key, key.To.Table, false);
            }

            // Reverse arrows - FKs to this table from other tables
            if (_referrers.TryGetValue(table, out var referrersToThisTable))
            {
                foreach (var referrer in referrersToThisTable)
                {
                    yield return (referrer, referrer.Table, true);
                }
            }
        }

        private IEnumerable<(IForeignKey Key, LabelledTable Table, bool Reverse)> GetLabelledArrows(LabelledTable table)
        {
            foreach (var arrow in GetArrows(table.Value))
            {
                if (arrow.Table.Conjoint && !table.Value.Conjoint)
                {
                    foreach (var label in JoinLabels)
                    {
                        yield return (arrow.Key, LabelledTable.From(label, arrow.Table), arrow.Reverse);
                    }
                }
                else
                {
                    yield return (arrow.Key, LabelledTable.From(table.JoinLabel, arrow.Table), arrow.Reverse);
                }            
            }
        }

        private static string Indent(int level) => new string(' ', level * 4);

        // Generates a tree of joins starting from a specified table and following available arrows
        private JoinTree GetJoins(
            TextWriter log, 
            int logIndent, 
            LabelledTable root, 
            ISet<LabelledTable> needed, 
            ISet<LabelledTable> visited)
        {
            var indent = Indent(logIndent);

            if (!visited.Add(root))
            {
                log.WriteLine($"{indent}Already visited table {root}");
                return null;
            }

            if (needed.Remove(root))
            {
                log.WriteLine($"{indent}Found required table: {root}");
            }

            var joins = new List<Join>();

            foreach (var (key, table, reverse) in GetLabelledArrows(root))
            {
                if (needed.Count == 0) break;

                var oldCount = needed.Count;

                var suffix = table.JoinLabel == null ? string.Empty : $" @{table.JoinLabel}";

                var direction = reverse ? "reverse" : "forward";
                log.WriteLine($"{indent}Following {direction} arrow {key.RefName} -> {key.To.RefName}{suffix}");

                var nestedJoins = GetJoins(log, logIndent + 1, table, needed, visited);

                if (nestedJoins != null && needed.Count < oldCount)
                {
                    log.WriteLine($"{indent}Found helpful {direction} arrow {key.RefName} -> {key.To.RefName}{suffix}");
                    if (table.Value.Conjoint && table.JoinLabel == null)
                    {
                        log.WriteLine($"WARNING ------ that's wring");
                    }

                    joins.Add(new Join(key, table.JoinLabel, nestedJoins.Joins, reverse));
                }
            }

            var result = new JoinTree(root, joins, needed.Count == 0);

            log.WriteLine($"{indent}Produced tree for {root}, depth {result.Depth}, count {result.Count}:");

            foreach (var join in result.Joins.SelectMany(j => j.Describe(0)))
            {
                log.WriteLine($"{Indent(logIndent + join.Level)}{join.Description}");
            }

            if (needed.Count == 0)
            {
                log.WriteLine($"{indent}No tables left to find");
            }
            else
            {
                log.WriteLine($"{indent}These tables are still missing: {string.Join(", ", needed)}");
            }

            return result;
        }

        // Builds a lookup of FKs pointing to each table
        private IReadOnlyDictionary<Table, IEnumerable<IForeignKey>> _referrers;

        private void BuildReferrersByTable(TextWriter log)
        {
            var referrers = new Dictionary<Table, List<IForeignKey>>();

            foreach (var table in Schema.Tables)
            {
                var fks = table.Columns
                    .OfType<IForeignKey>()
                    .GroupBy(x => x.To.Table)                    
                    .Select(x => (x.First(), x.Count() == 1));

                foreach (var (fk, isUnique) in fks)
                {
                    if (!isUnique)
                    {
                        log.WriteLine($"{fk} is not the only foreign key from {fk.Table} to {fk.To.Table}, so cannot be used in reverse join resolution");
                    }
                    else 
                    {
                        if (!referrers.TryGetValue(fk.To.Table, out var tableReferrers))
                        {
                            referrers[fk.To.Table] = tableReferrers = new List<IForeignKey>();
                        }

                        tableReferrers.Add(fk);
                    }
                }
            }

            _referrers = referrers.ToDictionary(x => x.Key, x => x.Value.AsEnumerable());
        }

        private JoinTree GetBestTree(TextWriter log, IEnumerable<LabelledTable> tables,  IEnumerable<LabelledTable> needed)
            => tables
                .Select(t => 
                {
                    log.WriteLine($"===== Starting from table {t} =====");
                    return GetJoins(log, 0, t, needed.ToHashSet(), new HashSet<LabelledTable>());
                })
                .Where(x => x.Complete)
                .OrderBy(x => x.Reversed).ThenBy(x => x.Depth).ThenBy(x => x.Count)
                .FirstOrDefault();

        public string ToSql(ISqlFormatter sql)
        {
            var needed = Aliases.Keys.ToList();

            var log = new StringWriter();
            log.WriteLine($"Trying to connect tables: {string.Join(",", needed)}");

            BuildReferrersByTable(log);

            // If using join labels, just search from the required tables for simplicity
            var tables = JoinLabels.Any(x => x != null) ? needed : Tables;

            var tree = GetBestTree(log, tables, needed.ToHashSet());
            if (tree == null)
            {
                throw new InvalidOperationException(log.ToString());
            }

            var output = new List<string> { $"from {tree.Root.Value.ToSql(sql)} {GetAlias(tree.Root)}" };

            foreach (var join in tree.Joins)
            {
                join.ToSql(sql, this, output);
            }

            return string.Join(Environment.NewLine, output);
        }
    }
}
