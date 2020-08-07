import React, { useRef } from "react";
import { useQuery } from "tinybi-react";
import { TinyBIChartBox } from "tinybi-react-utils";
import { Customer, Bug, Workflow } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Pie } from "react-chartjs-2";
import { VisualProps } from "./VisualProps";
import { makeClickHandler } from "tinybi-react-chartjs";

export function ResolvedPerCustomer({ pageFilters, fetch }: VisualProps) {

    const id = "ResolvedPerCustomer";

    const query = {
        select: {
            customer: Customer.CustomerName,
            bugCount: Bug.Id.count()
        },
        filters: [
            Workflow.Resolved.equalTo(true),
            ...pageFilters.getFilters(id)
        ],
    };

    const ref = useRef<Pie>(null);
    const result = useQuery(fetch, query);
    const clickHandler = makeClickHandler(id, ref, query.select, pageFilters);
    
    return (
        <TinyBIChartBox id={id} title="Resolved Per Customer">
            <Pie
                ref={ref}
                options={{ ...clickHandler }}
                data={{
                    labels: result.records.map(x => x.customer),
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
