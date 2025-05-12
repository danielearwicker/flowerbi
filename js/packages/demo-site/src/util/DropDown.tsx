import { useState } from "react";

export interface DropDownOption<T> {
    label: string;
    value: T;
}

export function useDropDown<T>(options: DropDownOption<T>[]) {
    const [selectedLabel, setSelectedLabel] = useState(options[0].label);
    return {
        options: options.map((x) => x.label),
        value: selectedLabel,
        onChange(e: React.ChangeEvent<HTMLSelectElement>) {
            setSelectedLabel(e.target.value);
        },
        get selected() {
            return (
                options.find((x) => x.label === selectedLabel)?.value ??
                options[0].value
            );
        },
    };
}

export interface DropDownProps {
    options: string[];
    value: string;
    onChange(e: React.ChangeEvent<HTMLSelectElement>): void;
}

export function DropDown({ options, value, onChange }: DropDownProps) {
    return (
        <select value={value} onChange={onChange}>
            {options.map((x) => (
                <option key={x}>{x}</option>
            ))}
        </select>
    );
}
