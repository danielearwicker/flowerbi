import { type QueryResultJson } from "@flowerbi/client";
import {
    type BuiltOrdering,
    type BuiltQuery,
    getColumnsWithOffsets,
} from "./builtQueryModel";

export interface DataPreviewProps {
    query: BuiltQuery;
    data: QueryResultJson;
    onHeaderClick(name: string): void;
}

export function DataPreview({ query, data, onHeaderClick }: DataPreviewProps) {
    const columns = getColumnsWithOffsets(query);

    return (
        <table>
            <thead>
                <tr className="sticky">
                    {columns.map((x) => (
                        <th
                            key={x.selection.name}
                            className="sort-header"
                            onClick={() => onHeaderClick(x.selection.name)}
                        >
                            <span>{x.selection.name}</span>
                            <span className="arrow">
                                {getOrderingIcon(
                                    query.ordering,
                                    x.selection.name
                                )}
                            </span>
                        </th>
                    ))}
                </tr>
            </thead>
            <tbody>
                {data.records.map((record, index) => (
                    <tr key={index}>
                        {columns.map((x) => (
                            <td key={x.selection.name}>
                                {(x.selection.aggregation
                                    ? record.aggregated[x.offset]
                                    : record.selected[x.offset]
                                )?.toString()}
                            </td>
                        ))}
                    </tr>
                ))}
            </tbody>
        </table>
    );
}

function getOrderingIcon(orderings: BuiltOrdering[], name: string) {
    const ordering = orderings.find((x) => x.name === name);
    return !ordering ? "\u00A0" : ordering.desc ? "⬆️" : "⬇️";
}
