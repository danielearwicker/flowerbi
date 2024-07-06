import React from "react";

export interface InputProps {
    value: string;
    onChange(v: string): void;
}

export function Input({ value, onChange }: InputProps) {
    return <input value={value} onChange={(e) => onChange(e.target.value)} />;
}
