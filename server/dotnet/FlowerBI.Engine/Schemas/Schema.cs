using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowerBI
{
    public sealed class Schema : Named
    {
        private Dictionary<string, Table> _tables
            = new Dictionary<string, Table>();

        public Schema(Type source)
        {
            DbName = source.GetCustomAttributes(false)
                         .OfType<DbSchemaAttribute>()
                         .Single().Name;

            RefName = source.Name;

            foreach (var tableClass in source.GetNestedTypes())
            {
                var table = new Table(this, tableClass);
                _tables[table.RefName] = table;
            }
        }

        public IList<IColumn> Load(IEnumerable<string> columns)
            => columns?.Select(GetColumn).ToList() ?? new List<IColumn>();

        public IEnumerable<Table> Tables => _tables.Values;

        public IColumn GetColumn(string name)
        {
            if (name == null)
            {
                return null;
            }

            var parts = name.Split(".");
            if (parts.Length != 2)
            {
                throw new InvalidOperationException(
                    "Column names must be of the form Table.Column");
            }

            if (!_tables.TryGetValue(parts[0], out var table))
            {
                throw new InvalidOperationException(
                    $"No such table {parts[0]}");
            }

            return table.GetColumn(parts[1]);
        }
    }
}
