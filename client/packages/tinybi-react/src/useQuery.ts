import { useState, useEffect } from "react";
import { QueryJson, Query, executeQuery, QueryFetch, jsonifyQuery } from "tinybi";
import stableStringify from "json-stable-stringify";

export interface QueryRecord {
    selected: unknown[];
    aggregated: number[];
}

export interface QueryResult {
    rows: QueryRecord[];

    // Optional extra row with actual totals, not necessarily
    // same as summing the returned rows (which are truncated)
    totals?: QueryRecord;
}

export function useQuery(fetch: QueryFetch, query: Query) {
    const queryJson = jsonifyQuery(query);
    
    const [result, setResult] = useState<QueryResult>({ rows: [] });

    const queryJsonStr = stableStringify(queryJson);

    useEffect(() => {
        executeQuery(fetch, queryJson).then(setResult);
    }, [queryJsonStr]);

    return result;
}
