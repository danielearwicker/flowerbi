import {
  Schema,
  Table,
  IColumn,
  IForeignKey,
  LabelledColumn,
  LabelledTable,
  ISqlFormatter,
  FlowerBIException,
} from '../types';
import { IdentifierPair } from '../SqlFormatter';
import { ResolvedSchema, ResolvedTable, ResolvedColumn, DataType } from './YamlSchemaTypes';

export class DynamicColumn implements IColumn {
  public readonly dbName: string;
  public readonly table: Table;
  public readonly clrType: string;
  public readonly LogicalName: string;

  constructor(table: Table, dbName: string, clrType: string, logicalName: string) {
    this.table = table;
    this.dbName = dbName;
    this.clrType = clrType;
    this.LogicalName = logicalName;
  }
}

export class DynamicForeignKey extends DynamicColumn implements IForeignKey {
  public to!: { table: Table };
  private targetColumn?: ResolvedColumn;

  constructor(table: Table, dbName: string, clrType: string, logicalName: string, target: ResolvedColumn) {
    super(table, dbName, clrType, logicalName);
    this.targetColumn = target;
  }

  bind(schema: SchemaImplementation): void {
    const targetTable = schema.getTable(this.targetColumn!.Table.Name);
    this.to = { table: targetTable };
  }
}

export class TableImplementation implements Table {
  public readonly schema: Schema;
  public readonly name: string;
  public readonly dbName: string;
  public readonly columns: IColumn[];
  public readonly id?: IColumn;
  public readonly conjoint: boolean;
  public readonly associative: IForeignKey[];

  private readonly foreignKeys = new Map<Table, IForeignKey>();
  private readonly resolvedTable: ResolvedTable;

  constructor(schema: Schema, resolvedTable: ResolvedTable) {
    this.schema = schema;
    this.name = resolvedTable.Name;
    this.dbName = resolvedTable.NameInDb;
    this.conjoint = resolvedTable.conjoint;
    this.columns = [];
    this.associative = [];
    this.resolvedTable = resolvedTable;

    // Create ID column (optional)
    if (resolvedTable.IdColumn) {
      this.id = this.createColumn(resolvedTable.IdColumn);
    }

    // Create regular columns
    for (const resolvedColumn of resolvedTable.Columns) {
      this.columns.push(this.createColumn(resolvedColumn));
    }
  }

  private createColumn(resolvedColumn: ResolvedColumn): IColumn {
    const clrType = this.getClrType(resolvedColumn.DataType, resolvedColumn.Nullable);
    
    if (resolvedColumn.Target) {
      // This is a foreign key
      return new DynamicForeignKey(this, resolvedColumn.NameInDb, clrType, resolvedColumn.Name, resolvedColumn.Target);
    } else {
      // Regular column
      return new DynamicColumn(this, resolvedColumn.NameInDb, clrType, resolvedColumn.Name);
    }
  }

  private getClrType(dataType: DataType, nullable: boolean): string {
    let baseType: string;
    
    switch (dataType) {
      case DataType.Bool:
        baseType = 'boolean';
        break;
      case DataType.Byte:
      case DataType.Short:
      case DataType.Int:
      case DataType.Long:
      case DataType.Float:
      case DataType.Double:
      case DataType.Decimal:
        baseType = 'number';
        break;
      case DataType.String:
        baseType = 'string';
        break;
      case DataType.DateTime:
        baseType = 'Date';
        break;
      default:
        baseType = 'any';
    }

    return nullable ? `${baseType} | null` : baseType;
  }

  bindForeignKeys(schema: SchemaImplementation): void {
    const allColumns = this.id ? [...this.columns, this.id] : this.columns;
    
    for (const column of allColumns) {
      if (column instanceof DynamicForeignKey) {
        column.bind(schema);
        this.foreignKeys.set(column.to.table, column);
      }
    }

    // Set up associative relationships
    for (const associativeColumn of this.resolvedTable.Associative) {
      // Find the corresponding foreign key in our columns
      const fk = allColumns.find(col => 
        col instanceof DynamicForeignKey && 
        col.dbName === associativeColumn.NameInDb
      ) as IForeignKey;
      
      if (fk) {
        this.associative.push(fk);
      }
    }
  }

  getForeignKeyTo(table: Table): IForeignKey {
    const fk = this.foreignKeys.get(table);
    if (!fk) {
      throw new FlowerBIException(`Table ${this.name} has no foreign key to ${table.name}`);
    }
    return fk;
  }

  toSql(formatter: ISqlFormatter): string {
    return IdentifierPair(formatter, (this.schema as SchemaImplementation).NameInDb, this.dbName);
  }
}

export class SchemaImplementation implements Schema {
  public readonly tables: Table[];
  public readonly NameInDb: string;
  private readonly tableMap = new Map<string, Table>();

  constructor(resolvedSchema: ResolvedSchema) {
    this.NameInDb = resolvedSchema.NameInDb;
    this.tables = [];

    // Create all tables first
    for (const resolvedTable of resolvedSchema.Tables) {
      const table = new TableImplementation(this, resolvedTable);
      this.tables.push(table);
      this.tableMap.set(table.name, table);
    }

    // Then bind foreign keys
    for (const table of this.tables) {
      if (table instanceof TableImplementation) {
        table.bindForeignKeys(this);
      }
    }
  }

  load(columns: string[]): LabelledColumn[] {
    return columns.map(columnName => this.getColumn(columnName));
  }

  getColumn(labelledName: string): LabelledColumn {
    const atIndex = labelledName.indexOf('@');
    const [name, label] = atIndex === -1 
      ? [labelledName, null] 
      : [labelledName.substring(0, atIndex), labelledName.substring(atIndex + 1)];

    const parts = name.split('.');
    if (parts.length !== 2) {
      throw new FlowerBIException('Column names must be of the form Table.Column');
    }

    const [tableName, columnName] = parts;
    const table = this.getTable(tableName);
    const column = this.getColumnFromTable(table, columnName);

    return {
      joinLabel: label,
      value: column,
    };
  }

  getTable(name: string): Table {
    const table = this.tableMap.get(name);
    if (!table) {
      throw new FlowerBIException(`No such table ${name}`);
    }
    return table;
  }

  private getColumnFromTable(table: Table, columnName: string): IColumn {
    // Check ID column
    if (table.id && (table.id.dbName === columnName || columnName === 'Id')) {
      return table.id;
    }

    // Check regular columns - need to match by logical name (from ResolvedColumn.Name)
    for (const column of table.columns) {
      if (column instanceof DynamicColumn) {
        // Get the logical name from the ResolvedColumn
        const logicalName = this.getLogicalNameFromColumn(column);
        if (logicalName === columnName || column.dbName === columnName) {
          return column;
        }
      }
    }

    throw new FlowerBIException(`No such column ${columnName} in table ${table.name}`);
  }

  private getLogicalNameFromColumn(column: DynamicColumn): string {
    return column.LogicalName;
  }
}