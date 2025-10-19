import { CalculationJson, FlowerBIException, ISqlFormatter } from './types';

export class Calculation {
  constructor(private json: CalculationJson) {}

  private static allowedOperators = new Set(['+', '-', '*', '/', '??']);

  toSql(sql: ISqlFormatter, fetchAggValue: (index: number) => string): string {
    if (this.json.value !== undefined) {
      this.requireNulls(this.json.aggregation, this.json.first, this.json.second, this.json.operator);
      return `${this.json.value}`;
    }

    if (this.json.aggregation !== undefined) {
      this.requireNulls(this.json.value, this.json.first, this.json.second, this.json.operator);
      return fetchAggValue(this.json.aggregation);
    }

    if (this.json.first && this.json.second && this.json.operator) {
      this.requireNulls(this.json.aggregation, this.json.value);

      if (!Calculation.allowedOperators.has(this.json.operator)) {
        throw new FlowerBIException(`Operator '${this.json.operator}' not supported`);
      }

      const firstCalc = new Calculation(this.json.first);
      const secondCalc = new Calculation(this.json.second);
      
      const firstExpr = firstCalc.toSql(sql, fetchAggValue);
      const secondExpr = secondCalc.toSql(sql, fetchAggValue);

      if (this.json.operator === '/') {
        return sql.conditional(
          `${secondExpr} = 0`,
          '0',
          `${firstExpr} / ${sql.castToFloat(secondExpr)}`
        );
      }

      if (this.json.operator === '??') {
        return sql.conditional(`${firstExpr} is null`, secondExpr, firstExpr);
      }

      return `(${firstExpr} ${this.json.operator} ${secondExpr})`;
    }

    throw new FlowerBIException('Calculation does not specify enough properties');
  }

  compile(aggregations: (index: number) => () => number | null): () => number | null {
    if (this.json.value !== undefined) {
      this.requireNulls(this.json.aggregation, this.json.first, this.json.second, this.json.operator);
      const constant = this.json.value;
      return () => constant;
    }

    if (this.json.aggregation !== undefined) {
      this.requireNulls(this.json.value, this.json.first, this.json.second, this.json.operator);
      return aggregations(this.json.aggregation);
    }

    if (this.json.first && this.json.second && this.json.operator) {
      this.requireNulls(this.json.aggregation, this.json.value);

      const firstCalc = new Calculation(this.json.first);
      const secondCalc = new Calculation(this.json.second);
      
      const firstExpr = firstCalc.compile(aggregations);
      const secondExpr = secondCalc.compile(aggregations);

      switch (this.json.operator) {
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
          throw new FlowerBIException(`Operator '${this.json.operator}' not supported`);
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