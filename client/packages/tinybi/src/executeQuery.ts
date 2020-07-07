import { QueryJson, Query } from "./queryModel";

export interface QueryRecord {
    selected: unknown[];
    aggregated: number[];
}

export interface QueryResult {
    rows: QueryRecord[];

    // Optional extra row with actual totals, not necessarily
    // same as summing the returned rows (which may be truncated)
    totals?: QueryRecord;
}

export type QueryFetch = (queryJson: string) => Promise<QueryRecord[]>;

export function jsonifyQuery(query: Query): QueryJson {
    const { select, ...others } = query;

    return {
        ...others,
        select: select?.map((x) => x.name)
    };

}
export async function executeQuery(fetch: QueryFetch, query: QueryJson) {
    
    const result = await fetch(JSON.stringify(query));

    const totalsIndex = result.findIndex(r => r.selected?.[0] === "_grand_total_");

    const totals = totalsIndex !== -1 ? result[totalsIndex] : undefined;

    const rows = totalsIndex === -1 ? result : result.slice(0, totalsIndex).concat(result.slice(totalsIndex + 1));

    return { totals, rows };
}
