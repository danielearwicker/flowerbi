import { useState, useEffect } from "react";
import { Query, executeQuery, QueryFetch, jsonifyQuery, QueryResult } from "tinybi";
import stableStringify from "json-stable-stringify";

export function useQuery(fetch: QueryFetch, query: Query) {
    const queryJson = jsonifyQuery(query);
    
    const [result, setResult] = useState<QueryResult>({ records: [] });

    const queryJsonStr = stableStringify(queryJson);

    useEffect(() => {
        executeQuery(fetch, queryJson).then(setResult);
    }, [queryJsonStr]);

    return result;
}
