import React, { useRef } from "react";
import { QueryColumn, smartDates } from "tinybi";
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

    const datedRecords = smartDates(result.records, r => r.period, (dateLabel, record) => ({
        dateLabel,
        countAllCauses: 0,
        countHackers: 0,
        ...record 
    }));

    const ref = useRef<Bar>(null);

    const clickHandler = makeClickHandler(id, ref, query.select!, pageFilters);
    
    const hackers: ChartDataSets = {
        type: "line",
        data: datedRecords.map(r => r.countHackers ?? 0),
        label: "Hackers",
        backgroundColor: "transparent",
        lineTension: 0,
        borderColor: dataColours[1],
    };
    
    const allCauses: ChartDataSets = {
        data: datedRecords.map(r => r.countAllCauses),
        label: "All Causes",
        backgroundColor: dataColours[0]
    };
    
    const data: ChartData = {
        labels: datedRecords.map(x => x.dateLabel),
        datasets: [hackers, allCauses],
    };

    const options: ChartOptions = {
        ...clickHandler,
        scales: {
            yAxes: [{ ticks: { beginAtZero: true } }]
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
