import { useCallback, useMemo, useState } from "react";
import { BugSchema } from "../demoSchema";
import { keysOf } from "@flowerbi/client";
import { Select } from "./Select";
import { Input } from "./Input";
import { type SetterFunc, useLensOnArray, useLensOnObject } from "./lensHooks";
import {
    type BuiltSelection,
    aggregationsForDataType,
    getColumnDataType,
} from "./builtQueryModel";
import { FilterRow } from "./FilterRow";

function getDefaultName(others: BuiltSelection[]) {
    for (let n = 1; n < 100; n++) {
        const name = `col${n}`;
        if (!others.find((x) => x.name.trim() === name)) {
            return name;
        }
    }
    return "";
}

function isDeletion(from: BuiltSelection, to: BuiltSelection) {
    return !!((from.table && !to.table) || (from.name && !to.name));
}

function createDefaultWithName(name: string): BuiltSelection {
    return { name, filters: [] };
}

export function SelectionRow({
    selections,
    setSelections,
    index,
}: {
    selections: BuiltSelection[];
    setSelections: SetterFunc<BuiltSelection[]>;
    index: number;
}) {
    const nextDefaultName = useMemo(
        () => getDefaultName(selections),
        [selections]
    );

    const createDefaultSelection = useCallback(
        () => createDefaultWithName(nextDefaultName),
        [nextDefaultName]
    );

    const [selection, setSelection] = useLensOnArray(
        selections,
        setSelections,
        index,
        createDefaultSelection,
        isDeletion
    );

    const [table, setTable] = useLensOnObject(selection, setSelection, "table");
    const [column, setColumn] = useLensOnObject(
        selection,
        setSelection,
        "column"
    );
    const [aggregation, setAggregation] = useLensOnObject(
        selection,
        setSelection,
        "aggregation"
    );
    const [name, setName] = useLensOnObject(selection, setSelection, "name");

    const [expanded, setExpanded] = useState(false);
    const toggleExpanded = useCallback(
        () => setExpanded((p) => !p),
        [setExpanded]
    );

    const [realFilters, setFilters] = useLensOnObject(
        selection,
        setSelection,
        "filters"
    );
    const filters = realFilters.concat({ value: "" });

    return (
        <>
            <tr>
                <td
                    className={
                        (!expanded && realFilters[0] ? "filter-cell " : "") +
                        "expander"
                    }
                    onClick={toggleExpanded}
                >
                    {!!aggregation && (expanded ? "🔽" : "▶️")}
                </td>
                <td>
                    <Select
                        options={keysOf(BugSchema)}
                        value={table}
                        onChange={setTable}
                    />
                </td>
                <td>
                    <Select
                        options={table ? keysOf(BugSchema[table]) : []}
                        value={column}
                        onChange={setColumn}
                    />
                </td>
                <td>
                    <Select
                        options={
                            aggregationsForDataType[
                                getColumnDataType(table, column)
                            ]
                        }
                        value={aggregation}
                        onChange={setAggregation}
                    />
                </td>
                <td>
                    <Input value={name} onChange={setName} />
                </td>
            </tr>
            {expanded && aggregation && (
                <>
                    <tr className="section">
                        <td />
                        <th colSpan={4}>Extra filters for {name}</th>
                    </tr>

                    {filters.map((_, index) => (
                        <FilterRow
                            key={index}
                            filters={filters}
                            setFilters={setFilters}
                            index={index}
                            nested={true}
                        />
                    ))}
                    <tr className="spacer">
                        <td colSpan={5} />
                    </tr>
                </>
            )}
        </>
    );
}
