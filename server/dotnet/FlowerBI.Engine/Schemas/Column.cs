using System;
using FlowerBI.Yaml;

namespace FlowerBI;

public interface IColumn : INamed
{
    Table Table { get; }

    DataType DataType { get; }

    Type ClrType { get; }

    bool Nullable { get; }

    object ConvertValue(object fromDb);

    string FullName => $"{Table.RefName}.{RefName}";
}

public class Column : Named, IColumn
{
    public Table Table { get; internal set; }

    public DataType DataType { get; }

    public bool Nullable { get; }

    public Type ClrType => GetClrType(DataType, Nullable);

    internal Column(string dbName, string refName, DataType dataType, bool nullable)
    {
        DbName = dbName;
        RefName = refName;
        DataType = dataType;
        Nullable = nullable;
    }

    public override string ToString() => $"{Table}.{RefName}";

    public object ConvertValue(object fromDb) => fromDb;

    internal static Type GetClrType(DataType dataType, bool nullable)
    {
        var baseType = dataType switch
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

        if (nullable && baseType.IsValueType)
        {
            return typeof(Nullable<>).MakeGenericType(baseType);
        }

        return baseType;
    }
}
