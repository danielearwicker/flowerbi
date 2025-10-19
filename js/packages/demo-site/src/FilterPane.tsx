import React, { useState } from "react";
import { DateReported } from "./demoSchema";
import type { PageFilters } from "./util/usePageFilters";

function lastYear() {
    const d = new Date();
    d.setFullYear(d.getFullYear() - 5);
    return d;
}

function toDateString(d: Date) {
    return d.toISOString().substr(0, 10);
}

function useDateInput(init: Date) {
    const [editing, setEditing] = useState(toDateString(init));
    const [applied, setApplied] = useState(toDateString(init));

    function apply() {
        setApplied(editing);
    }

    function onChange(event: React.ChangeEvent<HTMLInputElement>) {
        setEditing(event.target.value);
    }

    return {
        editing,
        applied,
        dirty: editing !== applied,
        onChange,
        apply,
    };
}

let initCounter = 0;

export function useFilterPane(pageFilters: PageFilters) {
    const [visible, setVisible] = useState(false);
    const from = useDateInput(lastYear());
    const to = useDateInput(new Date());
    const dates = [from, to];

    function toggle() {
        setVisible(!visible);
    }

    function apply() {
        dates.forEach((d) => d.apply());
        pageFilters.setGlobal([
            DateReported.Id.greaterThanOrEqualTo(new Date(from.editing)),
            DateReported.Id.lessThanOrEqualTo(new Date(to.editing)),
        ]);
    }

    const [init] = useState(++initCounter);
    if (init === initCounter) {
        apply();
    }

    const title = `Entered date from ${from.applied} to ${to.applied}`;

    const dirty = dates.some((d) => d.dirty);

    return { visible, toggle, from, to, dirty, title, apply };
}

export interface FilterPaneProps {
    filterPane: ReturnType<typeof useFilterPane>;
}

export function FilterPane({
    filterPane: { visible, from, to, apply, dirty },
}: FilterPaneProps) {
    return (
        <div className={`filter-pane ${visible && "visible"}`}>
            <div>From</div>
            <input type="date" value={from.editing} onChange={from.onChange} />
            <div>To</div>
            <input type="date" value={to.editing} onChange={to.onChange} />
            <div className={`button ${dirty && "enabled"}`} onClick={apply}>
                Update
            </div>
        </div>
    );
}
