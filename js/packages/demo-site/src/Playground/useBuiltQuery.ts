import { useEffect, useState } from "react";
import type {
    FilterJson,
    FilterOperator,
    OrderingJson,
    QueryFetch,
    QueryJson,
    QueryResultJson,
} from "@flowerbi/client";
import {
    type BuiltFilter,
    type BuiltQuery,
    getColumnDataType,
    getColumnsWithOffsets,
    getTypedFilterValue,
} from "./builtQueryModel";
import { getSql } from "../query";

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
            console.log(value);
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
        select: columns
            .filter((x) => !x.selection.aggregation)
            .map((x) => `${x.selection.table}.${x.selection.column}`),
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
    const [result, setResult] = useState<{
        data: QueryResultJson;
        sql: string;
        error: string;
    }>({
        data: { records: [] },
        sql: "",
        error: "",
    });

    const queryJson = jsonFromBuiltQuery(query);
    const queryJsonString = JSON.stringify(queryJson);

    useEffect(() => {
        const q: QueryJson = JSON.parse(queryJsonString);
        if (q.aggregations.length + (q.select?.length ?? 0) === 0) {
            setResult({ data: { records: [] }, sql: "", error: "" });
        } else {
            Promise.all([fetch(q), getSql(q)]).then(([data, sql]) => {
                setResult({
                    data,
                    sql,
                    error: "",
                });
            });
        }
    }, [fetch, queryJsonString]);

    return result;
}
