import React, { ReactNode } from "react";

export interface TinyBIChartBox extends React.HTMLAttributes<HTMLDivElement> {
    id?: string;
    title?: string;    
    children?: ReactNode | ReactNode[];
} 

export function TinyBIChartBox({id, title, children}: TinyBIChartBox) {
    return (
        <div className="chart-box" id={id}>
            <div className="title">{title}</div>
            <div className="chart">
                {children}
            </div>
        </div>
    );
}
