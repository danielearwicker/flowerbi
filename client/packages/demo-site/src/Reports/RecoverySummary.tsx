import React from "react";
import { FlowerBITable, useFlowerBI } from "flowerbi-react";
import { FlowerBIChartBox } from "flowerbi-react-utils";
import { Bug, Workflow } from "../demoSchema";
import { VisualProps } from "./VisualProps";

export interface RecoverySummaryProps extends VisualProps {
    fixedByCustomer: boolean;
    title: string;
}

export function RecoverySummary({ pageFilters, fixedByCustomer, title, fetch }: RecoverySummaryProps) {
    const data = useFlowerBI(fetch, {
        select: {
            state: Workflow.WorkflowState,
            bugCount: Bug.Id.count(),
            resolvedBugCount: Bug.Id.count([Workflow.Resolved.equalTo(true)]),
        },
        filters: [Workflow.FixedByCustomer.equalTo(fixedByCustomer), ...pageFilters.getFilters("")],
        totals: true,
    });

    return (
        <FlowerBIChartBox title={title} state={data.state}>
            <FlowerBITable
                data={data}
                columns={{
                    State: (d) => d.values.state ?? "Total",
                    Count: (d) => [`${d.values.bugCount}`, "right"],
                    "% of Count": (d) => [`${d.percentage("resolvedBugCount")}%`, "right"],
                }}
            />
        </FlowerBIChartBox>
    );
}
