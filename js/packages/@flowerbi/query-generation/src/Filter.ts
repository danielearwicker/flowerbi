import { FilterJson, LabelledColumn, IColumn, Schema, FlowerBIException } from './types';

export class Filter {
  public readonly Column: LabelledColumn;
  public readonly Operator: string;
  public readonly Value: any;
  public readonly Constant?: any;

  private static readonly allowedOperators = new Set([
    '=',
    '<>',
    '!=',
    '>',
    '<',
    '>=',
    '<=',
    'IN',
    'NOT IN',
    'BITS IN',
    'LIKE',
  ]);

  private static readonly basicValueTypes = new Set([
    'boolean',
    'number',
    'string',
    'object', // for Date objects
  ]);

  constructor(column: LabelledColumn, operator: string, value: any, constant?: any) {
    this.Column = column;
    this.Operator = this.checkOperator(operator);
    this.Value = value;
    this.Constant = constant;
  }

  static fromJson(json: FilterJson, schema: Schema): Filter {
    return new Filter(
      schema.GetColumn(json.Column),
      json.Operator,
      this.unpackAndValidateValue(json.Value),
      this.unpackAndValidateValue(json.Constant)
    );
  }

  static fromIColumn(column: IColumn, operator: string, value: any, constant?: any): Filter {
    return new Filter({ JoinLabel: null, Value: column }, operator, value, constant);
  }

  static Load(filters: FilterJson[] | undefined, schema: Schema): Filter[] {
    return filters?.map(x => Filter.fromJson(x, schema)) ?? [];
  }

  private static validateBasicType(value: any): void {
    if (value === null || value === undefined) return;
    
    const type = typeof value;
    if (value instanceof Date) return;
    if (!this.basicValueTypes.has(type)) {
      throw new FlowerBIException('Unsupported filter value');
    }
  }

  private static unpackAndValidateValue(json: any): any {
    if (json === null || json === undefined) return null;

    if (Array.isArray(json)) {
      if (json.length === 0) {
        throw new FlowerBIException('Filter JSON contains empty array');
      }
      json.forEach(item => this.validateBasicType(item));
      return json;
    }

    this.validateBasicType(json);
    return json;
  }

  private checkOperator(op: string): string {
    if (!Filter.allowedOperators.has(op)) {
      throw new FlowerBIException(`${op} is not an allowed operator`);
    }
    return op;
  }

  public compile(): (value: any) => boolean {
    switch (this.Operator) {
      case '=':
        return (value: any) => value === this.Value;
      case '<>':
      case '!=':
        return (value: any) => value !== this.Value;
      case '>':
        return (value: any) => value > this.Value;
      case '<':
        return (value: any) => value < this.Value;
      case '>=':
        return (value: any) => value >= this.Value;
      case '<=':
        return (value: any) => value <= this.Value;
      case 'IN':
        return (value: any) => (this.Value as any[]).includes(value);
      case 'NOT IN':
        return (value: any) => !(this.Value as any[]).includes(value);
      case 'BITS IN':
        const mask = Number(this.Constant);
        const target = Number(this.Value);
        return (value: any) => (Number(value) & mask) === target;
      case 'LIKE':
        const str = String(this.Value);
        return (value: any) => String(value).includes(str);
      default:
        throw new FlowerBIException(`Unsupported operator: ${this.Operator}`);
    }
  }
}