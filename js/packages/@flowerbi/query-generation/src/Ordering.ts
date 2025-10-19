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
    return new Ordering({ joinLabel: null, value: column }, descending, index);
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
    const column = json.column ? schema.getColumn(json.column) : null;
    
    let index = 0;
    let selectedIndex: number | undefined;
    let aggregatedIndex: number | undefined;

    if (json.index !== undefined && json.type !== undefined) {
      switch (json.type) {
        case OrderingType.Select:
          if (json.index < selects) {
            selectedIndex = json.index;
            index = json.index;
          } else {
            throw new FlowerBIException(`Ordering index ${json.index} is out of range in ${json.type}`);
          }
          break;
        case OrderingType.Value:
          if (json.index < values) {
            aggregatedIndex = json.index;
            index = json.index + selects;
          } else {
            throw new FlowerBIException(`Ordering index ${json.index} is out of range in ${json.type}`);
          }
          break;
        case OrderingType.Calculation:
          if (json.index < calcs) {
            aggregatedIndex = values + json.index;
            index = json.index + selects + values;
          } else {
            throw new FlowerBIException(`Ordering index ${json.index} is out of range in ${json.type}`);
          }
          break;
      }
    }

    return new Ordering(column, json.descending, index, selectedIndex, aggregatedIndex);
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