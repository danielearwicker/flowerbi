namespace FlowerBI.Yaml;

using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

public record ResolvedSchema(string Name, string NameInDb, IEnumerable<ResolvedTable> Tables)
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

        // Validate all columns are [name, type] array
        foreach (var (tableKey, table) in yaml.tables)
        {
            if (table.id != null && table.id.Count != 1)
            {
                throw new FlowerBIException($"Table {tableKey} id must have a single column");
            }

            if (table.columns != null)
            {
                foreach (
                    var (name, type) in table.columns.Concat(
                        table.id ?? new Dictionary<string, string[]>()
                    )
                )
                {
                    if (type.Length < 1 || type.Length > 2)
                    {
                        throw new FlowerBIException(
                            $"Table {tableKey} column {name} type must be an array of length 1 or 2"
                        );
                    }
                }
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
                resolvedTable.IdColumn =
                    table.id != null
                        ? new ResolvedColumn(
                            resolvedTable,
                            table.id.First().Key,
                            table.id.First().Value
                        )
                        : null;
                if (table.columns != null)
                {
                    resolvedTable.Columns.AddRange(
                        table.columns.Select(x => new ResolvedColumn(resolvedTable, x.Key, x.Value))
                    );
                }
                resolvedTable.NameInDb = table.name;

                if (table.extends != null)
                {
                    if (!yaml.tables.TryGetValue(table.extends, out var extendsYaml))
                    {
                        throw new FlowerBIException(
                            $"No such table {table.extends}, referenced in {tableKey}"
                        );
                    }

                    var extendsTable = ResolveTable(table.extends, extendsYaml);

                    resolvedTable.Columns.AddRange(
                        extendsTable.Columns.Select(x => new ResolvedColumn(
                            resolvedTable,
                            x.Name,
                            x.YamlType
                        )
                        {
                            Extends = x,
                        })
                    );

                    if (extendsTable.IdColumn != null)
                    {
                        resolvedTable.IdColumn ??= new ResolvedColumn(
                            resolvedTable,
                            extendsTable.IdColumn.Name,
                            extendsTable.IdColumn.YamlType
                        )
                        {
                            Extends = extendsTable.IdColumn,
                        };
                    }

                    resolvedTable.NameInDb ??= extendsTable.NameInDb;
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

        return new ResolvedSchema(yaml.schema, yaml.name ?? yaml.schema, resolvedTables);
    }
}
