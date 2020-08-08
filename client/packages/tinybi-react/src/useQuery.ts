import { useState, useEffect } from "react";
import { Query, QueryFetch, jsonifyQuery, QueryResultJson, QuerySelect, expandQueryResult } from "tinybi";
import stableStringify from "json-stable-stringify";

/**
 * A custom React hook that evaluates to the result of a 
 * [Query](../tinybi/interfaces/query.html), making it easy to perform a 
 * query from within a component.
 * 
 * The returned object has a strongly-typed `records` array, and optionally
 * a `totals` object.
 * 
 * @param fetch The fetch function to use.
 * @param query The [Query](../tinybi/interfaces/query.html) specification.
 */
export function useQuery<S extends QuerySelect>(fetch: QueryFetch, query: Query<S>) {

    const queryJson = jsonifyQuery(query);

    const [result, setResult] = useState<QueryResultJson>({ records: [] });

    useEffect(() => {
        fetch(queryJson).then(setResult);
    }, [stableStringify(queryJson)]);

    return expandQueryResult(query.select, result);
}
