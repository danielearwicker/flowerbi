/**
 * The allowed filter comparison operators.
 */
export type FilterOperator = "=" | "<>" | ">" | "<" | ">=" | "<=" | "IN" | "NOT IN" | "BITS ON" | "BITS OFF";

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
export type AggregationType = "Count" | "Sum" | "Avg" | "Min" | "Max" | "CountDistinct";

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
 * whether it is descending (the default is ascending). The column can be
 * specified by a string of the form `table.column`, though this can only
 * target one of the columns specified in select. More flexibly, specify
 * a type of column (the type 'Value' refers to aggregations) and its
 * zero-based position.
 */
export type OrderingJson = {
    descending?: boolean;
} & (
    | {
          column: string;
      }
    | {
          type: "Select" | "Value" | "Calculation";
          index: number;
      }
);

/**
 * Specifies an expression for calculating a derived value based on
 * the values of aggregations, specified by zero-based position.
 */
export type CalculationJson =
    | { value: number }
    | { aggregation: number }
    | {
          first: CalculationJson;
          operator: "+" | "-" | "*" | "/" | "??";
          second: CalculationJson;
      };

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
     * The calculations to perform.
     */
    calculations?: CalculationJson[];
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
    /**
     * See {@link Query.fullJoins}.
     */
    fullJoins?: boolean;
}
