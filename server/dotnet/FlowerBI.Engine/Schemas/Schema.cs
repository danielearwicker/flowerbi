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

    private readonly Dictionary<string, Topic> _topics = new();

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

        // Build Topic objects (without their See lists yet — resolved in the next pass).
        foreach (var rtopic in resolved.Topics)
        {
            schema._topics[rtopic.Name] = new Topic(rtopic.Name, rtopic.Doc);
        }

        // Resolve every See string list into IDocumented references. Done last so that all
        // tables, columns, and topics exist when we look up references.
        foreach (var rt in resolved.Tables)
        {
            var table = schema._tables[rt.Name];
            table.See = schema.ResolveSeeList(rt.See, $"table {rt.Name}");

            if (rt.IdColumn != null)
            {
                var col = (Column)columnMap[rt.IdColumn];
                col.See = schema.ResolveSeeList(
                    rt.IdColumn.See,
                    $"column {rt.Name}.{rt.IdColumn.Name}"
                );
            }
            foreach (var rc in rt.Columns)
            {
                var col = (Column)columnMap[rc];
                col.See = schema.ResolveSeeList(rc.See, $"column {rt.Name}.{rc.Name}");
            }
        }
        foreach (var rtopic in resolved.Topics)
        {
            schema._topics[rtopic.Name].See = schema.ResolveSeeList(
                rtopic.See,
                $"topic '{rtopic.Name}'"
            );
        }

        return schema;
    }

    private IReadOnlyList<IDocumented> ResolveSeeList(IReadOnlyList<string> entries, string context)
    {
        if (entries == null || entries.Count == 0)
        {
            return System.Array.Empty<IDocumented>();
        }

        var result = new List<IDocumented>(entries.Count);
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                throw new FlowerBIException($"{context} has an empty 'see' entry");
            }

            var dot = entry.IndexOf('.');
            if (dot >= 0)
            {
                var tableName = entry[..dot];
                var columnName = entry[(dot + 1)..];
                if (!_tables.TryGetValue(tableName, out var table))
                {
                    throw new FlowerBIException(
                        $"{context} 'see' references unknown table '{tableName}' in '{entry}'"
                    );
                }
                IColumn column;
                try
                {
                    column = table.GetColumn(columnName);
                }
                catch (FlowerBIException)
                {
                    throw new FlowerBIException(
                        $"{context} 'see' references unknown column '{entry}'"
                    );
                }
                result.Add(column);
            }
            else if (_topics.TryGetValue(entry, out var topic))
            {
                result.Add(topic);
            }
            else if (_tables.TryGetValue(entry, out var table))
            {
                result.Add(table);
            }
            else
            {
                throw new FlowerBIException(
                    $"{context} 'see' references '{entry}' which is neither a topic nor a table"
                );
            }
        }
        return result;
    }

    public IList<LabelledColumn> Load(IEnumerable<string> columns) =>
        columns?.Select(GetColumn).ToList() ?? new List<LabelledColumn>();

    public IEnumerable<Table> Tables => _tables.Values;

    public IReadOnlyDictionary<string, Topic> Topics => _topics;

    public Table GetTable(string refName)
    {
        if (!_tables.TryGetValue(refName, out var table))
        {
            throw new FlowerBIException($"No such table {refName}");
        }

        return table;
    }

    public Topic GetTopic(string name)
    {
        if (!_topics.TryGetValue(name, out var topic))
        {
            throw new FlowerBIException($"No such topic {name}");
        }

        return topic;
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
