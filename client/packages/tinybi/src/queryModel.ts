import { keysOf } from "./arrayHelpers";
import { QueryColumn } from "./QueryColumn";
import { AggregationJson, QueryJson } from "./QueryJson";

// Queries can select plain columns, or aggregation functions on columns
export type SelectMember = QueryColumn<any> | AggregationJson;

// Each such member is given a name
export type QuerySelect = Record<string, SelectMember>;

// The data type of a column can be extracted from it
export type QueryColumnType<T> = T extends QueryColumn<infer C> ? C : never;

// The same names are used in the output, with the data type of plain columns, or number for aggregations
export type ExpandedQueryRecord<S extends QuerySelect> = { 
    [P in keyof S]: S[P] extends QueryColumn<any> ? QueryColumnType<S[P]> : number;
};

export type ExpandedQueryRecordWithOptionalColumns<S extends QuerySelect> = { 
    [P in keyof S]: S[P] extends QueryColumn<any> ? (QueryColumnType<S[P]> | undefined) : number;
};

export type AggregatePropsOnlyHelper<T extends QuerySelect> = {
    [K in keyof T]: T[K] extends QueryColumn<any> ? never : K;
};

// The names of properties from T that refer to aggregate values
export type AggregatePropsOnly<T extends QuerySelect> = AggregatePropsOnlyHelper<T>[keyof AggregatePropsOnlyHelper<T>];

export function getAggregatePropsOnly<T extends QuerySelect>(select: T) {
    const keys = keysOf(select).filter(x => !(select[x] instanceof QueryColumn))
    return keys as AggregatePropsOnly<T>[];
}

// A record including only the properties from T that refer to aggregate values
export type AggregatesOnly<T extends QuerySelect> = {
    [K in AggregatePropsOnly<T>]: T[K]
};

// A record including only the aggregate values from T
export type AggregateValuesOnly<T extends QuerySelect> = {
    [K in AggregatePropsOnly<T>]: number;
};

export type ColumnPropsOnlyHelper<T extends QuerySelect> = {
    [K in keyof T]: T[K] extends QueryColumn<any> ? K : never;
};

// The names of properties from T that refer to plain columns
export type ColumnPropsOnly<T extends QuerySelect> = ColumnPropsOnlyHelper<T>[keyof ColumnPropsOnlyHelper<T>];

export function getColumnPropsOnly<T extends QuerySelect>(select: T) {
    const keys = keysOf(select).filter(x => select[x] instanceof QueryColumn)
    return keys as ColumnPropsOnly<T>[];
}

// A record including only the properties from T that refer to plain columns
export type ColumnsOnly<T extends QuerySelect> = {
    [K in ColumnPropsOnly<T>]: T[K]
};

export function getColumnsOnly(select: QuerySelect) {
    return keysOf(select)
        .map(k => select[k])
        .filter(x => x instanceof QueryColumn)
        .map(x => (x as QueryColumn<any>));
}

// A record including only the aggregate values from T
export type ColumnValuesOnly<T extends QuerySelect> = {
    [K in ColumnPropsOnly<T>]: QueryColumnType<T[K]>;
};

/**
 * Strongly-typed query definition: use jsonifyQuery to convert to the JSON format 
 * and expandQueryResult to generate corresponding output records.
 */
export interface Query<S extends QuerySelect> extends Omit<QueryJson, "select"|"aggregations"> {
    select: S;
}
