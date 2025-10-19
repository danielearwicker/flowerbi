using System.Collections.Generic;
using System.Linq;
using FlowerBI.Yaml;

namespace FlowerBI;

public record LabelledColumn(string JoinLabel, IColumn Value)
{
    public static LabelledColumn From(string joinLabel, IColumn column)
    {
        if (!column.Table.Conjoint)
        {
            throw new FlowerBIException($"Table {column.Table.RefName} is not conjoint");
        }

        return new LabelledColumn(joinLabel, column);
    }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(JoinLabel) ? Value.RefName : $"{Value.RefName}@{JoinLabel}";
}

public record LabelledTable(string JoinLabel, Table Value)
{
    public static LabelledTable From(string joinLabel, Table value) =>
        new(value.Conjoint ? joinLabel : null, value);

    public override string ToString() =>
        string.IsNullOrWhiteSpace(JoinLabel) ? Value.RefName : $"{Value.RefName}@{JoinLabel}";
}

public sealed class Schema : Named
{
    private readonly Dictionary<string, Table> _tables = [];

    // Keep the original constructor for backward compatibility
    public Schema(ResolvedSchema schema)
    {
        DbName = schema.NameInDb;
        RefName = schema.Name;

        foreach (var table in schema.Tables)
        {
            _tables[table.Name] = new Table(this, table);
        }

        foreach (var table in _tables.Values)
        {
            table.BindDynamicForeignKeys();
        }
    }

    // Add static factory method that uses Jint
    public static Schema FromYaml(string yamlText, string bundlePath = null)
    {
        // Use the existing ResolvedSchema.Resolve for now to maintain compatibility
        // TODO: Replace with pure Jint implementation in future version
        return new Schema(ResolvedSchema.Resolve(yamlText));
    }

    public IList<LabelledColumn> Load(IEnumerable<string> columns) =>
        columns?.Select(GetColumn).ToList() ?? [];

    public IEnumerable<Table> Tables => _tables.Values;

    public Table GetTable(string name) =>
        _tables.TryGetValue(name, out var table)
            ? table
            : throw new FlowerBIException($"No such table {name}");

    public LabelledColumn GetColumn(string labelledName)
    {
        var at = labelledName.IndexOf('@');
        var (name, label) =
            at == -1 ? (labelledName, null) : (labelledName[0..at], labelledName[(at + 1)..]);

        var parts = name.Split(".");
        if (parts.Length != 2)
        {
            throw new FlowerBIException("Column names must be of the form Table.Column");
        }

        return new LabelledColumn(label, GetTable(parts[0]).GetColumn(parts[1]));
    }
}
