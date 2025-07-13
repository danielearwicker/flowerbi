import * as yaml from 'yaml';
import { 
  YamlSchema, 
  YamlTable, 
  DataType, 
  ResolvedColumn, 
  ResolvedTable, 
  ResolvedSchema 
} from './YamlSchemaTypes';
import { FlowerBIException } from '@flowerbi/query-generation';

export class SchemaResolver {
  static resolve(yamlText: string): ResolvedSchema {
    const yamlObj = yaml.parse(yamlText) as YamlSchema;
    return SchemaResolver.resolveYaml(yamlObj);
  }

  static resolveYaml(yamlSchema: YamlSchema): ResolvedSchema {
    if (!yamlSchema.schema) {
      throw new FlowerBIException('Schema must have non-empty schema property');
    }

    if (!yamlSchema.tables || Object.keys(yamlSchema.tables).length === 0) {
      throw new FlowerBIException('Schema must have non-empty tables property');
    }

    // Validate all columns are [name, type] array
    for (const [tableKey, table] of Object.entries(yamlSchema.tables)) {
      if (table.id && Object.keys(table.id).length !== 1) {
        throw new FlowerBIException(`Table ${tableKey} id must have a single column`);
      }

      if (table.columns) {
        const allColumns = { ...table.columns, ...(table.id || {}) };
        for (const [name, type] of Object.entries(allColumns)) {
          if (type.length < 1 || type.length > 2) {
            throw new FlowerBIException(
              `Table ${tableKey} column ${name} type must be an array of length 1 or 2`
            );
          }
        }
      }
    }

    // Create resolved tables
    const resolvedTables: ResolvedTable[] = Object.keys(yamlSchema.tables).map(
      tableKey => ({
        Name: tableKey,
        NameInDb: '',
        conjoint: yamlSchema.tables[tableKey].conjoint || false,
        Columns: [],
        Associative: [],
      })
    );

    const usedNames = new Set<string>();
    const resolutionStack = new Set<string>();

    const resolveTable = (tableKey: string, table: YamlTable): ResolvedTable => {
      if (resolutionStack.has(tableKey)) {
        const stackString = Array.from(resolutionStack).join(', ');
        throw new FlowerBIException(`Circular reference detected: ${stackString}`);
      }

      resolutionStack.add(tableKey);

      const resolvedTable = resolvedTables.find(x => x.Name === tableKey)!;
      if (!resolvedTable.NameInDb) {
        // Resolve ID column
        if (table.id) {
          const [idName, idType] = Object.entries(table.id)[0];
          resolvedTable.IdColumn = {
            Table: resolvedTable,
            Name: idName,
            NameInDb: idName,
            YamlType: idType,
            DataType: DataType.None,
            Nullable: false,
          };
        }

        // Resolve regular columns
        if (table.columns) {
          for (const [columnName, columnType] of Object.entries(table.columns)) {
            resolvedTable.Columns.push({
              Table: resolvedTable,
              Name: columnName,
              NameInDb: columnName,
              YamlType: columnType,
              DataType: DataType.None,
              Nullable: false,
            });
          }
        }

        resolvedTable.NameInDb = table.name || tableKey;

        // Handle extends
        if (table.extends) {
          const extendsTable = yamlSchema.tables[table.extends];
          if (!extendsTable) {
            throw new FlowerBIException(
              `No such table ${table.extends}, referenced in ${tableKey}`
            );
          }

          const resolvedExtendsTable = resolveTable(table.extends, extendsTable);

          // Add columns from extended table
          for (const column of resolvedExtendsTable.Columns) {
            resolvedTable.Columns.push({
              Table: resolvedTable,
              Name: column.Name,
              NameInDb: column.NameInDb,
              YamlType: column.YamlType,
              DataType: DataType.None,
              Nullable: false,
              Extends: column,
            });
          }

          // Add ID from extended table if we don't have one
          if (resolvedExtendsTable.IdColumn && !resolvedTable.IdColumn) {
            resolvedTable.IdColumn = {
              Table: resolvedTable,
              Name: resolvedExtendsTable.IdColumn.Name,
              NameInDb: resolvedExtendsTable.IdColumn.NameInDb,
              YamlType: resolvedExtendsTable.IdColumn.YamlType,
              DataType: DataType.None,
              Nullable: false,
              Extends: resolvedExtendsTable.IdColumn,
            };
          }

          resolvedTable.NameInDb = resolvedTable.NameInDb || resolvedExtendsTable.NameInDb;
        }

        if (resolvedTable.Columns.length === 0) {
          throw new FlowerBIException(
            `Table ${tableKey} must have columns (or use 'extends')`
          );
        }

        // Handle associative columns
        if (table.associative) {
          const allColumns = resolvedTable.IdColumn 
            ? [...resolvedTable.Columns, resolvedTable.IdColumn]
            : resolvedTable.Columns;

          for (const assocName of table.associative) {
            const assocColumn = allColumns.find(c => c.Name === assocName);
            if (!assocColumn) {
              throw new FlowerBIException(
                `Table ${tableKey} has an association ${assocName} that is not a column`
              );
            }
            resolvedTable.Associative.push(assocColumn);
          }
        }
      }

      resolutionStack.delete(tableKey);
      return resolvedTable;
    };

    // Resolve all tables
    for (const [tableKey, table] of Object.entries(yamlSchema.tables)) {
      if (!tableKey) {
        throw new FlowerBIException('Table must have non-empty key');
      }

      if (!usedNames.add(tableKey)) {
        throw new FlowerBIException(`More than one table is named '${tableKey}'`);
      }

      resolveTable(tableKey, table);
    }

    // Resolve column types
    const columnResolutionStack = new Set<string>();

    const resolveColumnType = (column: ResolvedColumn): void => {
      const stackKey = `${column.Table.Name}.${column.Name}`;
      if (columnResolutionStack.has(stackKey)) {
        const stackString = Array.from(columnResolutionStack).join(', ');
        throw new FlowerBIException(`Circular reference detected: ${stackString}`);
      }

      columnResolutionStack.add(stackKey);

      if (column.DataType === DataType.None) {
        const typeString = column.YamlType[0];
        const nullable = typeString.endsWith('?');
        const typeName = nullable ? typeString.slice(0, -1) : typeString;

        // Try to parse as primitive data type
        const dataTypeValue = Object.values(DataType).find(dt => dt.toLowerCase() === typeName.toLowerCase());
        if (dataTypeValue && dataTypeValue !== DataType.None) {
          column.DataType = dataTypeValue as DataType;
        } else {
          // Try to find as foreign key to another table
          const targetTable = resolvedTables.find(t => t.Name === typeName);
          if (!targetTable?.IdColumn) {
            throw new FlowerBIException(
              `${typeName} is neither a data type nor a table, in ${column.Table.Name}.${column.Name}`
            );
          }

          resolveColumnType(targetTable.IdColumn);
          column.Target = targetTable.IdColumn;
          column.DataType = targetTable.IdColumn.DataType;
        }

        column.NameInDb = column.YamlType.length === 2 ? column.YamlType[1] : column.Name;
        column.Nullable = nullable;
      }

      columnResolutionStack.delete(stackKey);
    };

    // Resolve all column types
    for (const table of resolvedTables) {
      if (table.IdColumn) {
        resolveColumnType(table.IdColumn);
      }

      for (const column of table.Columns) {
        resolveColumnType(column);
      }
    }

    return {
      Name: yamlSchema.schema,
      NameInDb: yamlSchema.name || yamlSchema.schema,
      Tables: resolvedTables,
    };
  }
}