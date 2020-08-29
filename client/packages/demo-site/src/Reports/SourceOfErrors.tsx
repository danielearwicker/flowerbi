import React, { useRef } from "react";
import { useQuery } from "flowerbi-react";
import { FlowerBIChartBox } from "flowerbi-react-utils";
import { Bug, Workflow } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Pie } from "react-chartjs-2";
import { VisualProps } from "./VisualProps";
import { makeClickHandler } from "flowerbi-react-chartjs";

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

    const ref = useRef<Pie>(null);
    const clickHandler = makeClickHandler(id, ref, query.select, pageFilters);
    
    return (
        <FlowerBIChartBox id={id} title="Source Of Errors" state={result.state}>
            <Pie
                ref={ref}
                options={{ ...clickHandler }} 
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
