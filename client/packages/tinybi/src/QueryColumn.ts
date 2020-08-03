import { 
    FilterValue, 
    AggregationType, 
    FilterJson, 
    AggregationJson, 
    FilterOperator, 
    OrderingJson 
} from "./QueryJson";

/**
 * A column from the schema, with a name and a data type. A whole schema of
 * such declared columns can be auto-generated using the CLI.
 */
export class QueryColumn<T extends FilterValue> {

    /** 
     * @param name The name, of the form `table.column`.
     */
    constructor(public readonly name: string) {}

    private aggregation(aggregationType: AggregationType, filters?: FilterJson[]): AggregationJson {
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
        return this.aggregation("count", filters);
    }

    /**
     * Aggregates the column by summing values.
     * @param filters Optional filters to apply.
     */
    sum(filters?: FilterJson[]) {
        return this.aggregation("sum", filters);
    }

    private filter(operator: FilterOperator, value: T): FilterJson {
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
}
