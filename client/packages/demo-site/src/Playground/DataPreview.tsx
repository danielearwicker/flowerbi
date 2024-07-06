import React from "react";
import { Selection, BuiltQuery, BuiltOrdering } from "./QueryBuilder";
import { QueryResultJson } from "flowerbi";

export interface DataPreviewProps {
    query: BuiltQuery;
    data: QueryResultJson;
    onHeaderClick(name: string): void;
}

function getOrderingIcon(orderings: BuiltOrdering[], name: string) {
    const ordering = orderings.find((x) => x.name === name);
    return !ordering ? "\u00A0" : ordering.desc ? "⬆️" : "⬇️";
}

export function getColumnsWithOffsets(query: BuiltQuery) {
    const columns = query.select.filter((x) => x.name.trim() && x.table && x.column);
    const result: { selection: Selection; offset: number }[] = [];
    let nextSelected = 0,
        nextAggregated = 0;
    for (const selection of columns) {
        if (selection.aggregation) {
            result.push({ selection, offset: nextAggregated++ });
        } else {
            result.push({ selection, offset: nextSelected++ });
        }
    }
    return result;
}

export function DataPreview({ query, data, onHeaderClick }: DataPreviewProps) {
    const columns = getColumnsWithOffsets(query);

    return (
        <table>
            <thead>
                <tr>
                    {columns.map((x) => (
                        <th className="sort-header" onClick={(e) => onHeaderClick(x.selection.name)}>
                            <span>{x.selection.name}</span>
                            <span className="arrow">{getOrderingIcon(query.ordering, x.selection.name)}</span>
                        </th>
                    ))}
                </tr>
            </thead>
            <tbody>
                {data.records.map((record) => (
                    <tr>
                        {columns.map((x) => (
                            <td>{x.selection.aggregation ? record.aggregated[x.offset] : record.selected[x.offset]}</td>
                        ))}
                    </tr>
                ))}
            </tbody>
        </table>
    );
}
