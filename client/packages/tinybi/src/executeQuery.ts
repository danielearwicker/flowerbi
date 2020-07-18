import { QueryJson, Query } from "./queryModel";
import { analyseRecords } from "./analyseRecords";

export interface QueryRecord {
    selected: unknown[];
    aggregated: number[];
}

export interface QueryResult {
    records: QueryRecord[];

    // Optional extra row with actual totals, not necessarily
    // same as summing the returned rows (which may be truncated)
    totals?: QueryRecord;
}

export type QueryFetch = (queryJson: string) => Promise<QueryResult>;

export function jsonifyQuery(query: Query): QueryJson {
    const { select, ...others } = query;

    return {
        ...others,
        select: select?.map((x) => x.name)
    };

}
export async function executeQuery(fetch: QueryFetch, query: QueryJson) {
    return analyseRecords(await fetch(JSON.stringify(query)));
}
