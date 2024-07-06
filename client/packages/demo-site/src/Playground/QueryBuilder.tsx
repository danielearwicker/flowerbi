import React from "react";
import { BugSchema } from "../demoSchema";
import { AggregationType, QueryColumnDataType, keysOf } from "flowerbi";
import { Select } from "./Select";
import { Input } from "./Input";

export type TableName = keyof typeof BugSchema;

export const aggregationTypes: AggregationType[] = ["Count", "CountDistinct", "Sum", "Avg", "Min", "Max"];

const nonNumericAggregationTypes: AggregationType[] = ["Count", "CountDistinct", "Min", "Max"];

const aggregationsForDataType: Record<QueryColumnDataType, AggregationType[]> = {
    [QueryColumnDataType.Bool]: nonNumericAggregationTypes,
    [QueryColumnDataType.Byte]: aggregationTypes,
    [QueryColumnDataType.DateTime]: nonNumericAggregationTypes,
    [QueryColumnDataType.Decimal]: aggregationTypes,
    [QueryColumnDataType.Double]: aggregationTypes,
    [QueryColumnDataType.Float]: aggregationTypes,
    [QueryColumnDataType.Int]: aggregationTypes,
    [QueryColumnDataType.Long]: aggregationTypes,
    [QueryColumnDataType.Short]: aggregationTypes,
    [QueryColumnDataType.String]: nonNumericAggregationTypes,
    [QueryColumnDataType.None]: [],
};

function getColumnDataType(table: TableName | undefined, column: string | undefined): QueryColumnDataType {
    if (!table || !column) return QueryColumnDataType.None;
    const x = BugSchema[table];
    const y = x[column as keyof typeof x];
    return y?.type.dataType ?? QueryColumnDataType.None;
}

export interface Selection {
    name: string;
    table?: TableName;
    column?: string;
    aggregation?: AggregationType;
}

export interface BuiltOrdering {
    name: string;
    desc: boolean;
}

export interface BuiltQuery {
    select: Selection[];
    ordering: BuiltOrdering[];
}

export interface QueryBuilderProps {
    value: BuiltQuery;
    onChange(updater: (prev: BuiltQuery) => BuiltQuery): void;
}

function getDefaultName(others: Selection[]) {
    for (let n = 1; n < 100; n++) {
        const name = `col${n}`;
        if (!others.find((x) => x.name.trim() === name)) {
            return name;
        }
    }
    return "";
}

export function QueryBuilder({ value, onChange }: QueryBuilderProps) {
    function setter<P extends keyof Selection>(index: number, prop: P) {
        return (value: Selection[P]) => {
            onChange((previousQuery) => {
                const previousItem = previousQuery.select[index] ?? { name: getDefaultName(previousQuery.select) };
                const updatedItem = { ...previousItem, [prop]: value };
                const updatedList = previousQuery.select.slice();
                if (!updatedItem.table && updatedItem.name === previousItem.name) {
                    updatedList.splice(index, 1);
                } else {
                    updatedList[index] = updatedItem;
                }
                return {
                    ...previousQuery,
                    select: updatedList,
                };
            });
        };
    }

    return (
        <table>
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Table</th>
                    <th>Column</th>
                    <th>Aggregation</th>
                </tr>
            </thead>
            <tbody>
                {value.select.concat({ name: "" }).map((selection, index) => (
                    <tr>
                        <td>
                            <Input value={selection.name} onChange={setter(index, "name")} />
                        </td>
                        <td>
                            <Select options={keysOf(BugSchema)} value={selection.table} onChange={setter(index, "table")} />
                        </td>
                        <td>
                            <Select
                                options={selection.table ? keysOf(BugSchema[selection.table]) : []}
                                value={selection.column}
                                onChange={setter(index, "column")}
                            />
                        </td>
                        <td>
                            <Select
                                options={aggregationsForDataType[getColumnDataType(selection.table, selection.column)]}
                                value={selection.aggregation}
                                onChange={setter(index, "aggregation")}
                            />
                        </td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
}
