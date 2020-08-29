using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TinyBI
{
    public sealed class Table : Named
    {
        public Schema Schema { get; }

        public IColumn Id { get; private set; }

        private readonly Dictionary<Table, IForeignKey> _keys
                   = new Dictionary<Table, IForeignKey>();

        private readonly Dictionary<string, IColumn> _columns
                   = new Dictionary<string, IColumn>();

        public Table(Schema schema, Type source)
        {
            Schema = schema;

            DbName = source.GetCustomAttributes(false)
                         .OfType<DbTableAttribute>()
                         .Single().Name;

            RefName = source.Name;

            foreach (var member in source.GetFields(BindingFlags.Static|BindingFlags.Public))
            {
                var column = (IColumnInternal)member.GetValue(null);

                column.SetTable(this, member.Name);

                if (column.GetType().GetGenericTypeDefinition() == typeof(PrimaryKey<>))
                {
                    if (Id != null)
                    {
                        throw new InvalidOperationException(
                            $"Table {this} has two primary keys");
                    }

                    Id = column;
                }
                else if (column is IForeignKey key)
                {
                    if (_keys.ContainsKey(key.To.Table))
                    {
                        throw new InvalidOperationException(
                            $"Table {this} already has foreign key to" +
                            $" {key.To.Table}, can't set {key}");
                    }
                    _keys.Add(key.To.Table, key);
                }

                _columns.Add(column.RefName, column);
            }
        }

        public override string ToString() => $"{Schema.RefName}.{RefName}";

        public IEnumerable<IColumn> Columns => _columns.Values;

        public IColumn GetColumn(string refName)
        {
            if (!_columns.TryGetValue(refName, out var column))
            {
                throw new InvalidOperationException(
                    $"No such column {refName} in table {this}");
            }

            return column;
        }

        public IForeignKey GetForeignKeyTo(Table otherTable)
        {
            return _keys.TryGetValue(otherTable, out var foreignKey) ? foreignKey
                : throw new InvalidOperationException($"Table {this} has no foreign key to {otherTable}");
        }

        public string ToSql(ISqlFormatter sql) => sql.IdentifierPair(Schema.DbName, DbName);
    }
}
