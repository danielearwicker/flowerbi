import { FilterValue, AggregationType, FilterJson, AggregationJson, FilterOperator, OrderingJson } from "./QueryJson";

/**
 * A column from the schema, with a name and a data type. A whole schema of
 * such declared columns can be auto-generated using the CLI.
 */
export class QueryColumn<T extends FilterValue> {
    /**
     * @param name The name, of the form `table.column`.
     */
    constructor(public readonly name: string) {}

    protected aggregation(aggregationType: AggregationType, filters?: FilterJson[]): AggregationJson {
        return {
            column: this.name,
            function: aggregationType,
            filters,
        };
    }

    /**
     * Aggregates the column by counting values.
     * @param filters Optional filters to apply.
     */
    count(filters?: FilterJson[]) {
        return this.aggregation("Count", filters);
    }

    /**
     * Aggregates the column by counting distinct values.
     * @param filters Optional filters to apply.
     */
    countDistinct(filters?: FilterJson[]) {
        return this.aggregation("CountDistinct", filters);
    }

    /**
     * Aggregates the column by selecting the minimum value.
     * @param filters Optional filters to apply.
     */
    min(filters?: FilterJson[]) {
        return this.aggregation("Min", filters);
    }

    /**
     * Aggregates the column by selecting the maximum value.
     * @param filters Optional filters to apply.
     */
    max(filters?: FilterJson[]) {
        return this.aggregation("Max", filters);
    }

    protected filter(operator: FilterOperator, value: T): FilterJson {
        return {
            column: this.name,
            operator,
            value,
        };
    }

    /**
     * Sorts by the column in ascending order.
     */
    ascending(): OrderingJson {
        return { column: this.name, descending: false };
    }

    /**
     * Sorts by the column in descending order.
     */
    descending(): OrderingJson {
        return { column: this.name, descending: true };
    }

    /**
     * Produces a filter that requires this column to be equal to some value.
     */
    equalTo(value: T) {
        return this.filter("=", value);
    }

    /**
     * Produces a filter that requires this column to be not equal to some value.
     */
    notEqualTo(value: T) {
        return this.filter("<>", value);
    }

    /**
     * Produces a filter that requires this column to be greater than to some
     * value.
     */
    greaterThan(value: T) {
        return this.filter(">", value);
    }

    /**
     * Produces a filter that requires this column to be less than to some value.
     */
    lessThan(value: T) {
        return this.filter("<", value);
    }

    /**
     * Produces a filter that requires this column to be greater than or equal to
     * some value.
     */
    greaterThanOrEqualTo(value: T) {
        return this.filter(">=", value);
    }

    /**
     * Produces a filter that requires this column to be less than or equal to
     * some value.
     */
    lessThanOrEqualTo(value: T) {
        return this.filter("<=", value);
    }

    /**
     * Produces a filter that requires this column's value to appear in the list.
     * Only supported for number or string columns.
     */
    in(value: T extends number | string ? T[] : never): FilterJson {
        return {
            column: this.name,
            operator: "IN",
            value,
        };
    }

    /**
     * Produces a filter that requires this column's value to not appear in the list.
     * Only supported for number or string columns.
     */
    notIn(value: T extends number | string ? T[] : never): FilterJson {
        return {
            column: this.name,
            operator: "IN",
            value,
        };
    }
}

export class NumericQueryColumn<T extends number | null = number> extends QueryColumn<T> {
    /**
     * @param name The name, of the form `table.column`.
     */
    constructor(public readonly name: string) {
        super(name);
    }

    /**
     * Aggregates the column by summing values.
     * @param filters Optional filters to apply.
     */
    sum(filters?: FilterJson[]) {
        return this.aggregation("Sum", filters);
    }

    /**
     * Aggregates the column by averaging values.
     * @param filters Optional filters to apply.
     */
    avg(filters?: FilterJson[]) {
        return this.aggregation("Avg", filters);
    }
}

export class IntegerQueryColumn<T extends number | null = number> extends NumericQueryColumn<T> {
    /**
     * @param name The name, of the form `table.column`.
     */
    constructor(public readonly name: string) {
        super(name);
    }

    bitsOn(value: NonNullable<T>) {
        return this.filter("BITS ON", value);
    }

    bitsOff(value: NonNullable<T>) {
        return this.filter("BITS OFF", value);
    }
}
