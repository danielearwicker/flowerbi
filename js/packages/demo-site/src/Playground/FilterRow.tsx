import { BugSchema } from "../demoSchema";
import { keysOf } from "@flowerbi/client";
import { Select } from "./Select";
import { Input } from "./Input";
import { type SetterFunc, useLensOnArray, useLensOnObject } from "./lensHooks";
import {
    type BuiltFilter,
    getColumnDataType,
    operatorsForDataType,
} from "./builtQueryModel";

function createDefaultFilter() {
    return { value: "", operator: "=" } as BuiltFilter;
}

function isDeletion(from: BuiltFilter, to: BuiltFilter) {
    return !!((from.table && !to.table) || (from.operator && !to.operator));
}

export interface FilterRowProps {
    filters: BuiltFilter[];
    setFilters: SetterFunc<BuiltFilter[]>;
    index: number;
    nested: boolean;
}

export function FilterRow({
    filters,
    setFilters,
    index,
    nested,
}: FilterRowProps) {
    const [filter, setFilter] = useLensOnArray(
        filters,
        setFilters,
        index,
        createDefaultFilter,
        isDeletion
    );

    const [table, setTable] = useLensOnObject(filter, setFilter, "table");
    const [column, setColumn] = useLensOnObject(filter, setFilter, "column");
    const [operator, setOperator] = useLensOnObject(
        filter,
        setFilter,
        "operator"
    );
    const [value, setValue] = useLensOnObject(filter, setFilter, "value");

    return (
        <tr>
            {nested && <td />}
            <td colSpan={nested ? 1 : 2} className="filter-cell">
                <Select
                    options={keysOf(BugSchema)}
                    value={table}
                    onChange={setTable}
                />
            </td>
            <td className="filter-cell">
                <Select
                    options={table ? keysOf(BugSchema[table]) : []}
                    value={column}
                    onChange={setColumn}
                />
            </td>
            <td className="filter-cell">
                <Select
                    options={
                        operatorsForDataType[getColumnDataType(table, column)]
                    }
                    value={operator}
                    onChange={setOperator}
                />
            </td>
            <td className="filter-cell">
                <Input value={value} onChange={setValue} />
            </td>
        </tr>
    );
}
