import { useEffect, useState } from "react";
import { FilterJson, FilterOperator, OrderingJson, QueryFetch, QueryJson, QueryResultJson } from "flowerbi";
import { latestError, latestSql } from "../localFetch";
import { BuiltFilter, BuiltQuery, getColumnDataType, getColumnsWithOffsets, getTypedFilterValue } from "./builtQueryModel";

export interface DataPreviewProps {
    query: BuiltQuery;
    fetch: QueryFetch;
}

function generateFilters(builtFilters: BuiltFilter[]) {
    const filters: FilterJson[] = [];

    for (const f of builtFilters) {
        const dataType = getColumnDataType(f.table, f.column);
        const value = getTypedFilterValue(dataType, f.value);
        if (value !== undefined) {
            filters.push({
                column: `${f.table}.${f.column}`,
                operator: f.operator as FilterOperator,
                value,
            });
        }
    }
    return filters;
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
                filters: generateFilters(x.selection.filters),
            })),
        filters: generateFilters(query.filters),
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

    return { ...data, sql: latestSql, error: latestError };
}
