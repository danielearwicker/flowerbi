import React from "react";
import { TinyBIChartBox } from "./TinyBIChartBox";

export interface TinyBIValueBoxProps {
    id?: string;
    value?: string|number;
    title?: string;
    label?: string;    
}

// result.records[0]?.aggregated?.[0]

export function TinyBIValueBox({ id, value, title, label }: TinyBIValueBoxProps) {    
    return (
        <TinyBIChartBox id={id} title={title}>
            <div className="value-box">
                <div className="value">{value}</div>
                <div className="title">{label}</div>
            </div>
        </TinyBIChartBox>        
    );
}
