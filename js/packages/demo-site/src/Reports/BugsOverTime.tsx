import { useRef } from "react";
import { Bug, Workflow, DateReported } from "../demoSchema";
import { dataColours } from "./dataColours";
import {
    Chart as ChartJS,
    type ChartDataset,
    type ChartOptions,
    type ChartData,
} from "chart.js";
import { Chart } from "react-chartjs-2";
import type { VisualProps } from "./VisualProps";
import type { QueryColumn } from "@flowerbi/client";
import { DropDown, FlowerBIChartBox, useDropDown } from "../util";
import { useFlowerBI } from "@flowerbi/react";
import { fillDates } from "@flowerbi/dates";

const dateGroupings = [
    { label: "Month", value: DateReported.FirstDayOfMonth },
    { label: "Quarter", value: DateReported.FirstDayOfQuarter },
    { label: "Year", value: DateReported.CalendarYearNumber },
];

export function BugsOverTime({ pageFilters, fetch }: VisualProps) {
    const id = "BugsOverTime";

    const dateGrouping = useDropDown<QueryColumn<Date | number>>(dateGroupings);

    const result = useFlowerBI(fetch, {
        select: {
            period: dateGrouping.selected,
            countAllCauses: Bug.Id.count(),
            countHackers: Bug.Id.count([
                Workflow.SourceOfError.equalTo("Hackers"),
            ]),
        },
        filters: [
            Workflow.Resolved.equalTo(true),
            ...pageFilters.getFilters(id),
        ],
        orderBy: [dateGrouping.selected.ascending()],
    });

    const datedRecords = fillDates({
        records: result.records,
        min: 2017,
        max: "2021-12-31",
        getDate: (r) => r.period,
        fill: (dateLabel, record) => ({
            dateLabel,
            countAllCauses: 0,
            countHackers: 0,
            ...record,
        }),
    });

    const ref = useRef<ChartJS<"bar">>(null);

    const hackers: ChartDataset = {
        type: "line",
        data: datedRecords.map((r) => r.countHackers ?? 0),
        label: "Hackers",
        backgroundColor: "transparent",
        // lineTension: 0,
        borderColor: dataColours[1],
    };

    const allCauses: ChartDataset = {
        data: datedRecords.map((r) => r.countAllCauses),
        label: "All Causes",
        backgroundColor: dataColours[0],
    };

    const data: ChartData = {
        labels: datedRecords.map((x) => x.dateLabel),
        datasets: [hackers, allCauses],
    };

    const options: ChartOptions = {
        onClick(evt, elements, chart) {
            console.log("clicked", { evt, elements, chart });
            if (elements[0]) {
                const clicked = result.records[elements[0].index].period;
                pageFilters.setInteraction(id, [
                    dateGrouping.selected.equalTo(clicked),
                ]);
            }
        },
        scales: {
            y: { beginAtZero: true },
        },
    };

    return (
        <FlowerBIChartBox id={id} title="Bugs Over Time" state={result.state}>
            <div className="chart-with-dropdown">
                <div className="content">
                    <Chart type="bar" ref={ref} options={options} data={data} />
                </div>
                <div className="dropdown">
                    <span>By </span>
                    <DropDown {...dateGrouping} />
                </div>
            </div>
        </FlowerBIChartBox>
    );
}
