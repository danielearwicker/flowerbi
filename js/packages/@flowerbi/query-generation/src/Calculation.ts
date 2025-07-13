import { CalculationJson, FlowerBIException, ISqlFormatter } from './types';

export class Calculation {
  constructor(private json: CalculationJson) {}

  private static allowedOperators = new Set(['+', '-', '*', '/', '??']);

  toSql(sql: ISqlFormatter, fetchAggValue: (index: number) => string): string {
    if (this.json.Value !== undefined) {
      this.requireNulls(this.json.Aggregation, this.json.First, this.json.Second, this.json.Operator);
      return `${this.json.Value}`;
    }

    if (this.json.Aggregation !== undefined) {
      this.requireNulls(this.json.Value, this.json.First, this.json.Second, this.json.Operator);
      return fetchAggValue(this.json.Aggregation);
    }

    if (this.json.First && this.json.Second && this.json.Operator) {
      this.requireNulls(this.json.Aggregation, this.json.Value);

      if (!Calculation.allowedOperators.has(this.json.Operator)) {
        throw new FlowerBIException(`Operator '${this.json.Operator}' not supported`);
      }

      const firstCalc = new Calculation(this.json.First);
      const secondCalc = new Calculation(this.json.Second);
      
      const firstExpr = firstCalc.toSql(sql, fetchAggValue);
      const secondExpr = secondCalc.toSql(sql, fetchAggValue);

      if (this.json.Operator === '/') {
        return sql.Conditional(
          `${secondExpr} = 0`,
          '0',
          `${firstExpr} / ${sql.CastToFloat(secondExpr)}`
        );
      }

      if (this.json.Operator === '??') {
        return sql.Conditional(`${firstExpr} is null`, secondExpr, firstExpr);
      }

      return `(${firstExpr} ${this.json.Operator} ${secondExpr})`;
    }

    throw new FlowerBIException('Calculation does not specify enough properties');
  }

  compile(aggregations: (index: number) => () => number | null): () => number | null {
    if (this.json.Value !== undefined) {
      this.requireNulls(this.json.Aggregation, this.json.First, this.json.Second, this.json.Operator);
      const constant = this.json.Value;
      return () => constant;
    }

    if (this.json.Aggregation !== undefined) {
      this.requireNulls(this.json.Value, this.json.First, this.json.Second, this.json.Operator);
      return aggregations(this.json.Aggregation);
    }

    if (this.json.First && this.json.Second && this.json.Operator) {
      this.requireNulls(this.json.Aggregation, this.json.Value);

      const firstCalc = new Calculation(this.json.First);
      const secondCalc = new Calculation(this.json.Second);
      
      const firstExpr = firstCalc.compile(aggregations);
      const secondExpr = secondCalc.compile(aggregations);

      switch (this.json.Operator) {
        case '+':
          return () => {
            const first = firstExpr();
            const second = secondExpr();
            return first !== null && second !== null ? first + second : null;
          };
        case '-':
          return () => {
            const first = firstExpr();
            const second = secondExpr();
            return first !== null && second !== null ? first - second : null;
          };
        case '*':
          return () => {
            const first = firstExpr();
            const second = secondExpr();
            return first !== null && second !== null ? first * second : null;
          };
        case '/':
          return () => {
            const first = firstExpr();
            const second = secondExpr();
            return first !== null && second !== null && second !== 0 ? first / second : null;
          };
        case '??':
          return () => {
            const first = firstExpr();
            return first !== null ? first : secondExpr();
          };
        default:
          throw new FlowerBIException(`Operator '${this.json.Operator}' not supported`);
      }
    }

    throw new FlowerBIException('Calculation does not specify enough properties');
  }

  private requireNulls(...values: any[]): void {
    if (values.some(x => x !== null && x !== undefined)) {
      throw new FlowerBIException('Calculation has too many properties in same object');
    }
  }
}