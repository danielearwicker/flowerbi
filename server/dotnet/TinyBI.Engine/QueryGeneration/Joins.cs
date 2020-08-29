using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyBI
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

        private class Join
        {
            public IForeignKey Key { get; }
            public IReadOnlyCollection<Join> Joins { get; }

            public Join(IForeignKey key, IReadOnlyCollection<Join> joins)
            {
                Key = key;
                Joins = joins;
            }

            public int Depth => 1 + (Joins.Count == 0 ? 0 : Joins.Max(x => x.Depth));
            public int Count => 1 + Joins.Sum(x => x.Count);

            public void ToSql(ISqlFormatter sql, Joins aliases, List<string> output)
            {
                var table = Key.To.Table.ToSql(sql);
                var alias = aliases[Key.To.Table];
                var left = sql.IdentifierPair(alias, Key.To.Table.Id.DbName);
                var right = sql.IdentifierPair(aliases[Key.Table], Key.DbName);

                output.Add($"join {table} {alias} on {left} = {right}");

                foreach (var join in Joins)
                {
                    join.ToSql(sql, aliases, output);
                }
            }
        }

        private static (IReadOnlyCollection<Join> joins, bool complete) GetJoins(Table root, ISet<Table> needed)
        {
            needed.Remove(root);

            var joins = new List<Join>();

            foreach (var key in root.Columns.OfType<IForeignKey>())
            {
                if (needed.Count == 0) break;

                var oldCount = needed.Count;

                var (nestedJoins, _) = GetJoins(key.To.Table, needed);

                if (needed.Count < oldCount)
                {
                    joins.Add(new Join(key, nestedJoins));
                }
            }

            return (joins, needed.Count == 0);
        }

        public string ToSql(ISqlFormatter sql)
        {
            var needed = Aliases.Keys;
            var schema = needed.First().Schema;

            var (main, joined) = (
                from table in schema.Tables
                let j = GetJoins(table, needed.ToHashSet())
                where j.complete
                orderby j.joins.Count == 0 ? 0 : j.joins.Max(x => x.Depth),
                        j.joins.Sum(x => x.Count)
                select (table, j.joins)
            ).FirstOrDefault();

            if (main == null)
            {
                var names = string.Join(",", needed.Select(x => x.RefName));
                throw new InvalidOperationException($"Could not make joins between tables {names}");
            }

            var output = new List<string> { $"from {main.ToSql(sql)} {this[main]}" };

            foreach (var join in joined)
            {
                join.ToSql(sql, this, output);
            }

            return string.Join(Environment.NewLine, output);
        }
    }
}
