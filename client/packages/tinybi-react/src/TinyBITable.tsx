import React from "react";
import { CategoryData, CategoryDataRow, CategoryDataTotal, Query, analyseRecords, QueryFetch } from "tinybi";
import { PageFilters } from "./usePageFilters";
import { useQuery } from ".";

export type ColumnDefinition = string | [string, "left" | "right"];

function renderCell(key: string, def: ColumnDefinition) {
    const value = typeof def === "string" ? def : def[0];
    const align = typeof def === "string" ? "left" : def[1];

    return (
        <td key={key} className={align}>
            {value}
        </td>
    );
}

export interface TinyBITableProps {
    fetch: QueryFetch;
    chartKey?: string;
    title?: string;
    query: Query;
    columns: {
        [label: string]: (data: CategoryData) => ColumnDefinition;
    },
    pageFilters?: PageFilters;
}

export function TinyBITable({fetch, chartKey, title, query, columns, pageFilters}: TinyBITableProps) {

    chartKey = chartKey ?? title;
    if (!chartKey) {
        throw new Error("Need to specify chartKey or title");
    }

    const nonNullChartKey = chartKey;

    if (pageFilters) {
        query = { ...query, filters: (query.filters ?? []).concat(pageFilters.getFilters(nonNullChartKey)) };
    }

    const data = analyseRecords(useQuery(fetch, query));
    
    return (
        <div className="chart-box" id={nonNullChartKey.replace(/\s+/g, "")}>
            <div className="title">{title}</div>
            <div className="chart">
                <table>
                    <thead>
                        <tr>
                            {Object.keys(columns).map((column) => (
                                <th key={column}>{column}</th>
                            ))}
                        </tr>
                    </thead>
                    <tbody>
                        {data.rows.map((row) => (
                            <tr key={row.selected.join("|")}>
                                {Object.keys(columns).map((column) => renderCell(column, columns[column](new CategoryDataRow(data, row.selected))))}
                            </tr>
                        ))}
                    </tbody>
                    {data.totals && (
                        <tfoot>
                            <tr>{Object.keys(columns).map((column) => renderCell(column, columns[column](new CategoryDataTotal(data))))}</tr>
                        </tfoot>
                    )}
                </table>                
            </div>
        </div>
    );
}
