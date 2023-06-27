using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using FlowerBI.Yaml;

namespace FlowerBI.Conversion;

public static class Reflection
{
    static (DataType, bool) DataTypeFromClr(Type clrType)
    {
        var nonNullable = Nullable.GetUnderlyingType(clrType);
        clrType = nonNullable ?? clrType;

        var dataType =
            clrType == typeof(bool) ? DataType.Bool :
            clrType == typeof(byte) ? DataType.Byte :
            clrType == typeof(short) ? DataType.Short :
            clrType == typeof(int) ? DataType.Int :
            clrType == typeof(long) ? DataType.Long :
            clrType == typeof(float) ? DataType.Float :
            clrType == typeof(double) ? DataType.Double :
            clrType == typeof(decimal) ? DataType.Decimal :
            clrType == typeof(string) ? DataType.String :
            clrType == typeof(DateTime) ? DataType.DateTime :
            throw new InvalidOperationException($"Unsupported data type: {clrType}");

        return (dataType, nonNullable != null);
    }

    public static ResolvedSchema ToSchema(string path, string schemaClass, TextWriter console)
        => ResolvedSchema.Resolve(ToYaml(path, schemaClass, console));

    public static void ToYaml(string path, string schemaClass, string yamlFile, TextWriter console)
    {
        using var writer = new WriteIfDifferent(yamlFile, console);

        var yaml = ToYaml(path, schemaClass, writer.Console);

        Serialize(yaml, writer.Output);
    }

    public static void Serialize(YamlSchema yaml, TextWriter yamlWriter)
    {            
        yamlWriter.WriteLine($"schema: {yaml.schema}");

        if (yaml.name != yaml.schema)
        {
            yamlWriter.WriteLine($"name: {yaml.name}");
        }

        yamlWriter.WriteLine("tables:");

        var tableWriter = new IndentedWriter(yamlWriter);

        foreach (var (tableKey, table) in yaml.tables)
        {
            tableWriter.WriteLine();
            tableWriter.WriteLine($"{tableKey}:");

            var tablePropWriter = new IndentedWriter(tableWriter, 4);

            if (table.name != tableKey)
            {
                tablePropWriter.WriteLine($"name: {table.name}");
            }

            if (table.conjoint)
            {
                tablePropWriter.WriteLine($"conjoint: true");
            }

            if (table.associative != null && table.associative.Length > 0)
            {
                tablePropWriter.WriteLine($"associative: [{string.Join(", ", table.associative)}]");
            }

            void WriteColumns(IDictionary<string, string[]> columns)
            {
                var columnWriter = new IndentedWriter(tablePropWriter);
                foreach (var column in columns)
                {
                    var list = string.Join(", ", column.Value);
                    columnWriter.WriteLine($"{column.Key}: [{list}]");
                }
            }

            if (table.id != null)
            {
                tablePropWriter.WriteLine("id:");
                WriteColumns(table.id);
            }

            tablePropWriter.WriteLine("columns:");
            WriteColumns(table.columns);
        }
    }

    static YamlSchema ToYaml(string path, string schemaClass, TextWriter console)
    {
        path = Path.GetFullPath(path);

        console.WriteLine($"Reading assembly {path}");

        var schemaType = AssemblyLoadContext.Default.LoadFromAssemblyPath(path).GetType(schemaClass);
        if (schemaType == null)
        {
            throw new InvalidOperationException($"No such type {schemaType} in assembly");                
        }

        console.WriteLine($"Reading type {schemaType.FullName}");
        return ToYaml(schemaType);
    }

    public static YamlSchema ToYaml(Type schemaType)
        => ToYaml(new Schema(schemaType));

    public static YamlSchema ToYaml(Schema schema)
    {
        Dictionary<string, string[]> GetColumns(IEnumerable<IColumn> columns) => columns.ToDictionary(
            column => column.RefName, 
            column => 
            {                
                var (dt, nullable) = DataTypeFromClr(column.ClrType);
                var typeOrTable = (column is IForeignKey fk ? fk.To.Table.RefName : dt.ToString().ToLowerInvariant())
                                    + (nullable ? "?" : string.Empty);
                return column.DbName == column.RefName
                    ? new[] { typeOrTable }
                    : new[] { typeOrTable, column.DbName };
            });

        return new YamlSchema
        {
            schema = schema.RefName,
            name = schema.DbName,
            tables = schema.Tables.ToDictionary(t => t.RefName, t => new YamlTable
            {
                name = t.DbName,
                conjoint = t.Conjoint,
                id = t.Id != null ? GetColumns(new[] { t.Id }) : null,
                columns = GetColumns(t.Columns.Where(x => x != t.Id)),
                associative = t.Associative.Select(x => x.RefName).ToArray()
            })
        };
    }
}
