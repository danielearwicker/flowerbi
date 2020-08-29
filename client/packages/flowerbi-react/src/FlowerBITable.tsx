import React from "react";
import { QueryValues, QueryValuesRow, QueryValuesTotal, ExpandedQueryResult, QuerySelect } from "flowerbi";

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

export type FlowerBITableProps<S extends QuerySelect> = {
    data: ExpandedQueryResult<S>;
    columns: {
        [label: string]: (record: QueryValues<S>) => ColumnDefinition;
    }
};

export function FlowerBITable<S extends QuerySelect>({data, columns}: FlowerBITableProps<S>) {
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
                {data.records.map((record, i) => (
                    <tr key={JSON.stringify(record.selected) ?? i}>
                    { 
                        Object.keys(columns).map((column) => renderCell(
                            column, columns[column](new QueryValuesRow(record, data.totals)))) 
                    }
                    </tr>
                ))}
            </tbody>
            {data.totals && (
                <tfoot>
                    <tr>
                    {
                        Object.keys(columns).map((column) => renderCell(
                            column, columns[column](new QueryValuesTotal(data.totals!))))
                    }
                    </tr>
                </tfoot>
            )}
        </table>
    );
}
