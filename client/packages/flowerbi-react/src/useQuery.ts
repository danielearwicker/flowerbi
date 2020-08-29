import { useState, useEffect } from "react";
import { Query, QueryFetch, jsonifyQuery, QueryResultJson, QuerySelect, expandQueryResult, ExpandedQueryResult } from "flowerbi";
import stableStringify from "json-stable-stringify";

export type UseQueryState = "init" | "ready" | "refresh" | "error";

export interface UseQueryResult<S extends QuerySelect> extends ExpandedQueryResult<S> {
    /**
     * The state of the query operation:
     * 
     * - `init` - no result has been downloaded yet
     * - `ready` - result has been downloaded
     * - `refresh` - a new result is being downloaded
     * - `error` - most recent query attempt failed
     */
    state: UseQueryState;
}

/**
 * A custom React hook that evaluates to the result of a 
 * [Query](../flowerbi/interfaces/query.html), making it easy to perform a 
 * query from within a component.
 * 
 * The returned object has a strongly-typed `records` array, and optionally
 * a `totals` object. It has a `state` of type {@link UseQueryState} that
 * can be used to show a loading indicator.
 * 
 * @param fetch The fetch function to use.
 * @param query The [Query](../flowerbi/interfaces/query.html) specification.
 */
export function useQuery<S extends QuerySelect>(fetch: QueryFetch, query: Query<S>): UseQueryResult<S> {

    const queryJson = jsonifyQuery(query);

    const [state, setState] = useState<UseQueryState>("init");
    const [result, setResult] = useState<QueryResultJson>({ records: [] });

    useEffect(() => {
        if (state !== "init") {
            setState("refresh");
        }
        fetch(queryJson).then(x => {
            setState("ready");
            setResult(x);
        }, () => {
            setState("error");
        });
    }, [stableStringify(queryJson)]);

    return { ...expandQueryResult(query.select, result), state };
}
