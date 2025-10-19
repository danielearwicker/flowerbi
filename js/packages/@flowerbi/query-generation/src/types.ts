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
  column: string;
  operator: string;
  value: any;
  constant?: any;
}

export interface AggregationJson {
  function: AggregationType;
  column: string;
  filters?: FilterJson[];
}

export interface OrderingJson {
  descending: boolean;
  column?: string;
  type?: OrderingType;
  index?: number;
}

export interface CalculationJson {
  value?: number;
  aggregation?: number;
  first?: CalculationJson;
  second?: CalculationJson;
  operator?: string;
}

export interface QueryJson {
  select?: string[];
  aggregations?: AggregationJson[];
  filters?: FilterJson[];
  orderBy?: OrderingJson[];
  calculations?: CalculationJson[];
  totals?: boolean;
  skip?: number;
  take?: number;
  comment?: string;
  allowDuplicates?: boolean;
  fullJoins?: boolean;
}

export interface QueryRecordJson {
  selected: any[];
  aggregated: any[];
}

export interface QueryResultJson {
  records: QueryRecordJson[];
  totals?: QueryRecordJson;
}

// Schema-related interfaces (simplified for now)
export interface IColumn {
  dbName: string;
  table: Table;
  clrType?: string;
}

export interface IForeignKey extends IColumn {
  to: { table: Table };
}

export interface Table {
  schema: Schema;
  name: string;
  columns: IColumn[];
  id?: IColumn;
  conjoint?: boolean;
  associative?: IForeignKey[];
  getForeignKeyTo(table: Table): IForeignKey;
  toSql(formatter: ISqlFormatter): string;
}

export interface Schema {
  tables: Table[];
  load(columns: string[]): LabelledColumn[];
  getColumn(name: string): LabelledColumn;
}

export interface LabelledColumn {
  value: IColumn;
  joinLabel: string | null;
}

export interface LabelledTable {
  value: Table;
  joinLabel: string | null;
}

export interface ISqlFormatter {
  identifier(name: string): string;
  escapedIdentifierPair(id1: string, id2: string): string;
  skipAndTake(skip: number, take: number): string;
  conditional(predExpr: string, thenExpr: string, elseExpr: string): string;
  castToFloat(valueExpr: string): string;
  getParamPrefix(): string;
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