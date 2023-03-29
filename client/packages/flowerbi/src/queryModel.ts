import { keysOf } from "./arrayHelpers";
import { QueryColumn } from "./QueryColumn";
import { AggregationJson, FilterJson, OrderingJson } from "./QueryJson";

/**
 * Defines the kinds of member that can appear in the `select` object of a query.
 * Queries can select plain columns, or aggregation functions on columns.
 */
export type SelectMember = QueryColumn<any> | AggregationJson;

/**
 * The `select` object of a query has named properties of type {@link SelectMember}.
 */
export type QuerySelect = Record<string, SelectMember>;

/**
 * Extracts the data type from a column. So `QueryColumnType<QueryColumn<boolean>>` is `boolean`.
 */
export type QueryColumnType<T> = T extends QueryColumn<infer C> ? C : never;

/**
 * Defines the shape of a record returned from a query, based on its `select` object.
 * Each selected property appears as a property in the record with the same name. For
 * plain columns the data type depends on the column definition, but for aggregations
 * and calculations the data type is always `number`.
 */
export type ExpandedQueryRecord<S extends QuerySelect, C extends QueryCalculations<S>> = {
    [P in keyof S]: S[P] extends QueryColumn<any> ? QueryColumnType<S[P]> : number;
} & {
    [P in keyof C]: number;
};

/**
 * Similar to {@link ExpandedQueryRecord} but the plain columns are optional, so they
 * may be `undefined`. Aggregations/calculations are not optional.
 */
export type ExpandedQueryRecordWithOptionalColumns<S extends QuerySelect, C extends QueryCalculations<S>> = {
    [P in keyof S]: S[P] extends QueryColumn<any> ? QueryColumnType<S[P]> | undefined : number;
} & {
    [P in keyof C]: number;
};

/**
 * A helper type used in the definition of {@link AggregatePropsOnly}.
 */
export type AggregatePropsOnlyHelper<T extends QuerySelect> = {
    [K in keyof T]: T[K] extends QueryColumn<any> ? never : K;
};

/**
 * The names of properties from a `select` object that refer to aggregated values.
 * Compare with {@link ColumnPropsOnly}.
 */
export type AggregatePropsOnly<T extends QuerySelect> = AggregatePropsOnlyHelper<T>[keyof AggregatePropsOnlyHelper<T>];

/**
 * Returns the names of properties in a query that refer to aggregated columns.
 * The result is an array of strings, but type-constrained to string literal types:
 *
 * ```ts
 * getAggregatePropsOnly({
 *    customer: Customer.Name,
 *    spend: Invoice.Amount.sum()
 * }) // ["spend"]
 * ```
 *
 * @param select the `select` object from a query
 */
export function getAggregatePropsOnly<T extends QuerySelect>(select: T) {
    const keys = keysOf(select).filter((x) => !(select[x] instanceof QueryColumn));
    return keys as AggregatePropsOnly<T>[];
}

/**
 * An object that contains a subset of the the named properties from a query's
 * `select` object, those that refer to aggregated values (hence all are of
 * type `number`.)
 */
export type AggregateValuesOnly<T extends QuerySelect> = {
    [K in AggregatePropsOnly<T>]: number;
};

export type CalculationValues<C extends QueryCalculations<any>> = {
    [K in keyof C]: number;
};

/**
 * A helper type used in the definition of {@link ColumnPropsOnly}.
 */
export type ColumnPropsOnlyHelper<T extends QuerySelect> = {
    [K in keyof T]: T[K] extends QueryColumn<any> ? K : never;
};

/**
 * The names of properties from a `select` object that refer to plain columns.
 * Compare with {@link AggregatePropsOnly}.
 */
export type ColumnPropsOnly<T extends QuerySelect> = ColumnPropsOnlyHelper<T>[keyof ColumnPropsOnlyHelper<T>];

/**
 * Returns the names of properties in a query that refer to plain columns. The
 * result is an array of strings, but type-constrained to string literal types:
 *
 * ```ts
 * getColumnPropsOnly({
 *    customer: Customer.Name,
 *    spend: Invoice.Amount.sum()
 * }) // ["customer"]
 * ```
 *
 * @param select the `select` object from a query
 */
export function getColumnPropsOnly<T extends QuerySelect>(select: T) {
    const keys = keysOf(select).filter((x) => select[x] instanceof QueryColumn);
    return keys as ColumnPropsOnly<T>[];
}

/**
 * Returns the plain column objects referred to in a query, ignoring
 * aggregated columns.
 * @param select the `select` object from a query
 */
export function getColumnsOnly(select: QuerySelect) {
    return keysOf(select)
        .map((k) => select[k])
        .filter((x) => x instanceof QueryColumn)
        .map((x) => x as QueryColumn<any>);
}

export type Calculation<S extends QuerySelect> = number | AggregatePropsOnly<S> | [Calculation<S>, "+" | "-" | "*" | "/", Calculation<S>];

/**
 * The `select` object of a query has named properties of type {@link SelectMember}.
 */
export type QueryCalculations<S extends QuerySelect> = Record<string, Calculation<S>>;

export type Ordering<S extends QuerySelect, C extends QueryCalculations<S>> = { descending?: boolean } & (
    | {
          select: keyof S;
      }
    | {
          calculation: keyof C;
      }
);

/**
 * Strongly-typed query definition: use {@link jsonifyQuery} to convert to the JSON format
 * and {@link expandQueryResult} to generate corresponding output records.
 */
export interface Query<S extends QuerySelect, C extends QueryCalculations<S>> {
    select: S;
    filters?: FilterJson[];
    calculations?: C;
    /**
     * Ordering criteria to apply.
     */
    orderBy?: (OrderingJson | Ordering<S, C>)[];
    /**
     * Specifies whether to return a `totals` property containing the
     * aggregation values across the whole dataset, e.g. if the `select`
     * object is:
     *
     * ```ts
     * {
     *    customer: Customer.Name,
     *    spend: Invoice.Amount.sum()
     * }
     * ```
     *
     * the returned `records` will have properties `customer` and `spend`,
     * one per customer. Specify `totals: true` to also get a `totals` object
     * with only a `spend` property, containing the total spend across all
     * customers.
     */
    totals?: boolean;
    /**
     * Number of result records to skip before the first record returned.
     */
    skip?: number;
    /**
     * Number of result records to return.
     */
    take?: number;
    /**
     * A string to insert in a comment at the start of the generated SQL.
     *
     * This will be aggressively processed to remove the danger of injection
     * attacks, so anything other than alpha, numeric, new line or CR
     * characters will be replaced with space.
     */
    comment?: string;
    /**
     * Only applicable if the query specifies only ordinary columns, no
     * aggregations. If true, no GROUP BY clause is added to the SQL, so if
     * multiple results have the same values they will appear repeatedly in
     * the output. This can greatly reduce the work required by the SQL
     * engine, and so should be specified if duplicate rows are tolerable or
     * are known to be impossible.
     */
    allowDuplicates?: boolean;
}
