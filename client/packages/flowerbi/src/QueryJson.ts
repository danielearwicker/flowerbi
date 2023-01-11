/**
 * The allowed filter comparison operators.
 */
export type FilterOperator = "=" | "<>" | ">" | "<" | ">=" | "<=" | "IN";

/**
 * The allowed value types for a filter.
 */
export type FilterValue = string | number | boolean | Date | unknown | string[] | number[];

/**
 * Specifies a filter criterion. The column is specified by a string of the 
 * form `table.column`.
 */
export interface FilterJson {
    column: string;
    operator: FilterOperator;
    value: FilterValue;
}

/**
 * The allowed aggregation function names.
 */
export type AggregationType = "Count" | "Sum" | "Avg" | "Min" | "Max";

/**
 * Describes an aggregated values. It is always taken from one column, with 
 * a function applied to it. Filters can optionally be supplied to limit
 * the set of rows included in the aggregation. The column is specified by a
 * string of the form `table.column`.
 */
export interface AggregationJson {
    function: AggregationType;
    column: string;
    filters?: FilterJson[];
}

/**
 * Specifies an ordering criteria: which column to sort by, and optionally
 * whether it is descending (the default is ascending). The column is 
 * specified by a string of the form `table.column`.
 */
export interface OrderingJson {
    column: string;
    descending?: boolean;
}

/**
 * Specifies an entire query.
 */
export interface QueryJson {
    /**
     * The plain columns to fetch, each specified by a string of the form `table.column`.
     */
    select?: string[];
    /**
     * The aggregated values to fetch.
     */
    aggregations: AggregationJson[];
    /**
     * Filters to apply. They are always combined with AND.
     */
    filters?: FilterJson[];
    /**
     * Ordering criteria to apply.
     */
    orderBy?: OrderingJson[];
    /**
     * See {@link Query.totals}.
     */
    totals?: boolean;
    /**
     * See {@link Query.skip}.
     */
    skip?: number;
    /**
     * See {@link Query.take}.
     */
    take?: number;
    /**
     * See {@link Query.comment}.
     */
    comment?: string;
    /**
     * See {@link Query.allowDuplicates}.
     */
    allowDuplicates?: boolean;
}
