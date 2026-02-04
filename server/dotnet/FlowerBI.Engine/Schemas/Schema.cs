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
        new LabelledTable(value.Conjoint ? joinLabel : null, value);

    public override string ToString() =>
        string.IsNullOrWhiteSpace(JoinLabel) ? Value.RefName : $"{Value.RefName}@{JoinLabel}";
}

public sealed class Schema : Named
{
    private readonly Dictionary<string, Table> _tables = new();

    private Schema(string refName, string dbName)
    {
        RefName = refName;
        DbName = dbName;
    }

    public static Schema FromYaml(string yamlContent)
    {
        var resolved = ResolvedSchema.Resolve(yamlContent);
        return FromResolved(resolved);
    }

    internal static Schema FromResolved(ResolvedSchema resolved)
    {
        var schema = new Schema(resolved.Name, resolved.NameInDb);

        // Build lookup from ResolvedColumn to the runtime IColumn we create
        // Use reference equality since ResolvedColumn/ResolvedTable have circular references
        // that break the auto-generated GetHashCode/Equals
        var columnMap = new Dictionary<ResolvedColumn, IColumn>(ReferenceEqualityComparer.Instance);

        // First pass: create all tables and columns (without FK targets)
        foreach (var rt in resolved.Tables)
        {
            var table = new Table(schema, rt, columnMap);
            schema._tables[table.RefName] = table;
        }

        // Second pass: wire up FK targets
        foreach (var rt in resolved.Tables)
        {
            var table = schema._tables[rt.Name];
            table.ResolveTargets(rt, columnMap);
        }

        return schema;
    }

    public IList<LabelledColumn> Load(IEnumerable<string> columns) =>
        columns?.Select(GetColumn).ToList() ?? new List<LabelledColumn>();

    public IEnumerable<Table> Tables => _tables.Values;

    public Table GetTable(string refName)
    {
        if (!_tables.TryGetValue(refName, out var table))
        {
            throw new FlowerBIException($"No such table {refName}");
        }

        return table;
    }

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

        if (!_tables.TryGetValue(parts[0], out var table))
        {
            throw new FlowerBIException($"No such table {parts[0]}");
        }

        return new LabelledColumn(label, table.GetColumn(parts[1]));
    }
}
