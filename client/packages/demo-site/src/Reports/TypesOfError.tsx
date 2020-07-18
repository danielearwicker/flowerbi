import React, { useRef } from "react";
import { decodeBitCategory, QueryColumn } from "tinybi";
import { useQuery, TinyBIChartBox } from "tinybi-react";
import { makeClickHandler } from "tinybi-react-chartjs";
import { Bug, Workflow, CategoryCombination } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Bar } from "react-chartjs-2";
import { VisualProps } from "./VisualProps";

const categories: { [label: string]: QueryColumn<boolean> } = {
    "Crashed": CategoryCombination.Crashed,
    "Data loss": CategoryCombination.DataLoss,
    "Security breach": CategoryCombination.SecurityBreach,
    "Off by one": CategoryCombination.OffByOne,
    "Slow": CategoryCombination.Slow,
    "Stack overflow": CategoryCombination.StackOverflow,
};

export function TypesOfError({ pageFilters, fetch }: VisualProps) {

    const id = "TypesOfError";

    const query = {
        select: [Bug.CategoryCombinationId, Workflow.WorkflowState],
        aggregations: [Bug.Id.count()]
    };

    const data = useQuery(fetch, query, pageFilters, id);

    const chart = useRef<Bar>(null);

    const clickHandler = makeClickHandler(id, chart, 
        keys => [
            Workflow.WorkflowState.equalTo(keys[1]), 
            categories[keys[0]].equalTo(true)
        ],
        pageFilters
    );

    return (
        <TinyBIChartBox id={id} title="Types Of Error">
            <Bar 
                ref={chart}
                options={{ 
                    maintainAspectRatio: false,
                    scales: {
                        xAxes: [ { stacked: true } ],
                        yAxes: [ { stacked: true, ticks: { beginAtZero: true } } ]
                    },
                    ...clickHandler
                }}
                data={
                    decodeBitCategory(data, 
                        data.datasetPerLegend(undefined, false, dataColours), 
                        dataColours, 
                        Object.keys(categories))
                }
            />
        </TinyBIChartBox>
    )
}
