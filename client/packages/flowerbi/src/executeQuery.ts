import {
    Query,
    QuerySelect,
    ExpandedQueryRecord,
    AggregateValuesOnly,
    getAggregatePropsOnly,
    getColumnPropsOnly,
    Calculation,
    AggregatePropsOnly,
    QueryCalculations,
    Ordering,
    CalculationValues,
} from "./queryModel";
import { QueryJson, AggregationJson, CalculationJson, OrderingJson } from "./QueryJson";
import { QueryColumn } from "./QueryColumn";
import { keysOf } from "./arrayHelpers";

/**
 * The allowed data types for plain columns.
 */
export type QuerySelectValue = number | string | Date | boolean;

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

function jsonifyCalculation<S extends QuerySelect>(calculation: Calculation<S>, props: AggregatePropsOnly<S>[]): CalculationJson {
    if (typeof calculation === "string") {
        const aggregation = props.indexOf(calculation);
        if (aggregation === -1) {
            throw new Error(`Not a valid aggregation name: ${calculation}`);
        }
        return { aggregation };
    }
    if (typeof calculation === "number") {
        return {
            value: calculation,
        };
    }
    if (calculation instanceof Array) {
        return {
            first: jsonifyCalculation(calculation[0], props),
            operator: calculation[1],
            second: jsonifyCalculation(calculation[2], props),
        };
    }
    throw new Error("Invalid calculation");
}

function jsonifyOrdering<S extends QuerySelect, C extends QueryCalculations<S>>(
    ordering: OrderingJson | Ordering<S, C>,
    selects: string[],
    values: string[],
    calculations: string[]
): OrderingJson {
    if ("select" in ordering) {
        let index = selects.indexOf(ordering.select as string);
        if (index !== -1) {
            return {
                type: "Select",
                index,
                descending: ordering.descending,
            };
        }
        index = values.indexOf(ordering.select as string);
        if (index !== -1) {
            return {
                type: "Value",
                index,
                descending: ordering.descending,
            };
        }
        throw new Error(`Invalid ordering select key: ${String(ordering.select)}`);
    }
    if ("calculation" in ordering) {
        let index = calculations.indexOf(ordering.calculation as string);
        if (index !== -1) {
            return {
                type: "Calculation",
                index,
                descending: ordering.descending,
            };
        }
        throw new Error(`Invalid ordering calculation key: ${String(ordering.calculation)}`);
    }
    return ordering;
}

/**
 * Converts a statically-typed {@link Query} into the {@link QueryJson}
 * format, ready to be sent to your API.
 * @param query
 */
export function jsonifyQuery<S extends QuerySelect, C extends QueryCalculations<S>>(query: Query<S, C>): QueryJson {
    const { select, filters, calculations, orderBy, totals, take, skip, comment, allowDuplicates } = query;

    const columnProps = getColumnPropsOnly(select);
    const aggregationProps = getAggregatePropsOnly(select);
    const calculationProps = calculations ? keysOf(calculations) : [];

    return {
        select: columnProps.map((key) => (select[key] as QueryColumn<never>).name),
        aggregations: aggregationProps.map((key) => select[key] as AggregationJson),
        calculations: calculations ? calculationProps.map((key) => jsonifyCalculation(calculations[key] as Calculation<S>, aggregationProps)) : undefined,
        filters: filters ?? [],
        orderBy: orderBy?.map((o) => jsonifyOrdering(o, columnProps as string[], aggregationProps as string[], calculationProps as string[])) ?? [],
        totals: totals ?? false,
        skip: skip ?? 0,
        take: take ?? 100,
        comment,
        allowDuplicates,
    };
}

/**
 * Converts the `QueryRecordJson` for a single record into a strongly-typed record
 * with named properties, using the {@link Query.select} from the query to perform
 * the necessary mapping.
 * @param select The {@link Query.select} property from the query.
 * @param record The record returned from your API.
 */
export function expandQueryRecord<S extends QuerySelect, C extends QueryCalculations<S>>(
    select: S,
    record: QueryRecordJson,
    calcs?: C
): ExpandedQueryRecord<S, C> {
    const result: any = {};

    let n = 0;
    for (const key of getAggregatePropsOnly(select)) {
        result[key] = record.aggregated[n++];
    }

    const calculationProps = calcs ? keysOf(calcs) : [];
    for (const key of calculationProps) {
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
export function getAggregateValuesOnly<S extends QuerySelect, C extends QueryCalculations<S>>(
    select: S,
    record: QueryRecordJson,
    calcs?: C
): AggregateValuesOnly<S> & CalculationValues<C> {
    const result: any = {};

    let n = 0;
    for (const key of getAggregatePropsOnly(select)) {
        result[key] = record.aggregated[n++];
    }

    const calculationProps = calcs ? keysOf(calcs) : [];
    for (const key of calculationProps) {
        result[key] = record.aggregated[n++];
    }

    return result;
}

/**
 * The statically-typed result of a {@link Query}.
 */
export interface ExpandedQueryResult<S extends QuerySelect, C extends QueryCalculations<S>> {
    /**
     * The set of records returned, each having named properties
     * corresponding to the plain and aggregated columns selected
     * in the query.
     */
    records: ExpandedQueryRecord<S, C>[];
    /**
     * Optional extra record, only available if {@link QueryJson.totals}
     * was specified as `true` in the query, containing the aggregation
     * totals.
     */
    totals?: AggregateValuesOnly<S> & CalculationValues<C>;
}

/**
 * Converts the payload returned from the API into the statically-typed
 * form appropriate for the query.
 * @param select The {@link Query.select} property from the query.
 * @param result The response payload from the API call.
 */
export function expandQueryResult<S extends QuerySelect, C extends QueryCalculations<S>>(
    select: S,
    result: QueryResultJson,
    calcs?: C
): ExpandedQueryResult<S, C> {
    return {
        records: result.records.map((r) => expandQueryRecord(select, r, calcs)),
        totals: result.totals && getAggregateValuesOnly(select, result.totals, calcs),
    };
}
