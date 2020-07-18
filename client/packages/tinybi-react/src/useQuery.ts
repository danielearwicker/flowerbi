import { useState, useEffect } from "react";
import { Query, executeQuery, QueryFetch, jsonifyQuery, AnalysedRecords, initialAnalysedRecords } from "tinybi";
import stableStringify from "json-stable-stringify";
import { PageFilters } from "./usePageFilters";

export function useQuery(fetch: QueryFetch, query: Query, pageFilters?: PageFilters, interactionKey?: string) {

    query = !pageFilters ? query : {
        ...query, 
        filters: (query.filters ?? []).concat(pageFilters.getFilters(interactionKey ?? "")) 
    };

    const queryJson = jsonifyQuery(query);

    const [result, setResult] = useState<AnalysedRecords>(initialAnalysedRecords);

    const queryJsonStr = stableStringify(queryJson);

    useEffect(() => {
        executeQuery(fetch, queryJson).then(setResult);
    }, [queryJsonStr]);

    return result;
}
