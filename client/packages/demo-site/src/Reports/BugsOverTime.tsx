import React from "react";
import { QueryColumn, smartDates } from "tinybi";
import { useDropDown, DropDown, useQuery, TinyBIChartBox } from "tinybi-react";
import { Bug, Workflow, DateReported } from "../demoSchema";
import { dataColours } from "./dataColours";
import { ChartDataSets, ChartOptions, ChartData } from "chart.js";
import { Bar } from "react-chartjs-2";
import { VisualProps } from "./VisualProps";

const dateGroupings = [
    { label: "Month", value: DateReported.FirstDayOfMonth },
    { label: "Quarter", value: DateReported.FirstDayOfQuarter },
    { label: "Year", value: DateReported.CalendarYearNumber }
]

export function BugsOverTime({ pageFilters, fetch }: VisualProps) {

    const id = "BugsOverTime";

    const dateGrouping = useDropDown<QueryColumn<unknown>>(dateGroupings);

    const query = {
        select: [dateGrouping.selected],
        aggregations: [
            Bug.Id.count(),
            Bug.Id.count([Workflow.SourceOfError.equalTo("Hackers")]),
        ],
        orderBy: [dateGrouping.selected.ascending()]
    };

    const data = useQuery(fetch, query, pageFilters, id);

    const raw = data.datasetFromValue(1, "Hackers", false, "transparent");

    const line: ChartDataSets = {
        ...raw,
        type: "line",
        borderColor: dataColours[1],
        lineTension: 0,
        data: raw.data.map(x => Math.abs(x))
    };
    
    const chartData: ChartData = {
        labels: smartDates(data.distinctSelected[0]),
        datasets: [
            line,
            data.datasetFromValue(0, "All Causes", false, dataColours[0])
        ],
    };

    const options: ChartOptions = {
        maintainAspectRatio: false,
        scales: {
            yAxes: [ { ticks: { beginAtZero: true } } ]
        }
    };

    return (
        <TinyBIChartBox id={id} title="Bugs Over Time">
            <div className="chart-with-dropdown">
                <div className="content">
                    <Bar options={options} data={chartData} />
                </div>            
                <div className="dropdown">
                    <span>By </span><DropDown {...dateGrouping} />
                </div>
            </div>            
        </TinyBIChartBox>
    );
}
