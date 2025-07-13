import { AggregationJson, AggregationType, LabelledColumn, Schema } from './types';
import { Filter } from './Filter';

export class Aggregation {
  public readonly Function: AggregationType;
  public readonly Column: LabelledColumn;
  public readonly Filters: Filter[];

  constructor(func: AggregationType, column: LabelledColumn, filters: Filter[] = []) {
    this.Function = func;
    this.Column = column;
    this.Filters = filters;
  }

  static fromJson(json: AggregationJson, schema: Schema): Aggregation {
    return new Aggregation(
      json.Function,
      schema.GetColumn(json.Column),
      Filter.Load(json.Filters, schema)
    );
  }

  static Load(aggregations: AggregationJson[] | undefined, schema: Schema): Aggregation[] {
    return aggregations?.map(x => Aggregation.fromJson(x, schema)) ?? [];
  }
}