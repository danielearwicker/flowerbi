using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FlowerBI;

public sealed class Table : Named
{
    public Schema Schema { get; }

    public IColumn Id { get; private set; }

    public bool Conjoint { get; private set; }

    private readonly Dictionary<Table, IForeignKey> _keys = new();

    private readonly Dictionary<string, IColumn> _columns = new();

    private readonly List<IColumn> _associative = new();

    public Table(Schema schema, Type source)
    {
        Schema = schema;

        var attr = source.GetCustomAttributes(false).OfType<DbTableAttribute>().Single();

        DbName = attr.Name;
        Conjoint = attr.Conjoint;

        RefName = source.Name;

        foreach (var member in source.GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            var column = (IColumnInternal)member.GetValue(null);

            column.SetTable(this, member.Name);

            var columnTypeDef = column.GetType().GetGenericTypeDefinition();
            if (
                columnTypeDef == typeof(PrimaryKey<>)
                || columnTypeDef == typeof(PrimaryForeignKey<>)
            )
            {
                if (Id != null)
                {
                    throw new FlowerBIException($"Table {this} has two primary keys");
                }

                Id = column;
            }

            if (column is IForeignKey key)
            {
                if (_keys.ContainsKey(key.To.Table))
                {
                    throw new FlowerBIException(
                        $"Table {this} already has foreign key to"
                            + $" {key.To.Table}, can't set {key}"
                    );
                }
                _keys.Add(key.To.Table, key);
            }

            if (member.GetCustomAttribute<DbAssociativeAttribute>() != null)
            {
                _associative.Add(column);
            }

            _columns.Add(column.RefName, column);
        }
    }

    public override string ToString() => $"{Schema.RefName}.{RefName}";

    public IEnumerable<IColumn> Columns => _columns.Values;

    public IEnumerable<IColumn> Associative => _associative;

    public IColumn GetColumn(string refName)
    {
        if (!_columns.TryGetValue(refName, out var column))
        {
            throw new FlowerBIException($"No such column {refName} in table {this}");
        }

        return column;
    }

    public IForeignKey GetForeignKeyTo(Table otherTable)
    {
        return _keys.TryGetValue(otherTable, out var foreignKey)
            ? foreignKey
            : throw new FlowerBIException($"Table {this} has no foreign key to {otherTable}");
    }

    public string ToSql(ISqlFormatter sql) => sql.IdentifierPair(Schema.DbName, DbName);
}
