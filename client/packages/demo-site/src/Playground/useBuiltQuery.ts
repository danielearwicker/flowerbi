import { useEffect, useState } from "react";
import { BuiltQuery } from "./QueryBuilder";
import { OrderingJson, QueryFetch, QueryJson, QueryResultJson } from "flowerbi";
import { latestSql } from "../localFetch";
import { getColumnsWithOffsets } from "./DataPreview";

export interface DataPreviewProps {
    query: BuiltQuery;
    fetch: QueryFetch;
}

function jsonFromBuiltQuery(query: BuiltQuery): QueryJson {
    const columns = getColumnsWithOffsets(query);

    const orderBy: OrderingJson[] = [];

    for (const o of query.ordering) {
        const c = columns.find((c) => c.selection.name === o.name);
        if (c) {
            orderBy.push({
                type: c.selection.aggregation ? "Value" : "Select",
                index: c.offset,
                descending: o.desc,
            });
        }
    }

    return {
        select: columns.filter((x) => !x.selection.aggregation).map((x) => `${x.selection.table}.${x.selection.column}`),
        aggregations: columns
            .filter((x) => x.selection.aggregation)
            .map((x) => ({
                column: `${x.selection.table}.${x.selection.column}`,
                function: x.selection.aggregation!,
            })),
        orderBy,
    };
}

export function useBuiltQuery(query: BuiltQuery, fetch: QueryFetch) {
    const [data, setData] = useState<QueryResultJson>({ records: [] });

    const queryJson = jsonFromBuiltQuery(query);
    const queryJsonString = JSON.stringify(queryJson);

    useEffect(() => {
        const q: QueryJson = JSON.parse(queryJsonString);
        if (q.aggregations.length + (q.select?.length ?? 0) === 0) {
            setData({ records: [] });
        } else {
            fetch(q).then(setData);
        }
    }, [fetch, queryJsonString]);

    return { ...data, sql: latestSql };
}
