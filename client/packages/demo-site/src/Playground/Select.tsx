import React from "react";

export interface SelectProps<T> {
    options: T[];
    value: T | undefined;
    onChange(v: T | undefined): void;
}

export function Select<T>({ options, value, onChange }: SelectProps<T>) {
    return (
        <select value={value + ""} onChange={(e) => onChange((e.target.value as unknown as T) ?? undefined)}>
            <option value="">(None)</option>
            {options.map((o) => (
                <option key={`${o}`}>{o}</option>
            ))}
        </select>
    );
}
