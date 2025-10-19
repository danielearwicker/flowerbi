import type {
    QueryValues,
    ExpandedQueryResult,
    QuerySelect,
    QueryCalculations,
} from "@flowerbi/client";
import { QueryValuesRow, QueryValuesTotal } from "@flowerbi/client";

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

export type FlowerBITableProps<
    S extends QuerySelect,
    C extends QueryCalculations<S>
> = {
    data: ExpandedQueryResult<S, C>;
    columns: {
        [label: string]: (record: QueryValues<S, C>) => ColumnDefinition;
    };
};

export function FlowerBITable<
    S extends QuerySelect,
    C extends QueryCalculations<S>
>({ data, columns }: FlowerBITableProps<S, C>) {
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
                        {Object.keys(columns).map((column) =>
                            renderCell(
                                column,
                                columns[column](
                                    new QueryValuesRow<S, C>(
                                        record,
                                        data.totals
                                    )
                                )
                            )
                        )}
                    </tr>
                ))}
            </tbody>
            {data.totals && (
                <tfoot>
                    <tr>
                        {Object.keys(columns).map((column) =>
                            renderCell(
                                column,
                                columns[column](
                                    new QueryValuesTotal<S, C>(data.totals!)
                                )
                            )
                        )}
                    </tr>
                </tfoot>
            )}
        </table>
    );
}
