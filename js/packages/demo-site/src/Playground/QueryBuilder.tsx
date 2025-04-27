import React from "react";
import { SetterFunc, useLensOnObject } from "./lensHooks";
import { BuiltQuery } from "./builtQueryModel";
import { SelectionRow } from "./SelectionRow";
import { FilterRow } from "./FilterRow";

export interface QueryBuilderProps {
    value: BuiltQuery;
    onChange: SetterFunc<BuiltQuery>;
}

export function QueryBuilder({ value, onChange }: QueryBuilderProps) {
    const [realSelections, setSelections] = useLensOnObject(value, onChange, "select");
    const selections = realSelections.concat({ name: "", filters: [] });

    const [realFilters, setFilters] = useLensOnObject(value, onChange, "filters");
    const filters = realFilters.concat({ value: "" });

    return (
        <table>
            <thead>
                <tr className="sticky">
                    <th></th>
                    <th>Table</th>
                    <th>Column</th>
                    <th>Operator</th>
                    <th>Name/Value</th>
                </tr>
            </thead>
            <tbody>
                {selections.map((_, index) => (
                    <SelectionRow key={index} selections={selections} setSelections={setSelections} index={index} />
                ))}
            </tbody>
            <thead>
                <tr className="section">
                    <th colSpan={5}>General filters</th>
                </tr>
            </thead>
            <tbody>
                {filters.map((_, index) => (
                    <FilterRow key={index} filters={filters} setFilters={setFilters} index={index} nested={false} />
                ))}
            </tbody>
        </table>
    );
}
