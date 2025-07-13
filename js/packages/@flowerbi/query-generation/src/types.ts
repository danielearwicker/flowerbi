export enum AggregationType {
  Count = 'Count',
  Sum = 'Sum',
  Avg = 'Avg',
  Min = 'Min',
  Max = 'Max',
  CountDistinct = 'CountDistinct',
}

export enum OrderingType {
  Select = 'Select',
  Value = 'Value',
  Calculation = 'Calculation',
}

export interface FilterJson {
  Column: string;
  Operator: string;
  Value: any;
  Constant?: any;
}

export interface AggregationJson {
  Function: AggregationType;
  Column: string;
  Filters?: FilterJson[];
}

export interface OrderingJson {
  Descending: boolean;
  Column?: string;
  Type?: OrderingType;
  Index?: number;
}

export interface CalculationJson {
  Value?: number;
  Aggregation?: number;
  First?: CalculationJson;
  Second?: CalculationJson;
  Operator?: string;
}

export interface QueryJson {
  Select?: string[];
  Aggregations?: AggregationJson[];
  Filters?: FilterJson[];
  OrderBy?: OrderingJson[];
  Calculations?: CalculationJson[];
  Totals?: boolean;
  Skip?: number;
  Take?: number;
  Comment?: string;
  AllowDuplicates?: boolean;
  FullJoins?: boolean;
}

export interface QueryRecordJson {
  Selected: any[];
  Aggregated: any[];
}

export interface QueryResultJson {
  Records: QueryRecordJson[];
  Totals?: QueryRecordJson;
}

// Schema-related interfaces (simplified for now)
export interface IColumn {
  DbName: string;
  Table: Table;
  ClrType?: string;
}

export interface IForeignKey extends IColumn {
  To: { Table: Table };
}

export interface Table {
  Schema: Schema;
  Name: string;
  Columns: IColumn[];
  Id?: IColumn;
  Conjoint?: boolean;
  Associative?: IForeignKey[];
  GetForeignKeyTo(table: Table): IForeignKey;
  ToSql(formatter: ISqlFormatter): string;
}

export interface Schema {
  Tables: Table[];
  Load(columns: string[]): LabelledColumn[];
  GetColumn(name: string): LabelledColumn;
}

export interface LabelledColumn {
  Value: IColumn;
  JoinLabel: string | null;
}

export interface LabelledTable {
  Value: Table;
  JoinLabel: string | null;
}

export interface ISqlFormatter {
  Identifier(name: string): string;
  EscapedIdentifierPair(id1: string, id2: string): string;
  SkipAndTake(skip: number, take: number): string;
  Conditional(predExpr: string, thenExpr: string, elseExpr: string): string;
  CastToFloat(valueExpr: string): string;
  GetParamPrefix(): string;
}

export interface IFilterParameters {
  [key: string]: any;
}

export class FlowerBIException extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'FlowerBIException';
  }
}