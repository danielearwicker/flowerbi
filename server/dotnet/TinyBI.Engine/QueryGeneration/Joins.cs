using System.Collections.Generic;

namespace TinyBI
{
    public class Joins
    {
        public IDictionary<Table, string> Aliases { get; } = new Dictionary<Table, string>();

        public string MainAlias { get; }

        public Table MainTable { get; }

        public Joins(string mainAlias, Table mainTable)
        {
            MainAlias = mainAlias;
            MainTable = mainTable;
        }

        public string this[Table table]
        {
            get
            {
                if (table == MainTable)
                {
                    return MainAlias;
                }

                if (!Aliases.TryGetValue(table, out var alias))
                {
                    Aliases[table] = alias = $"join{Aliases.Count}";
                }

                return alias;
            }
        }
    }
}
