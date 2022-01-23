import React, { useRef } from "react";
import { useQuery } from "flowerbi-react";
import { FlowerBIChartBox } from "flowerbi-react-utils";
import { Bug, Workflow } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Pie } from "react-chartjs-2";
import { Chart as ChartJS } from "chart.js";
import { VisualProps } from "./VisualProps";

const id = "SourceOfErrors";

export function SourceOfErrors({ pageFilters, fetch }: VisualProps) {

    const query = {
        select: {
            sourceOfError: Workflow.SourceOfError,
            bugCount: Bug.Id.count()
        },
        filters: [
            Workflow.Resolved.equalTo(true),
            ...pageFilters.getFilters(id)
        ],
    };

    const result = useQuery(fetch, query);

    const ref = useRef<ChartJS<"pie">>(null);
    
    return (
        <FlowerBIChartBox id={id} title="Source Of Errors" state={result.state}>
            <Pie
                ref={ref}
                options={{
                    onClick(evt, elements, chart) {
                        console.log("clicked", { evt, elements, chart });
                        if (elements[0]) {
                            const clicked = result.records[elements[0].index].sourceOfError;
                            pageFilters.setInteraction(id, [
                                Workflow.SourceOfError.equalTo(clicked)
                            ]);
                        }
                    }
                }} 
                data={{
                    labels: result.records.map(x => x.sourceOfError),
                    datasets: [{
                        label: "Count",
                        backgroundColor: dataColours,
                        data: result.records.map(x => x.bugCount)
                    }]
                }}
            />
        </FlowerBIChartBox>
    );
}
