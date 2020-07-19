import { useState, useEffect } from "react";
import { Query, executeQuery, QueryFetch, jsonifyQuery, QueryResult, QuerySelect, expandQueryResult } from "tinybi";
import stableStringify from "json-stable-stringify";

export function useQuery<S extends QuerySelect>(fetch: QueryFetch, query: Query<S>) {

    const queryJson = jsonifyQuery(query);

    const [result, setResult] = useState<QueryResult>({ records: [] });

    const queryJsonStr = stableStringify(queryJson);

    useEffect(() => {
        executeQuery(fetch, queryJson).then(setResult);
    }, [queryJsonStr]);

    return expandQueryResult(query.select, result);
}
