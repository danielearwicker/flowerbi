export type AggregationType = "none" | "count" | "sum";

export interface AggregationJson {
    function: AggregationType;
    column: string;
    filters?: FilterJson[];
}

export interface QueryJson {
    select?: string[];
    aggregations: AggregationJson[];
    filters?: FilterJson[];
    totals?: boolean;
}

export type FilterOperator = "=" | "<>" | ">" | "<" | ">=" | "<=";

export type FilterValue = string | number | boolean | Date;

export interface FilterJson {
    column: string;
    operator: FilterOperator;
    value: FilterValue;
}

export interface Query extends Omit<QueryJson, "select"> {
    select?: QueryColumn<any>[];    
}

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
