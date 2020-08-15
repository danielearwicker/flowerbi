import React, { ReactNode } from "react";

export interface TinyBIChartBox extends React.HTMLAttributes<HTMLDivElement> {
    id?: string;
    title?: string;    
    children?: ReactNode | ReactNode[];
    state?: string;
} 

export function TinyBIChartBox({id, title, children, state}: TinyBIChartBox) {
    return (
        <div className="chart-box" id={id}>
            <div className="title">{title}</div>
            <div className="chart">
                {state === "init" ? (
                    <div className="loading">Loading...</div>
                ) : (
                    <>{children}</>
                )}
            </div>
        </div>
    );
}
