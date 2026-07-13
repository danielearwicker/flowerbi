namespace FlowerBI.Yaml;

using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

public record ResolvedSchema(
    string Name,
    string NameInDb,
    IEnumerable<ResolvedTable> Tables,
    IEnumerable<ResolvedTopic> Topics
)
{
    public static ResolvedSchema Resolve(string yamlText)
    {
        var deserializer = new DeserializerBuilder().Build();
        var yaml = deserializer.Deserialize<YamlSchema>(yamlText);
        return Resolve(yaml);
    }

    public static ResolvedSchema Resolve(YamlSchema yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml.schema))
        {
            throw new FlowerBIException("Schema must have non-empty schema property");
        }

        if (yaml.tables == null || !yaml.tables.Any())
        {
            throw new FlowerBIException("Schema must have non-empty tables property");
        }

        // Normalise loose column values into a canonical shape (yamlType + doc + see)
        // Each (tableKey -> columnName -> NormalisedColumn). Done up-front so the rest of
        // resolution can ignore the dual-form distinction.
        var normalisedColumns = new Dictionary<string, Dictionary<string, NormalisedColumn>>();
        var normalisedIds = new Dictionary<string, KeyValuePair<string, NormalisedColumn>?>();

        foreach (var (tableKey, table) in yaml.tables)
        {
            if (table.id != null && table.id.Count != 1)
            {
                throw new FlowerBIException($"Table {tableKey} id must have a single column");
            }

            var cols = new Dictionary<string, NormalisedColumn>();
            normalisedColumns[tableKey] = cols;

            if (table.columns != null)
            {
                foreach (var (name, raw) in table.columns)
                {
                    cols[name] = NormaliseColumn(tableKey, name, raw);
                }
            }

            if (table.id != null)
            {
                var (idName, idRaw) = table.id.First();
                normalisedIds[tableKey] = new KeyValuePair<string, NormalisedColumn>(
                    idName,
                    NormaliseColumn(tableKey, idName, idRaw)
                );
            }
            else
            {
                normalisedIds[tableKey] = null;
            }
        }

        var resolvedTables = yaml
            .tables.Select(t => new ResolvedTable(t.Key, t.Value.conjoint))
            .ToList();
        var usedNames = new HashSet<string>();

        var resolutionStack = new HashSet<string>();

        ResolvedTable ResolveTable(string tableKey, YamlTable table)
        {
            if (!resolutionStack.Add(tableKey))
            {
                var stackString = string.Join(", ", resolutionStack);
                throw new FlowerBIException($"Circular reference detected: {stackString}");
            }

            var resolvedTable = resolvedTables.FirstOrDefault(x => x.Name == tableKey);
            if (resolvedTable.NameInDb == null)
            {
                var idEntry = normalisedIds[tableKey];
                resolvedTable.IdColumn = idEntry.HasValue
                    ? new ResolvedColumn(
                        resolvedTable,
                        idEntry.Value.Key,
                        idEntry.Value.Value.YamlType
                    )
                    {
                        Doc = idEntry.Value.Value.Doc,
                        See = idEntry.Value.Value.See,
                        Meta = idEntry.Value.Value.Meta,
                    }
                    : null;

                foreach (var (colName, norm) in normalisedColumns[tableKey])
                {
                    resolvedTable.Columns.Add(
                        new ResolvedColumn(resolvedTable, colName, norm.YamlType)
                        {
                            Doc = norm.Doc,
                            See = norm.See,
                            Meta = norm.Meta,
                        }
                    );
                }
                resolvedTable.NameInDb = table.name;
                resolvedTable.Doc = table.doc;
                resolvedTable.See = table.see ?? Array.Empty<string>();
                resolvedTable.Meta = table.meta ?? EmptyMeta;

                if (table.extends != null)
                {
                    if (!yaml.tables.TryGetValue(table.extends, out var extendsYaml))
                    {
                        throw new FlowerBIException(
                            $"No such table {table.extends}, referenced in {tableKey}"
                        );
                    }

                    var extendsTable = ResolveTable(table.extends, extendsYaml);

                    var existingNames = new HashSet<string>(
                        resolvedTable.Columns.Select(c => c.Name)
                    );
                    foreach (var inherited in extendsTable.Columns)
                    {
                        if (existingNames.Contains(inherited.Name))
                        {
                            continue;
                        }
                        resolvedTable.Columns.Add(
                            new ResolvedColumn(resolvedTable, inherited.Name, inherited.YamlType)
                            {
                                Extends = inherited,
                                Doc = inherited.Doc,
                                See = inherited.See,
                                Meta = inherited.Meta,
                            }
                        );
                    }

                    if (extendsTable.IdColumn != null && resolvedTable.IdColumn == null)
                    {
                        resolvedTable.IdColumn = new ResolvedColumn(
                            resolvedTable,
                            extendsTable.IdColumn.Name,
                            extendsTable.IdColumn.YamlType
                        )
                        {
                            Extends = extendsTable.IdColumn,
                            Doc = extendsTable.IdColumn.Doc,
                            See = extendsTable.IdColumn.See,
                            Meta = extendsTable.IdColumn.Meta,
                        };
                    }

                    resolvedTable.NameInDb ??= extendsTable.NameInDb;
                    resolvedTable.Doc ??= extendsTable.Doc;
                    if (table.see == null)
                    {
                        resolvedTable.See = extendsTable.See;
                    }
                    // Per-key merge: the base table's meta is inherited, but any keys the
                    // derived table declares itself take precedence.
                    resolvedTable.Meta = MergeMeta(extendsTable.Meta, resolvedTable.Meta);
                }

                if (!resolvedTable.Columns.Any())
                {
                    throw new FlowerBIException(
                        $"Table {tableKey} must have columns (or use 'extends')"
                    );
                }

                resolvedTable.NameInDb ??= tableKey;

                if (table.associative != null)
                {
                    var allColumns =
                        resolvedTable.IdColumn == null
                            ? resolvedTable.Columns
                            : resolvedTable.Columns.Append(resolvedTable.IdColumn);

                    foreach (var assoc in table.associative)
                    {
                        var resolvedAssoc = allColumns.FirstOrDefault(c => c.Name == assoc);
                        if (resolvedAssoc == null)
                        {
                            throw new FlowerBIException(
                                $"Table {tableKey} has an association {assoc} that is not a column"
                            );
                        }
                        resolvedTable.Associative.Add(resolvedAssoc);
                    }
                }
            }

            resolutionStack.Remove(tableKey);

            return resolvedTable;
        }

        foreach (var (tableKey, table) in yaml.tables)
        {
            if (string.IsNullOrWhiteSpace(tableKey))
            {
                throw new FlowerBIException("Table must have non-empty key");
            }

            if (!usedNames.Add(tableKey))
            {
                throw new FlowerBIException($"More than one table is named '{tableKey}'");
            }

            ResolveTable(tableKey, table);
        }

        void ResolveColumnType(ResolvedColumn c)
        {
            var stackKey = $"{c.Table.Name}.{c.Name}";
            if (!resolutionStack.Add(stackKey))
            {
                var stackString = string.Join(", ", resolutionStack);
                throw new FlowerBIException($"Circular reference detected: {stackString}");
            }

            if (c.DataType == DataType.None)
            {
                var (typeName, nullable) =
                    c.YamlType[0].Last() == '?'
                        ? (c.YamlType[0][0..^1], true)
                        : (c.YamlType[0], false);
                if (Enum.TryParse<DataType>(typeName, true, out var dataType))
                {
                    c.DataType = dataType;
                }
                else
                {
                    var targetColumn = resolvedTables
                        .FirstOrDefault(x => x.Name == typeName)
                        ?.IdColumn;
                    if (targetColumn == null)
                    {
                        throw new FlowerBIException(
                            $"{typeName} is neither a data type nor a table, in {c.Table.Name}.{c.Name}"
                        );
                    }

                    ResolveColumnType(targetColumn);
                    c.Target = targetColumn;
                    c.DataType = targetColumn.DataType;
                }
                c.NameInDb = c.YamlType.Length == 2 ? c.YamlType[1] : c.Name;
                c.Nullable = nullable;
            }

            resolutionStack.Remove(stackKey);
        }

        foreach (var table in resolvedTables)
        {
            if (table.IdColumn != null)
            {
                ResolveColumnType(table.IdColumn);
            }

            foreach (var column in table.Columns)
            {
                ResolveColumnType(column);
            }
        }

        var resolvedTopics = new List<ResolvedTopic>();
        if (yaml.topics != null)
        {
            var topicNames = new HashSet<string>();
            foreach (var (topicKey, raw) in yaml.topics)
            {
                if (string.IsNullOrWhiteSpace(topicKey))
                {
                    throw new FlowerBIException("Topic must have non-empty key");
                }
                if (!topicNames.Add(topicKey))
                {
                    throw new FlowerBIException($"More than one topic is named '{topicKey}'");
                }
                if (yaml.tables.ContainsKey(topicKey))
                {
                    throw new FlowerBIException(
                        $"Topic '{topicKey}' has the same name as a table; topic and table names must not collide"
                    );
                }
                resolvedTopics.Add(NormaliseTopic(topicKey, raw));
            }
        }

        return new ResolvedSchema(
            yaml.schema,
            yaml.name ?? yaml.schema,
            resolvedTables,
            resolvedTopics
        );
    }

    private readonly record struct NormalisedColumn(
        string[] YamlType,
        string Doc,
        IReadOnlyList<string> See,
        IReadOnlyDictionary<string, string> Meta
    );

    private static NormalisedColumn NormaliseColumn(string tableKey, string columnName, object raw)
    {
        // Legacy short form: a YAML sequence -> List<object> of strings.
        if (raw is IList<object> seq)
        {
            if (seq.Count < 1 || seq.Count > 2)
            {
                throw new FlowerBIException(
                    $"Table {tableKey} column {columnName} type must be an array of length 1 or 2"
                );
            }
            var asStrings = seq.Select(x => x?.ToString()).ToArray();
            return new NormalisedColumn(
                asStrings,
                null,
                Array.Empty<string>(),
                EmptyMeta
            );
        }

        // New long form: a YAML mapping -> Dictionary<object, object>.
        if (raw is IDictionary<object, object> map)
        {
            string type = null;
            string dbName = null;
            string doc = null;
            IReadOnlyList<string> see = Array.Empty<string>();
            IReadOnlyDictionary<string, string> meta = EmptyMeta;

            foreach (var (rawKey, rawVal) in map)
            {
                var key = rawKey?.ToString();
                switch (key)
                {
                    case "type":
                        type = rawVal?.ToString();
                        break;
                    case "name":
                        dbName = rawVal?.ToString();
                        break;
                    case "doc":
                        doc = rawVal?.ToString();
                        break;
                    case "see":
                        see = ReadStringList(rawVal, $"{tableKey}.{columnName}.see");
                        break;
                    case "meta":
                        meta = ReadStringMap(rawVal, $"{tableKey}.{columnName}.meta");
                        break;
                    default:
                        throw new FlowerBIException(
                            $"Table {tableKey} column {columnName} has unknown property '{key}'"
                        );
                }
            }

            if (string.IsNullOrEmpty(type))
            {
                throw new FlowerBIException(
                    $"Table {tableKey} column {columnName} must specify a 'type'"
                );
            }

            var yamlType = dbName == null ? new[] { type } : new[] { type, dbName };
            return new NormalisedColumn(yamlType, doc, see, meta);
        }

        throw new FlowerBIException(
            $"Table {tableKey} column {columnName} must be either a [type] sequence or a mapping with a 'type' key"
        );
    }

    private static ResolvedTopic NormaliseTopic(string name, object raw)
    {
        if (raw is string s)
        {
            return new ResolvedTopic(name, s);
        }

        if (raw is IDictionary<object, object> map)
        {
            string doc = null;
            IReadOnlyList<string> see = Array.Empty<string>();

            foreach (var (rawKey, rawVal) in map)
            {
                var key = rawKey?.ToString();
                switch (key)
                {
                    case "doc":
                        doc = rawVal?.ToString();
                        break;
                    case "see":
                        see = ReadStringList(rawVal, $"topic '{name}' see");
                        break;
                    default:
                        throw new FlowerBIException($"Topic '{name}' has unknown property '{key}'");
                }
            }

            return new ResolvedTopic(name, doc) { See = see };
        }

        throw new FlowerBIException(
            $"Topic '{name}' must be either a string or a mapping with 'doc' and optional 'see'"
        );
    }

    private static IReadOnlyList<string> ReadStringList(object raw, string context)
    {
        if (raw == null)
        {
            return Array.Empty<string>();
        }
        if (raw is IList<object> list)
        {
            return list.Select(x => x?.ToString()).ToArray();
        }
        throw new FlowerBIException($"{context} must be a list of strings");
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyMeta =
        new Dictionary<string, string>();

    private static IReadOnlyDictionary<string, string> ReadStringMap(object raw, string context)
    {
        if (raw == null)
        {
            return EmptyMeta;
        }
        if (raw is IDictionary<object, object> map)
        {
            var result = new Dictionary<string, string>();
            foreach (var (rawKey, rawVal) in map)
            {
                result[rawKey?.ToString()] = rawVal?.ToString();
            }
            return result;
        }
        throw new FlowerBIException($"{context} must be a mapping of name/value pairs");
    }

    // Combine an inherited (base) meta map with a derived table's own map, so that the
    // derived table's keys override the base's on a per-key basis.
    private static IReadOnlyDictionary<string, string> MergeMeta(
        IReadOnlyDictionary<string, string> baseMeta,
        IReadOnlyDictionary<string, string> ownMeta
    )
    {
        if (baseMeta == null || baseMeta.Count == 0)
        {
            return ownMeta ?? EmptyMeta;
        }
        if (ownMeta == null || ownMeta.Count == 0)
        {
            return baseMeta;
        }
        var merged = new Dictionary<string, string>();
        foreach (var (key, value) in baseMeta)
        {
            merged[key] = value;
        }
        foreach (var (key, value) in ownMeta)
        {
            merged[key] = value;
        }
        return merged;
    }
}
