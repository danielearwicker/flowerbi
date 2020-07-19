import React, { useRef } from "react";
import { useQuery, TinyBIChartBox } from "tinybi-react";
import { Bug, Workflow } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Pie } from "react-chartjs-2";
import { VisualProps } from "./VisualProps";
import { makeClickHandler } from "tinybi-react-chartjs";

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

    console.log(pageFilters.getFilters(id));

    const result = useQuery(fetch, query);

    const ref = useRef<Pie>(null);
    const clickHandler = makeClickHandler(id, ref, query.select, pageFilters);
    
    return (
        <TinyBIChartBox id={id} title="Source Of Errors">
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
        </TinyBIChartBox>
    );
}
