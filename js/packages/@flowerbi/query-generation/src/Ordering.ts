import { OrderingJson, OrderingType, LabelledColumn, IColumn, Schema, FlowerBIException } from './types';

export class Ordering {
  public readonly Descending: boolean;
  public readonly Column: LabelledColumn | null;
  public readonly Index: number;
  public readonly SelectedIndex?: number;
  public readonly AggregatedIndex?: number;

  constructor(
    column: LabelledColumn | null,
    descending: boolean,
    index: number,
    selectedIndex?: number,
    aggregatedIndex?: number
  ) {
    this.Descending = descending;
    this.Column = column;
    this.Index = index;
    this.SelectedIndex = selectedIndex;
    this.AggregatedIndex = aggregatedIndex;
  }

  static fromIColumn(column: IColumn, descending: boolean, index: number): Ordering {
    return new Ordering({ JoinLabel: null, Value: column }, descending, index);
  }

  static defaultOrdering(): Ordering {
    return new Ordering(null, true, 0, undefined, 0);
  }

  static fromJson(
    json: OrderingJson,
    schema: Schema,
    selects = 0,
    values = 0,
    calcs = 0
  ): Ordering {
    const column = json.Column ? schema.GetColumn(json.Column) : null;
    
    let index = 0;
    let selectedIndex: number | undefined;
    let aggregatedIndex: number | undefined;

    if (json.Index !== undefined && json.Type !== undefined) {
      switch (json.Type) {
        case OrderingType.Select:
          if (json.Index < selects) {
            selectedIndex = json.Index;
            index = json.Index;
          } else {
            throw new FlowerBIException(`Ordering index ${json.Index} is out of range in ${json.Type}`);
          }
          break;
        case OrderingType.Value:
          if (json.Index < values) {
            aggregatedIndex = json.Index;
            index = json.Index + selects;
          } else {
            throw new FlowerBIException(`Ordering index ${json.Index} is out of range in ${json.Type}`);
          }
          break;
        case OrderingType.Calculation:
          if (json.Index < calcs) {
            aggregatedIndex = values + json.Index;
            index = json.Index + selects + values;
          } else {
            throw new FlowerBIException(`Ordering index ${json.Index} is out of range in ${json.Type}`);
          }
          break;
      }
    }

    return new Ordering(column, json.Descending, index, selectedIndex, aggregatedIndex);
  }

  static Load(
    orderings: OrderingJson[] | undefined,
    schema: Schema,
    selects = 0,
    values = 0,
    calcs = 0
  ): Ordering[] {
    return orderings?.map(x => Ordering.fromJson(x, schema, selects, values, calcs)) ?? [];
  }

  get Direction(): string {
    return this.Descending ? 'desc' : 'asc';
  }
}