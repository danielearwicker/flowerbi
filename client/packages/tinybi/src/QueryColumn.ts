import { 
    FilterValue, 
    AggregationType, 
    FilterJson, 
    AggregationJson, 
    FilterOperator, 
    OrderingJson 
} from "./QueryJson";

export class QueryColumn<T extends FilterValue> {
    constructor(public readonly name: string) {}

    private aggregation(aggregationType: AggregationType, filters?: FilterJson[]): AggregationJson {
        return {
            column: this.name,
            function: aggregationType,
            filters,
        };
    }

    count(filters?: FilterJson[]) {
        return this.aggregation("count", filters);
    }

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

    ascending(): OrderingJson {
        return { column: this.name, descending: false };
    }

    descending(): OrderingJson {
        return { column: this.name, descending: true };
    }

    equalTo(value: T) {
        return this.filter("=", value);
    }

    notEqualTo(value: T) {
        return this.filter("<>", value);
    }

    greaterThan(value: T) {
        return this.filter(">", value);
    }

    lessThan(value: T) {
        return this.filter("<", value);
    }

    greaterThanOrEqualTo(value: T) {
        return this.filter(">=", value);
    }

    lessThanOrEqualTo(value: T) {
        return this.filter("<=", value);
    }
}
