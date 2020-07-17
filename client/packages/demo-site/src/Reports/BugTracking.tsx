import React from "react";

import { decodeBitCategory, QueryColumn, smartDates, QueryFetch } from "tinybi";

import {
    TinyBITable,
    Layout,
    Column,
    Row,
    useDropDown, 
    DropDown
} from "tinybi-react";

import {
    TinyBIPieChart,
    TinyBIBarChart,
    TinyBIStackedBarChart    
} from "tinybi-react-chartjs";

import { Customer, Bug, Workflow, CategoryCombination, DateReported } from "../demoSchema";

import { dataColours } from "./dataColours";
import { ChartDataSets } from "chart.js";
import { PageFiltersProp } from "tinybi-react";

export interface VisualProps extends PageFiltersProp {
    fetch: QueryFetch;
}

function ResolvedPerCustomer({ pageFilters, fetch }: VisualProps) {
    return (
        <TinyBIPieChart
            fetch={fetch}
            title="Resolved Per Customer"
            pageFilters={pageFilters}
            query={{
                select: [Customer.CustomerName],
                aggregations: [Bug.Id.count()],
                filters: [Workflow.Resolved.equalTo(true)],
            }}
            mapData={(data) => ({
                labels: data.distinctSelected[0],
                datasets: [data.datasetFromValue(0, "Count", false, dataColours)],
            })}            
        />
    );
}

function SourceOfErrors({ pageFilters, fetch }: VisualProps) {
    return (
        <TinyBIPieChart
            fetch={fetch}
            title="Source Of Errors"
            pageFilters={pageFilters}
            query={{
                select: [Workflow.SourceOfError],
                aggregations: [Bug.Id.count()],
                filters: [Workflow.Resolved.equalTo(true)],
            }}
            mapData={(data) => ({
                labels: data.distinctSelected[0],
                datasets: [data.datasetFromValue(0, "Count", false, dataColours)],
            })}            
        />
    );
}

const categories = {
    "Crashed": CategoryCombination.Crashed,
    "Data Loss": CategoryCombination.DataLoss,
    "Security Breach": CategoryCombination.SecurityBreach,
    "OffByOne": CategoryCombination.OffByOne,
    "Slow": CategoryCombination.Slow,
    "StackOverflow": CategoryCombination.StackOverflow,
};

function TypesOfError({ pageFilters, fetch }: VisualProps) {
    return (
        <TinyBIStackedBarChart
            fetch={fetch}
            title="Types Of Error"
            pageFilters={pageFilters}
            query={{
                select: [Bug.CategoryCombinationId, Workflow.WorkflowState],
                aggregations: [Bug.Id.count()]
            }}
            mapData={(data) => decodeBitCategory(data, data.datasetPerLegend(undefined, false, dataColours), dataColours, Object.keys(categories))}
            clickedFilters={(keys: [keyof typeof categories, string]) => [Workflow.WorkflowState.equalTo(keys[1]), categories[keys[0]].equalTo(true)]}
        />
    );
}

interface RecoverySummaryProps extends VisualProps {
    fixedByCustomer: boolean;
    title: string;
}

function RecoverySummary({ pageFilters, fixedByCustomer, title, fetch }: RecoverySummaryProps) {
    return (
        <TinyBITable
            fetch={fetch}
            title={title}
            pageFilters={pageFilters}
            query={{
                select: [Workflow.WorkflowState],
                aggregations: [Bug.Id.count()],
                filters: [
                    Workflow.FixedByCustomer.equalTo(fixedByCustomer),                    
                ],
                totals: true
            }}
            columns={{
                State: (d) => `${d.keys[0]}`,                
                Count: (d) => [`${d.value(0)}`, "right"],
                "% of Count": (d) => [`${d.share(0)}%`, "right"],
            }}
        />
    );
}

const dateGroupings = [
    { label: "Month", value: DateReported.FirstDayOfMonth },
    { label: "Quarter", value: DateReported.FirstDayOfQuarter },
    { label: "Year", value: DateReported.CalendarYearNumber }
]

function BugsOverTime({ pageFilters, fetch }: VisualProps) {
    const dateGrouping = useDropDown<QueryColumn<unknown>>(dateGroupings);

    return (
        <div className="chart-with-dropdown chart-box">
            <TinyBIBarChart
                fetch={fetch}
                title={"Bugs Over Time"}
                pageFilters={pageFilters}
                query={{
                    select: [dateGrouping.selected],
                    aggregations: [
                        Bug.Id.count(),
                        Bug.Id.count([Workflow.SourceOfError.equalTo("Hackers")]),
                    ],
                    orderBy: [dateGrouping.selected.ascending()]
                }}
                mapData={(data) => {
                    const raw = data.datasetFromValue(1, "Hackers", false, "transparent");

                    const line: ChartDataSets = {
                        ...raw,
                        type: "line",
                        borderColor: dataColours[1],
                        lineTension: 0,
                        data: raw.data.map(x => Math.abs(x))
                    };
                    
                    return {
                        labels: smartDates(data.distinctSelected[0]),
                        datasets: [
                            line,
                            data.datasetFromValue(0, "All Causes", false, dataColours[0])
                        ],
                    };
                }}
            />
            <div className="dropdown">
                <span>By </span><DropDown {...dateGrouping} />
            </div>            
        </div>
    );
}

export function Performance(props: VisualProps) {
    return (
        <Layout>
            <Column>
                <Row sizes={[1, 1, 2]}>
                    <ResolvedPerCustomer {...props} />
                    <SourceOfErrors {...props} />
                    <TypesOfError {...props} />
                </Row>
                <Row>
                    <BugsOverTime {...props} />
                    <Column>
                        <RecoverySummary {...props} title="Progress Summary" fixedByCustomer={false} />
                        <RecoverySummary {...props} title="Fixed By Customers" fixedByCustomer={true} />
                    </Column>
                </Row>
            </Column>
        </Layout>
    );
}
