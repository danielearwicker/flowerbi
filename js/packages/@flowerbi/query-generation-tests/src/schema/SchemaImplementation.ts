import {
  Schema,
  Table,
  IColumn,
  IForeignKey,
  LabelledColumn,
  LabelledTable,
  ISqlFormatter,
  FlowerBIException,
} from '@flowerbi/query-generation';
import { IdentifierPair } from '@flowerbi/query-generation';
import { ResolvedSchema, ResolvedTable, ResolvedColumn, DataType } from './YamlSchemaTypes';

export class DynamicColumn implements IColumn {
  public readonly DbName: string;
  public readonly Table: Table;
  public readonly ClrType: string;
  public readonly LogicalName: string;

  constructor(table: Table, dbName: string, clrType: string, logicalName: string) {
    this.Table = table;
    this.DbName = dbName;
    this.ClrType = clrType;
    this.LogicalName = logicalName;
  }
}

export class DynamicForeignKey extends DynamicColumn implements IForeignKey {
  public To!: { Table: Table };
  private targetColumn?: ResolvedColumn;

  constructor(table: Table, dbName: string, clrType: string, logicalName: string, target: ResolvedColumn) {
    super(table, dbName, clrType, logicalName);
    this.targetColumn = target;
  }

  bind(schema: SchemaImplementation): void {
    const targetTable = schema.getTable(this.targetColumn!.Table.Name);
    this.To = { Table: targetTable };
  }
}

export class TableImplementation implements Table {
  public readonly Schema: Schema;
  public readonly Name: string;
  public readonly DbName: string;
  public readonly Columns: IColumn[];
  public readonly Id?: IColumn;
  public readonly Conjoint: boolean;
  public readonly Associative: IForeignKey[];

  private readonly foreignKeys = new Map<Table, IForeignKey>();
  private readonly resolvedTable: ResolvedTable;

  constructor(schema: Schema, resolvedTable: ResolvedTable) {
    this.Schema = schema;
    this.Name = resolvedTable.Name;
    this.DbName = resolvedTable.NameInDb;
    this.Conjoint = resolvedTable.conjoint;
    this.Columns = [];
    this.Associative = [];
    this.resolvedTable = resolvedTable;

    // Create ID column (optional)
    if (resolvedTable.IdColumn) {
      this.Id = this.createColumn(resolvedTable.IdColumn);
    }

    // Create regular columns
    for (const resolvedColumn of resolvedTable.Columns) {
      this.Columns.push(this.createColumn(resolvedColumn));
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
    const allColumns = this.Id ? [...this.Columns, this.Id] : this.Columns;
    
    for (const column of allColumns) {
      if (column instanceof DynamicForeignKey) {
        column.bind(schema);
        this.foreignKeys.set(column.To.Table, column);
      }
    }

    // Set up associative relationships
    for (const associativeColumn of this.resolvedTable.Associative) {
      // Find the corresponding foreign key in our columns
      const fk = allColumns.find(col => 
        col instanceof DynamicForeignKey && 
        col.DbName === associativeColumn.NameInDb
      ) as IForeignKey;
      
      if (fk) {
        this.Associative.push(fk);
      }
    }
  }

  GetForeignKeyTo(table: Table): IForeignKey {
    const fk = this.foreignKeys.get(table);
    if (!fk) {
      throw new FlowerBIException(`Table ${this.Name} has no foreign key to ${table.Name}`);
    }
    return fk;
  }

  ToSql(formatter: ISqlFormatter): string {
    return IdentifierPair(formatter, (this.Schema as SchemaImplementation).NameInDb, this.DbName);
  }
}

export class SchemaImplementation implements Schema {
  public readonly Tables: Table[];
  public readonly NameInDb: string;
  private readonly tableMap = new Map<string, Table>();

  constructor(resolvedSchema: ResolvedSchema) {
    this.NameInDb = resolvedSchema.NameInDb;
    this.Tables = [];

    // Create all tables first
    for (const resolvedTable of resolvedSchema.Tables) {
      const table = new TableImplementation(this, resolvedTable);
      this.Tables.push(table);
      this.tableMap.set(table.Name, table);
    }

    // Then bind foreign keys
    for (const table of this.Tables) {
      if (table instanceof TableImplementation) {
        table.bindForeignKeys(this);
      }
    }
  }

  Load(columns: string[]): LabelledColumn[] {
    return columns.map(columnName => this.GetColumn(columnName));
  }

  GetColumn(labelledName: string): LabelledColumn {
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
      JoinLabel: label,
      Value: column,
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
    if (table.Id && (table.Id.DbName === columnName || columnName === 'Id')) {
      return table.Id;
    }

    // Check regular columns - need to match by logical name (from ResolvedColumn.Name)
    for (const column of table.Columns) {
      if (column instanceof DynamicColumn) {
        // Get the logical name from the ResolvedColumn
        const logicalName = this.getLogicalNameFromColumn(column);
        if (logicalName === columnName || column.DbName === columnName) {
          return column;
        }
      }
    }

    throw new FlowerBIException(`No such column ${columnName} in table ${table.Name}`);
  }

  private getLogicalNameFromColumn(column: DynamicColumn): string {
    return column.LogicalName;
  }
}