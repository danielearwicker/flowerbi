import React, { useRef } from "react";
import { QueryColumn, distinct, keysOf } from "tinybi";
import { useQuery } from "tinybi-react";
import { TinyBIChartBox } from "tinybi-react-utils";
import { makeClickHandler } from "tinybi-react-chartjs";
import { Bug, Workflow, CategoryCombination } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Bar } from "react-chartjs-2";
import { VisualProps } from "./VisualProps";

const categoriesRaw = {
    "Crashed": CategoryCombination.Crashed,
    "Data loss": CategoryCombination.DataLoss,
    "Security breach": CategoryCombination.SecurityBreach,
    "Off by one": CategoryCombination.OffByOne,
    "Slow": CategoryCombination.Slow,
    "Stack overflow": CategoryCombination.StackOverflow,
};

type CategoryLabel = keyof typeof categoriesRaw;

const categories: Record<CategoryLabel, QueryColumn<boolean>> = categoriesRaw;

export function TypesOfError({ pageFilters, fetch }: VisualProps) {

    const id = "TypesOfError";

    // In the query we ask for the bits describing the categories applicable to each bug
    const query = {
        select: {
            categoryBits: Bug.CategoryCombinationId, 
            workflowState: Workflow.WorkflowState,
            bugCount: Bug.Id.count()
        },
        filters: [
            Workflow.Resolved.equalTo(true),
            ...pageFilters.getFilters(id)
        ]
    };

    const data = useQuery(fetch, query);

    const chart = useRef<Bar>(null);

    const clickHandler = makeClickHandler(id, chart, 
        keys => [
            Workflow.WorkflowState.equalTo(keys[1]), 
            categories[keys[0] as CategoryLabel].equalTo(true)
        ],
        pageFilters
    );

    const distinctWorkflowStates = distinct(data.records.map(r => r.workflowState));

    // Convert the bit combinations into category labels
    const bugCountByStateAndCategory = distinctWorkflowStates.flatMap(workflowState =>
        keysOf(categories).map((category, bit) => ({
            workflowState,
            category,
            bugCount: data.records
                .filter(r => r.workflowState === workflowState && r.categoryBits & (1 << bit))
                .map(r => r.bugCount)
                .reduce((l, r) => l + r, 0)
        })));

    // Sum up the categories (full height of each bar)
    const bugCountByCategory = keysOf(categories).map(category => ({
        category,
        bugCount: bugCountByStateAndCategory
            .filter(r => r.category === category)
            .map(r => r.bugCount)
            .reduce((l, r) => l + r, 0)
    }));

    // Sort by bugCount descending so tallest bar is first
    bugCountByCategory.sort((x, y) => y.bugCount - x.bugCount);

    // Omit empty categories
    const orderedCategories = bugCountByCategory.filter(c => c.bugCount > 0).map(c => c.category);

    return (
        <TinyBIChartBox id={id} title="Types Of Error">
            <Bar 
                ref={chart}
                options={{ 
                    scales: {
                        xAxes: [ { stacked: true } ],
                        yAxes: [ { stacked: true, ticks: { beginAtZero: true } } ]
                    },
                    ...clickHandler
                }}
                data={{
                    labels: orderedCategories,
                    datasets: distinctWorkflowStates.map((workflowState, colour) => ({
                        label: workflowState,
                        data: orderedCategories.map(category =>
                            bugCountByStateAndCategory.find(r => r.workflowState === workflowState && 
                                                                 r.category === category)?.bugCount ?? 0),
                        backgroundColor: dataColours[colour]
                    }))
                }}
            />
        </TinyBIChartBox>
    )
}
