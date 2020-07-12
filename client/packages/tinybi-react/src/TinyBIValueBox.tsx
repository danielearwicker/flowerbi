import { useQuery } from "./useQuery";
import { QueryFetch, AggregationJson, FilterJson } from "tinybi";
import { PageFilters } from "./usePageFilters";
import React from "react";

export interface TinyBIValueBoxProps {
    fetch: QueryFetch;
    title?: string;
    label?: string;
    pageFilters: PageFilters;
    aggregation: AggregationJson;
    filters?: FilterJson[];
}

export function TinyBIValueBox({ fetch, title, label, pageFilters, aggregation, filters }: TinyBIValueBoxProps) {
    
    const allFilters = (filters ?? []).concat(pageFilters.getFilters(""));
    
    const result = useQuery(fetch, {
        aggregations: [ aggregation ],
        filters: allFilters
    });

    return (
        <div className="chart-box">
            <div className="title">{title}</div>
            <div className="chart">
                <div className="value-box">
                    <div className="value">{result.records[0]?.aggregated?.[0]}</div>
                    <div className="title">{label}</div>
                </div>            
            </div>
        </div>
    );
}
