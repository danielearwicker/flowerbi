import { Query, QuerySelect, ExpandedQueryRecord, AggregateValuesOnly, getAggregatePropsOnly, getColumnPropsOnly } from "./queryModel";
import { QueryJson, AggregationJson } from "./QueryJson";
import { QueryColumn } from "./QueryColumn";

/**
 * The allowed data types for plain columns.
 */
export type QuerySelectValue = number|string|Date|boolean;

/**
 * The JSON format of a record returned from the API when executing a query.
 */
export interface QueryRecordJson {
    /**
     * The plain column values.
     */
    selected: QuerySelectValue[];
    /**
     * The aggregated column values.
     */
    aggregated: number[];
}

/**
 * The JSON format of the whole payload returned from the API when 
 * executing a query.
 */
export interface QueryResultJson {
    /**
     * The records of the query result.
     */
    records: QueryRecordJson[];

    /**
     * Optional extra record, only available if {@link QueryJson.totals}
     * was specified as `true` in the query, containing the aggregation
     * totals.
     */
    totals?: QueryRecordJson;
}

/**
 * The function you need to implement to pass {@link QueryJson} payloads to
 * your API and get them executed. This will typically be a wrapper around
 * the `fetch` browser API, or something more high-level, and can make use
 * of whatever authentication method you prefer.
 */
export type QueryFetch = (queryJson: QueryJson) => Promise<QueryResultJson>;

/**
 * Converts a statically-typed {@link Query} into the {@link QueryJson}
 * format, ready to be sent to your API.
 * @param query 
 */
export function jsonifyQuery<S extends QuerySelect>(query: Query<S>): QueryJson {
    const { select, ...others } = query;

    return {
        ...others,
        select: getColumnPropsOnly(select).map(key => (select[key] as QueryColumn<never>).name),
        aggregations: getAggregatePropsOnly(select).map(key => select[key] as AggregationJson)
    };
}

/**
 * Converts the `QueryRecordJson` for a single record into a strongly-typed record
 * with named properties, using the {@link Query.select} from the query to perform
 * the necessary mapping.
 * @param select The {@link Query.select} property from the query.
 * @param record The record returned from your API.
 */
export function expandQueryRecord<S extends QuerySelect>(
    select: S,
    record: QueryRecordJson
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

/**
 * Converts the `QueryRecordJson` from the `totals` record into a strongly-typed 
 * record named properties for the aggregated values only, using the
 * {@link Query.select} from the query to perform the necessary mapping.
 * 
 * @param select The {@link Query.select} property from the query.
 * @param record The {@link QueryResultJson.totals} record returned from your API.
 */
export function getAggregateValuesOnly<S extends QuerySelect>(
    select: S,
    record: QueryRecordJson
): AggregateValuesOnly<S> {

    const result: any = {};

    let n = 0;
    for (const key of getAggregatePropsOnly(select)) {
        result[key] = record.aggregated[n++];
    }

    return result;
}

/**
 * The statically-typed result of a {@link Query}.
 */
export interface ExpandedQueryResult<S extends QuerySelect> {
    /**
     * The set of records returned, each having named properties
     * corresponding to the plain and aggregated columns selected
     * in the query.
     */
    records: ExpandedQueryRecord<S>[];
    /**
     * Optional extra record, only available if {@link QueryJson.totals}
     * was specified as `true` in the query, containing the aggregation
     * totals.
     */
    totals?: AggregateValuesOnly<S>;
}

/**
 * Converts the payload returned from the API into the statically-typed
 * form appropriate for the query.
 * @param select The {@link Query.select} property from the query.
 * @param result The response payload from the API call.
 */
export function expandQueryResult<S extends QuerySelect>(
    select: S,
    result: QueryResultJson
): ExpandedQueryResult<S> {

    return {
        records: result.records.map(r => expandQueryRecord(select, r)),
        totals: result.totals && getAggregateValuesOnly(select, result.totals)
    };
}
