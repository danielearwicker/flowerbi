using System;
using System.Collections.Generic;
using System.Linq;
using FlowerBI.Yaml;

namespace FlowerBI;

public sealed class Table : Named
{
    public Schema Schema { get; }

    public IColumn Id { get; private set; }

    public bool Conjoint { get; private set; }

    private readonly Dictionary<Table, IForeignKey> _keys = [];

    private readonly Dictionary<string, IColumn> _columns = [];

    private readonly List<IColumn> _associative = [];

    public class DynamicColumn : Named, IColumn
    {
        public Table Table { get; }
        public Type ClrType { get; }

        public DynamicColumn(Table table, string dbName, string refName, Type clrType)
        {
            Table = table;
            DbName = dbName;
            RefName = refName;
            ClrType = clrType;
        }

        public object ConvertValue(object fromDb) => fromDb;

        public override string ToString() => $"{Table}.{RefName}";
    }

    public class DynamicForeignKey(
        Table table,
        string dbname,
        string refName,
        Type clrType,
        ResolvedColumn target
    ) : DynamicColumn(table, dbname, refName, clrType), IForeignKey
    {
        public IColumn To { get; private set; }

        public void Bind(Schema schema)
        {
            To = schema.GetColumn($"{target.Table.Name}.{target.Name}").Value;
        }
    }

    private static Type ClrType(DataType dataType, bool nullable)
    {
        var clrType = dataType switch
        {
            DataType.Bool => typeof(bool),
            DataType.Byte => typeof(byte),
            DataType.Short => typeof(short),
            DataType.Int => typeof(int),
            DataType.Long => typeof(long),
            DataType.Float => typeof(float),
            DataType.Double => typeof(double),
            DataType.Decimal => typeof(decimal),
            DataType.String => typeof(string),
            DataType.DateTime => typeof(DateTime),
            _ => throw new FlowerBIException($"Unsupported data type: {dataType}"),
        };

        return !nullable || !clrType.IsValueType
            ? clrType
            : typeof(Nullable<>).MakeGenericType(clrType);
    }

    public Table(Schema schema, ResolvedTable table)
    {
        Schema = schema;

        DbName = table.NameInDb;
        RefName = table.Name;
        Conjoint = table.conjoint;

        foreach (var column in table.Columns.Append(table.IdColumn).Where(c => c != null))
        {
            var clrType = ClrType(column.DataType, column.Nullable);

            IColumn allocated =
                column.Target != null
                    ? new DynamicForeignKey(
                        this,
                        column.NameInDb,
                        column.Name,
                        clrType,
                        column.Target
                    )
                    : new DynamicColumn(this, column.NameInDb, column.Name, clrType);

            if (table.Associative != null && table.Associative.Any(a => a.Name == column.Name))
            {
                _associative.Add(allocated);
            }

            _columns.Add(allocated.RefName, allocated);

            if (column == table.IdColumn)
            {
                Id = allocated;
            }
        }
    }

    public void BindDynamicForeignKeys()
    {
        foreach (var foreignKey in _columns.Values.Append(Id).OfType<DynamicForeignKey>())
        {
            foreignKey.Bind(Schema);

            _keys.Add(foreignKey.To.Table, foreignKey);
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
