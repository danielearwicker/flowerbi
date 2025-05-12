import { useRef } from "react";
import { useFlowerBI } from "@flowerbi/react";
import { Bug, Workflow } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Pie } from "react-chartjs-2";
import { Chart as ChartJS } from "chart.js";
import type { VisualProps } from "./VisualProps";
import { FlowerBIChartBox } from "../util";

const id = "SourceOfErrors";

export function SourceOfErrors({ pageFilters, fetch }: VisualProps) {
    const query = {
        select: {
            sourceOfError: Workflow.SourceOfError,
            bugCount: Bug.Id.count(),
        },
        filters: [
            Workflow.Resolved.equalTo(true),
            ...pageFilters.getFilters(id),
        ],
        comment: id,
    };

    const result = useFlowerBI(fetch, query);

    const ref = useRef<ChartJS<"pie">>(null);

    return (
        <FlowerBIChartBox id={id} title="Source Of Errors" state={result.state}>
            <Pie
                ref={ref}
                options={{
                    onClick(evt, elements, chart) {
                        console.log("clicked", { evt, elements, chart });
                        if (elements[0]) {
                            const clicked =
                                result.records[elements[0].index].sourceOfError;
                            pageFilters.setInteraction(id, [
                                Workflow.SourceOfError.equalTo(clicked),
                            ]);
                        }
                    },
                }}
                data={{
                    labels: result.records.map((x) => x.sourceOfError),
                    datasets: [
                        {
                            label: "Count",
                            backgroundColor: dataColours,
                            data: result.records.map((x) => x.bugCount),
                        },
                    ],
                }}
            />
        </FlowerBIChartBox>
    );
}
