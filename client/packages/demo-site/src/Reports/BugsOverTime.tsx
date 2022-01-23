import React, { useRef } from "react";
import { QueryColumn } from "flowerbi";
import { useQuery } from "flowerbi-react";
import { fillDates } from "flowerbi-dates";
import { useDropDown, DropDown, FlowerBIChartBox } from "flowerbi-react-utils";
import { Bug, Workflow, DateReported } from "../demoSchema";
import { dataColours } from "./dataColours";
import { Chart as ChartJS, ChartDataset, ChartOptions, ChartData } from "chart.js";
import { VisualProps } from "./VisualProps";
import { Chart } from "react-chartjs-2";

const dateGroupings = [
    { label: "Month", value: DateReported.FirstDayOfMonth },
    { label: "Quarter", value: DateReported.FirstDayOfQuarter },
    { label: "Year", value: DateReported.CalendarYearNumber }
]

export function BugsOverTime({ pageFilters, fetch }: VisualProps) {

    const id = "BugsOverTime";

    const dateGrouping = useDropDown<QueryColumn<Date|number>>(dateGroupings);

    const query = {
        select: {
            period: dateGrouping.selected,
            countAllCauses: Bug.Id.count(),
            countHackers: Bug.Id.count([
                Workflow.SourceOfError.equalTo("Hackers")
            ]),
        },
        filters: [
            Workflow.Resolved.equalTo(true),
            ...pageFilters.getFilters(id)
        ],
        orderBy: [dateGrouping.selected.ascending()],        
    };

    const result = useQuery(fetch, query);

    const datedRecords = fillDates({ 
        records: result.records, 
        min: 2017, 
        max: "2021-12-31", 
        getDate: r => r.period, 
        fill: (dateLabel, record) => ({
            dateLabel,
            countAllCauses: 0,
            countHackers: 0,
            ...record 
        })
    });

    const ref = useRef<ChartJS<"bar">>(null);

    const hackers: ChartDataset = {
        type: "line",
        data: datedRecords.map(r => r.countHackers ?? 0),
        label: "Hackers",
        backgroundColor: "transparent",
        // lineTension: 0,
        borderColor: dataColours[1],
    };
    
    const allCauses: ChartDataset = {
        data: datedRecords.map(r => r.countAllCauses),
        label: "All Causes",
        backgroundColor: dataColours[0]
    };
    
    const data: ChartData = {
        labels: datedRecords.map(x => x.dateLabel),
        datasets: [hackers, allCauses],
    };

    const options: ChartOptions = {
        onClick(evt, elements, chart) {
            console.log("clicked", { evt, elements, chart });
            if (elements[0]) {
                const clicked = result.records[elements[0].index].period;
                pageFilters.setInteraction(id, [
                    dateGrouping.selected.equalTo(clicked)
                ]);
            }
        },
        scales: {
            y: { beginAtZero: true }
        }
    };

    return (
        <FlowerBIChartBox id={id} title="Bugs Over Time" state={result.state}>
            <div className="chart-with-dropdown">
                <div className="content">
                    <Chart type="bar" ref={ref} options={options} data={data} />
                </div>            
                <div className="dropdown">
                    <span>By </span><DropDown {...dateGrouping} />
                </div>
            </div>            
        </FlowerBIChartBox>
    );
}
