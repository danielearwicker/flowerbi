// This is the wire protocol, matches the TinyBI.Engine declarations

export type AggregationType = "none" | "count" | "sum";

export interface AggregationJson {
    function: AggregationType;
    column: string;
    filters?: FilterJson[];
}

export interface OrderingJson {
    column: string;
    descending?: boolean;
}

export interface QueryJson {
    select?: string[];
    aggregations: AggregationJson[];
    filters?: FilterJson[];
    orderBy?: OrderingJson[];
    totals?: boolean;
}

export type FilterOperator = "=" | "<>" | ">" | "<" | ">=" | "<=";

export type FilterValue = string | number | boolean | Date | unknown;

export interface FilterJson {
    column: string;
    operator: FilterOperator;
    value: FilterValue;
}
