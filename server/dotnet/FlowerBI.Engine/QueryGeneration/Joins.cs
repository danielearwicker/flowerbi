using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlowerBI
{
    public class Joins
    {
        public IDictionary<Table, string> Aliases { get; } = new Dictionary<Table, string>();

        public string this[Table table]
        {
            get
            {
                if (!Aliases.TryGetValue(table, out var alias))
                {
                    Aliases[table] = alias = $"tbl{Aliases.Count}";
                }

                return alias;
            }
        }

        public string Aliased(IColumn column, ISqlFormatter sql)
            => sql.IdentifierPair(this[column.Table], column.DbName);

        private record Join(IForeignKey Key, IReadOnlyCollection<Join> Joins, bool Reverse)
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

                var left = sql.IdentifierPair(aliases[leftTable], leftKey.DbName);
                var right = sql.IdentifierPair(aliases[rightTable], rightKey.DbName);

                output.Add($"join {leftTable.ToSql(sql)} {aliases[leftTable]} on {left} = {right}");

                foreach (var join in Joins)
                {
                    join.ToSql(sql, aliases, output);
                }
            }
        }

        private record JoinTree(Table Root, IReadOnlyCollection<Join> Joins, bool Complete)
        {
            public int Depth => Joins.Count == 0 ? 0 : Joins.Max(x => x.Depth);

            public int Count => Joins.Sum(x => x.Count);

            public int Reversed => Joins.Sum(x => x.Reversed);

            public IEnumerable<string> ToSqlLines(ISqlFormatter sql, Joins aliases)
            {
                var output = new List<string> { $"from {Root.ToSql(sql)} {aliases[Root]}" };

                foreach (var join in Joins)
                {
                    join.ToSql(sql, aliases, output);
                }

                return output;
            }

            public string ToSql(ISqlFormatter sql, Joins aliases)
                => string.Join(Environment.NewLine, ToSqlLines(sql, aliases));
        }
        
        // Generates the arrows from (and optionally to) a table
        private static IEnumerable<(IForeignKey Key, Table Table, bool Reverse)> GetArrows(Table table, IReadOnlyDictionary<Table, IEnumerable<IForeignKey>> referrers)
        {
            // Forward arrows - FKs from this table to other tables
            foreach (var key in table.Columns.OfType<IForeignKey>())
            {
                yield return (key, key.To.Table, false);
            }

            // Reverse arrows (if available) - FKs to this table from other tables
            if (referrers != null && referrers.TryGetValue(table, out var referrersToThisTable))
            {
                foreach (var referrer in referrersToThisTable)
                {
                    yield return (referrer, referrer.Table, true);
                }
            }
        }

        private static string Indent(int level) => new string(' ', level * 4);

        // Generates a tree of joins starting from a specified table and following available arrows
        private static JoinTree GetJoins(TextWriter log, int logIndent, Table root, ISet<Table> needed, ISet<Table> visited, IReadOnlyDictionary<Table, IEnumerable<IForeignKey>> referrers = null)
        {
            var indent = Indent(logIndent);

            if (!visited.Add(root))
            {
                log.WriteLine($"{indent}Already visited table {root.RefName}");
                return null;
            }

            if (needed.Remove(root))
            {
                log.WriteLine($"{indent}Found required table: {root.RefName}");
            }

            var joins = new List<Join>();

            foreach (var (key, table, reverse) in GetArrows(root, referrers))
            {
                if (needed.Count == 0) break;

                var oldCount = needed.Count;

                var direction = reverse ? "reverse" : "forward";
                log.WriteLine($"{indent}Following {direction} arrow {key.RefName} -> {key.To.RefName}");

                var nestedJoins = GetJoins(log, logIndent + 1, table, needed, visited, referrers);

                if (nestedJoins != null && needed.Count < oldCount)
                {
                    log.WriteLine($"{indent}Found helpful {direction} arrow {key.RefName} -> {key.To.RefName}");
                    joins.Add(new Join(key, nestedJoins.Joins, reverse));
                }
            }

            var result = new JoinTree(root, joins, needed.Count == 0);

            log.WriteLine($"{indent}Produced tree for {root.RefName}, depth {result.Depth}, count {result.Count}:");

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
                log.WriteLine($"{indent}These tables are still missing: {string.Join(", ", needed.Select(x => x.RefName))}");
            }

            return result;
        }

        // Builds a lookup of FKs pointing to each table
        private IReadOnlyDictionary<Table, IEnumerable<IForeignKey>> GetReferrersByTable(TextWriter log)
        {
            var referrers = new Dictionary<Table, List<IForeignKey>>();

            foreach (var table in Aliases.Keys.First().Schema.Tables)
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

            return referrers.ToDictionary(x => x.Key, x => x.Value.AsEnumerable());
        }

        private JoinTree GetBestTree(
                TextWriter log, 
                IEnumerable<Table> tables, 
                IEnumerable<Table> needed, 
                IReadOnlyDictionary<Table, IEnumerable<IForeignKey>> referrers)
            => tables
                .Select(t => GetJoins(log, 0, t, needed.ToHashSet(), new HashSet<Table>(), referrers))
                .Where(x => x.Complete)
                .OrderBy(x => x.Reversed).ThenBy(x => x.Depth).ThenBy(x => x.Count)
                .FirstOrDefault();

        public string ToSql(ISqlFormatter sql)
        {
            var needed = Aliases.Keys.ToList();
            var schema = needed.First().Schema;

            var log = new StringWriter();
            log.WriteLine($"Trying to connect tables: {string.Join(",", needed.Select(x => x.RefName))}");

            var referrers = GetReferrersByTable(log);
            
            var tree = GetBestTree(log, schema.Tables, needed.ToHashSet(), referrers);
            if (tree == null)
            {                    
                throw new InvalidOperationException(log.ToString());
            }

            var output = new List<string> { $"from {tree.Root.ToSql(sql)} {this[tree.Root]}" };

            foreach (var join in tree.Joins)
            {
                join.ToSql(sql, this, output);
            }

            return string.Join(Environment.NewLine, output);
        }
    }
}
