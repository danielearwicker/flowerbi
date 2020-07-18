import React from "react";
import { CategoryData, CategoryDataRow, CategoryDataTotal, AnalysedRecords } from "tinybi";

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

/* 
import { PageFilters, applyPageFilters } from "./usePageFilters";
import { useQuery } from ".";
import { RequireIdOrTitle, getIdAndTitle } from "./propsHelpers";

export type TinyBITableProps = RequireIdOrTitle<{
    fetch: QueryFetch;
    query: Query;
    columns: {
        [label: string]: (data: CategoryData) => ColumnDefinition;
    },
    pageFilters?: PageFilters;
}>;

export function TinyBITable({fetch, query, columns, pageFilters, ...rest}: TinyBITableProps) { 
*/

export type TinyBITableProps = {
    data: AnalysedRecords;
    columns: {
        [label: string]: (data: CategoryData) => ColumnDefinition;
    }
};

export function TinyBITable({data, columns}: TinyBITableProps) {
    return (
        <table>
            <thead>
                <tr>
                    {Object.keys(columns).map((column) => (
                        <th key={column}>{column}</th>
                    ))}
                </tr>
            </thead>
            <tbody>
                {data.records.map((record) => (
                    <tr key={record.selected.join("|")}>
                        {Object.keys(columns).map((column) => renderCell(column, columns[column](new CategoryDataRow(data, record.selected))))}
                    </tr>
                ))}
            </tbody>
            {data.totals && (
                <tfoot>
                    <tr>{Object.keys(columns).map((column) => renderCell(column, columns[column](new CategoryDataTotal(data))))}</tr>
                </tfoot>
            )}
        </table>
    );
}
