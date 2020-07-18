import React, { useRef } from "react";
import { QueryColumn, smartDates, Query } from "tinybi";
import { useDropDown, DropDown, useQuery, TinyBIChartBox } from "tinybi-react";
import { Bug, Workflow, DateReported } from "../demoSchema";
import { dataColours } from "./dataColours";
import { ChartDataSets, ChartOptions, ChartData } from "chart.js";
import { Bar } from "react-chartjs-2";
import { VisualProps } from "./VisualProps";
import { makeClickHandler } from "tinybi-react-chartjs";

const dateGroupings = [
    { label: "Month", value: DateReported.FirstDayOfMonth },
    { label: "Quarter", value: DateReported.FirstDayOfQuarter },
    { label: "Year", value: DateReported.CalendarYearNumber }
]

export function BugsOverTime({ pageFilters, fetch }: VisualProps) {

    const id = "BugsOverTime";

    const dateGrouping = useDropDown<QueryColumn<unknown>>(dateGroupings);

    const query: Query = {
        select: [dateGrouping.selected],
        aggregations: [
            Bug.Id.count(),
            Bug.Id.count([Workflow.SourceOfError.equalTo("Hackers")]),
        ],
        orderBy: [dateGrouping.selected.ascending()],
        filters: [Workflow.Resolved.equalTo(true)]
    };

    const ref = useRef<Bar>(null);
    const result = useQuery(fetch, query, pageFilters, id);
    const clickHandler = makeClickHandler(id, ref, query.select!, pageFilters);
    
    const hackers = result.datasetFromValue(1, "Hackers", false, "transparent");
    const allCauses = result.datasetFromValue(0, "All Causes", false, dataColours[0]);

    const hackersLine: ChartDataSets = {
        ...hackers,
        type: "line",
        borderColor: dataColours[1],
        lineTension: 0,
        data: hackers.data.map(x => Math.abs(x))
    };
    
    const data: ChartData = {
        labels: smartDates(result.distinctSelected[0]),
        datasets: [ hackersLine, allCauses],
    };

    const options: ChartOptions = {
        ...clickHandler,
        scales: {
            yAxes: [ { ticks: { beginAtZero: true } } ]
        }
    };

    return (
        <TinyBIChartBox id={id} title="Bugs Over Time">
            <div className="chart-with-dropdown">
                <div className="content">
                    <Bar ref={ref} options={options} data={data} />
                </div>            
                <div className="dropdown">
                    <span>By </span><DropDown {...dateGrouping} />
                </div>
            </div>            
        </TinyBIChartBox>
    );
}
