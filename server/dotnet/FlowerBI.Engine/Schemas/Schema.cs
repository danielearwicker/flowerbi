using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowerBI
{
    public record LabelledColumn(string JoinLabel, IColumn Value)
    {
        public static LabelledColumn From(string joinLabel, IColumn column)
        {
            if (!column.Table.Conjoint)
            {
                throw new InvalidOperationException($"Table {column.Table.RefName} is not conjoint");
            }

            return new LabelledColumn(joinLabel, column);
        }

        public override string ToString()
            => string.IsNullOrWhiteSpace(JoinLabel) ? Value.RefName : $"{Value.RefName}@{JoinLabel}";
    }

    public record LabelledTable(string JoinLabel, Table Value)
    {
        public static LabelledTable From(string joinLabel, Table value)
            => new LabelledTable(value.Conjoint ? joinLabel : null, value);

        public override string ToString()
            => string.IsNullOrWhiteSpace(JoinLabel) ? Value.RefName : $"{Value.RefName}@{JoinLabel}";
    }

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

        public IList<LabelledColumn> Load(IEnumerable<string> columns)
            => columns?.Select(GetColumn).ToList() ?? new List<LabelledColumn>();

        public IEnumerable<Table> Tables => _tables.Values;

        public LabelledColumn GetColumn(string labelledName)
        {
            var at = labelledName.IndexOf('@');
            var (name, label) = at == -1 ? (labelledName, null) : (labelledName[0..at], labelledName[(at + 1)..]);

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

            return new LabelledColumn(label, table.GetColumn(parts[1]));
        } 
    }
}
