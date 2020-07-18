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
        select: [Workflow.SourceOfError],
        aggregations: [Bug.Id.count()],
        filters: [Workflow.Resolved.equalTo(true)],
    };

    const ref = useRef<Pie>(null);
    const result = useQuery(fetch, query, pageFilters, id);
    const clickHandler = makeClickHandler(id, ref, query.select, pageFilters);
    
    return (
        <TinyBIChartBox id={id} title="Source Of Errors">
            <Pie
                ref={ref}
                options={{ ...clickHandler }} 
                data={{
                    labels: result.distinctSelected[0],
                    datasets: [result.datasetFromValue(0, "Count", false, dataColours)],
                }}
            />
        </TinyBIChartBox>
    );
}
