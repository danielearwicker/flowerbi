import React, { ReactNode } from "react";

export interface FlowerBIChartBox extends React.HTMLAttributes<HTMLDivElement> {
    id?: string;
    title?: string;    
    children?: ReactNode | ReactNode[];
    state?: string;
} 

export function FlowerBIChartBox({id, title, children, state}: FlowerBIChartBox) {
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
