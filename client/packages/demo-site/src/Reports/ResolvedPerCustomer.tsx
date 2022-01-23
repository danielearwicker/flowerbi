import React, { useRef } from "react";
import { useQuery } from "flowerbi-react";
import { FlowerBIChartBox } from "flowerbi-react-utils";
import { Customer, Bug, Workflow } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Pie } from "react-chartjs-2";
import { Chart as ChartJS } from "chart.js";
import { VisualProps } from "./VisualProps";

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

    const ref = useRef<ChartJS<"pie">>(null);
    const result = useQuery(fetch, query);
    
    return (
        <FlowerBIChartBox id={id} title="Resolved Per Customer" state={result.state}>
            <Pie
                ref={ref}
                options={{
                    onClick(evt, elements, chart) {
                        console.log("clicked", { evt, elements, chart });
                        if (elements[0]) {
                            const clicked = result.records[elements[0].index].customer;
                            pageFilters.setInteraction(id, [
                                Customer.CustomerName.equalTo(clicked)
                            ]);
                        }
                    }
                }}
                data={{
                    labels: result.records.map(x => x.customer),
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
