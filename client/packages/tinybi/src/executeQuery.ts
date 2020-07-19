import { Query, QuerySelect, ExpandedQueryRecord, AggregateValuesOnly, getAggregatePropsOnly, getColumnPropsOnly } from "./queryModel";
import { QueryJson, AggregationJson } from "./QueryJson";
import { QueryColumn } from "./QueryColumn";

export type QuerySelectValue = number|string|Date|boolean;

export interface QueryRecord {
    selected: QuerySelectValue[];
    aggregated: number[];
}

export interface QueryResult {
    records: QueryRecord[];

    // Optional extra row with actual totals, not necessarily
    // same as summing the returned rows (which may be truncated)
    totals?: QueryRecord;
}

export type QueryFetch = (queryJson: string) => Promise<QueryResult>;

export function jsonifyQuery<S extends QuerySelect>(query: Query<S>): QueryJson {
    const { select, ...others } = query;

    return {
        ...others,
        select: getColumnPropsOnly(select).map(key => (select[key] as QueryColumn<never>).name),
        aggregations: getAggregatePropsOnly(select).map(key => select[key] as AggregationJson)
    };
}

export function expandQueryRecord<S extends QuerySelect>(
    select: S,
    record: QueryRecord
): ExpandedQueryRecord<S> {
    
    const result: any = {};

    let n = 0;
    for (const key of getAggregatePropsOnly(select)) {
        result[key] = record.aggregated[n++];
    }

    n = 0;
    for (const key of getColumnPropsOnly(select)) {
        result[key] = record.selected[n++];
    }

    return result;
}

export function getAggregateValuesOnly<S extends QuerySelect>(
    select: S,
    record: QueryRecord
): AggregateValuesOnly<S> {

    const result: any = {};

    let n = 0;
    for (const key of getAggregatePropsOnly(select)) {
        result[key] = record.aggregated[n++];
    }

    return result;
}

export interface ExpandedQueryResult<S extends QuerySelect> {
    records: ExpandedQueryRecord<S>[];
    totals?: AggregateValuesOnly<S>;
}

export function expandQueryResult<S extends QuerySelect>(
    select: S,
    result: QueryResult
): ExpandedQueryResult<S> {

    return {
        records: result.records.map(r => expandQueryRecord(select, r)),
        totals: result.totals && getAggregateValuesOnly(select, result.totals)
    };
}

export function executeQuery(fetch: QueryFetch, query: QueryJson) {
    return fetch(JSON.stringify(query));
}
