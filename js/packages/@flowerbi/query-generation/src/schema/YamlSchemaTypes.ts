export interface YamlSchema {
  schema: string;
  name?: string;
  tables: Record<string, YamlTable>;
}

export interface YamlTable {
  name?: string;
  id?: Record<string, string[]>;
  columns?: Record<string, string[]>;
  extends?: string;
  conjoint?: boolean;
  associative?: string[];
}

export enum DataType {
  None = 'None',
  Bool = 'bool',
  Byte = 'byte',
  Short = 'short',
  Int = 'int',
  Long = 'long',
  Float = 'float',
  Double = 'double',
  Decimal = 'decimal',
  String = 'string',
  DateTime = 'DateTime',
}

export interface ResolvedColumn {
  Table: ResolvedTable;
  Name: string;
  NameInDb: string;
  YamlType: string[];
  DataType: DataType;
  Nullable: boolean;
  Target?: ResolvedColumn;
  Extends?: ResolvedColumn;
}

export interface ResolvedTable {
  Name: string;
  NameInDb: string;
  conjoint: boolean;
  IdColumn?: ResolvedColumn;
  Columns: ResolvedColumn[];
  Associative: ResolvedColumn[];
}

export interface ResolvedSchema {
  Name: string;
  NameInDb: string;
  Tables: ResolvedTable[];
}