import { YamlColumn, YamlSchema, YamlTable } from "./FlowerBIYamlSchema";

interface ResolvedColumn {
    table: ResolvedTable;
    yamlColumn: YamlColumn;    
    foreignKeyTarget?: ResolvedColumn;
}

interface ResolvedTable {
    yamlTable: YamlTable;
    primaryKey: ResolvedColumn;
    columns: {
        [column: string]: ResolvedColumn;
    };
}

function resolve(yamlSchema: YamlSchema) {

    for (const [tableKey, yamlTable] of Object.entries(yamlSchema.tables)) {
        if (yamlTable.id && Object.keys(yamlTable).length !== 1) {
            throw new Error(`Table ${tableKey} id must have a single column`);
        }

        const allColumns = [yamlTable.id!, yamlTable.columns!]
                .filter(x => !!x)
                .flatMap(x => Object.entries(x));

        for (const [columnKey, yamlColumn] of allColumns) {
            if (yamlColumn.length < 1 || yamlColumn.length > 2) {
                throw new Error(`Table ${tableKey} column ${columnKey} type must be an array of length 1 or 2`);
            }
        }
        
        const usedNames = new Set<string>();
        const resolutionStack = new Set<string>();
        const resolvedTables: {
            [name: string]: ResolvedTable; 
        } = {};

        function resolveTable(tableKey: string) {
            if (resolutionStack.has(tableKey)) {
                throw new Error(`Circular reference detected: ${Array.from(resolutionStack.keys()).join(", ")}`);
            }

            let resolvedTable = resolvedTables[tableKey];
            if (resolvedTable) {
                return resolveTable;
            }

            const yamlTable = yamlSchema[tableKey];
            if (!yamlTable) {
                throw new Error(`No such table: ${tableKey}`);
            }

            const primaryKey = !yamlTable.id ? undefined :
                    

            resolvedTable = { yamlTable };

            resolvedTable.IdColumn = table.id != null ? new ResolvedColumn(resolvedTable, table.id.First().Key, table.id.First().Value) : null;
                if (table.columns != null)
                {
                    resolvedTable.Columns.AddRange(table.columns.Select(x => new ResolvedColumn(resolvedTable, x.Key, x.Value)));
                }
                resolvedTable.NameInDb = table.name;

                if (table.extends != null)
                {                    
                    if (!yaml.tables.TryGetValue(table.extends, out var extendsYaml))
                    {
                        throw new InvalidOperationException($"No such table {table.extends}, referenced in {tableKey}");
                    }

                    var extendsTable = ResolveTable(table.extends, extendsYaml);

                    resolvedTable.Columns.AddRange(extendsTable.Columns.Select(x => new ResolvedColumn(resolvedTable, x.Name, x.YamlType)
                    {
                        Extends = x
                    }));

                    if (extendsTable.IdColumn != null)
                    {
                        resolvedTable.IdColumn ??= new ResolvedColumn(resolvedTable, extendsTable.IdColumn.Name, extendsTable.IdColumn.YamlType)
                        {
                            Extends = extendsTable.IdColumn
                        };
                    }

                    resolvedTable.NameInDb ??= extendsTable.NameInDb;
                }

                if (!resolvedTable.Columns.Any())
                {
                    throw new InvalidOperationException($"Table {tableKey} must have columns (or use 'extends')");
                }

                resolvedTable.NameInDb ??= tableKey;
        }
/*
            
        var resolvedTables = yaml.tables.Select(t => new ResolvedTable(t.Key, t.Value.conjoint)).ToList();
        var usedNames = new HashSet<string>();
        
        var resolutionStack = new HashSet<string>();

        ResolvedTable ResolveTable(string tableKey, YamlTable table)
        {
            if (!resolutionStack.Add(tableKey))
            {
                var stackString = string.Join(", ", resolutionStack);
                throw new InvalidOperationException($"Circular reference detected: {stackString}");
            }

            var resolvedTable = resolvedTables.FirstOrDefault(x => x.Name == tableKey);
            if (resolvedTable.NameInDb == null)
            {
                resolvedTable.IdColumn = table.id != null ? new ResolvedColumn(resolvedTable, table.id.First().Key, table.id.First().Value) : null;
                if (table.columns != null)
                {
                    resolvedTable.Columns.AddRange(table.columns.Select(x => new ResolvedColumn(resolvedTable, x.Key, x.Value)));
                }
                resolvedTable.NameInDb = table.name;

                if (table.extends != null)
                {                    
                    if (!yaml.tables.TryGetValue(table.extends, out var extendsYaml))
                    {
                        throw new InvalidOperationException($"No such table {table.extends}, referenced in {tableKey}");
                    }

                    var extendsTable = ResolveTable(table.extends, extendsYaml);

                    resolvedTable.Columns.AddRange(extendsTable.Columns.Select(x => new ResolvedColumn(resolvedTable, x.Name, x.YamlType)
                    {
                        Extends = x
                    }));

                    if (extendsTable.IdColumn != null)
                    {
                        resolvedTable.IdColumn ??= new ResolvedColumn(resolvedTable, extendsTable.IdColumn.Name, extendsTable.IdColumn.YamlType)
                        {
                            Extends = extendsTable.IdColumn
                        };
                    }

                    resolvedTable.NameInDb ??= extendsTable.NameInDb;
                }

                if (!resolvedTable.Columns.Any())
                {
                    throw new InvalidOperationException($"Table {tableKey} must have columns (or use 'extends')");
                }

                resolvedTable.NameInDb ??= tableKey;
            }

            resolutionStack.Remove(tableKey);

            return resolvedTable;
        }

        foreach (var (tableKey, table) in yaml.tables)
        {
            if (string.IsNullOrWhiteSpace(tableKey))
            {
                throw new InvalidOperationException("Table must have non-empty key");
            }

            if (!usedNames.Add(tableKey))
            {
                throw new InvalidOperationException($"More than one table is named '{tableKey}'");
            }

            ResolveTable(tableKey, table);            
        }

        void ResolveColumnType(ResolvedColumn c)
        {
            var stackKey = $"{c.Table.Name}.{c.Name}";
            if (!resolutionStack.Add(stackKey))
            {
                var stackString = string.Join(", ", resolutionStack);
                throw new InvalidOperationException($"Circular reference detected: {stackString}");
            }

            if (c.DataType == DataType.None)
            {
                var (typeName, nullable) = c.YamlType[0].Last() == '?' ? (c.YamlType[0][0..^1], true) : (c.YamlType[0], false);
                if (Enum.TryParse<DataType>(typeName, true, out var dataType))
                {
                    c.DataType = dataType;
                }
                else
                {
                    var targetColumn = resolvedTables.FirstOrDefault(x => x.Name == typeName)?.IdColumn;
                    if (targetColumn == null)
                    {
                        throw new InvalidOperationException($"{typeName} is neither a data type nor a table, in {c.Table.Name}.{c.Name}");
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

        */