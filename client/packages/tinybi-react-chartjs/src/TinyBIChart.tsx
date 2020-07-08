import React from "react";
import ChartComponent, { Pie, Bar } from "react-chartjs-2";
import Chart, { ChartOptions, ChartData } from "chart.js";
import { Query, FilterJson, QueryFetch } from "tinybi";
import { useQuery } from "tinybi-react";
import { PageFilters } from "tinybi-react";
import { AnalysedRecords, analyseRecords } from "tinybi";
import { CategoryData, CategoryDataRow, CategoryWithTooltip } from "tinybi";

function getEventData(evt?: MouseEvent, chart?: Chart) {
    if (chart && evt) {
        const element: any = chart.getElementAtEvent(evt)[0];
        if (element) {
            const data = element._chart.data;
            const labelIndex = element._index;
            const datasetIndex = element._datasetIndex;
            return {
                category: data.labels[labelIndex],
                legend: data.datasets[datasetIndex].label,
            };
        }
    }
}

export type ChartRefHandler = (c: ChartComponent<never> | null) => void;

export interface TinyBIBaseProps {
    fetch: QueryFetch;
    chartKey?: string;
    title?: string;
    query: Query;
    clickedFilters?(keys: unknown[]): FilterJson[];
    pageFilters?: PageFilters;
}

export interface TinyBIChartProps extends TinyBIBaseProps {
    mapData(queryResult: AnalysedRecords): ChartData;
    options?: ChartOptions;
    tooltip?(data: CategoryData): string | string[];
}

export interface TinyBIChartRenderProps extends TinyBIChartProps {
    render(chartData: ChartData, options: ChartOptions, ref: ChartRefHandler, analysed: AnalysedRecords): React.ReactNode;
}

export function TinyBIChart({ 
    fetch, chartKey, title, query, mapData, options,
    tooltip, render, clickedFilters, pageFilters
}: TinyBIChartRenderProps) {

    const { select } = query;

    chartKey = chartKey ?? title;
    if (!chartKey) {
        throw new Error("Need to specify chartKey or title");
    }

    const nonNullChartKey = chartKey;

    const nonNullClickedFilters = clickedFilters ?? ((keys: unknown[]) => select?.map((c, i) => c.equalTo((keys[i] as string).toString())));

    if (pageFilters) {
        query = { ...query, filters: (query.filters ?? []).concat(pageFilters.getFilters(nonNullChartKey)) };
    }

    let chartInstance: Chart | undefined = undefined;

    let allOptions: ChartOptions = {
        ...options,
        maintainAspectRatio: false,
        onClick(evt) {
            const data = getEventData(evt, chartInstance);
            if (data && pageFilters && chartInstance) {
                const filters = nonNullClickedFilters([data.category, data.legend]);
                if (filters) {
                    pageFilters.setInteraction(nonNullChartKey, filters);
                    evt?.stopPropagation();
                }                
            }
        },
    };

    const analysed = analyseRecords(useQuery(fetch, query));
    const data = mapData(analysed);

    if (tooltip && data.labels) {
        allOptions = {
            ...allOptions,
            tooltips: {
                callbacks: {
                    label: (item) => {
                        if (item.xLabel) {
                            return (item.xLabel?.valueOf() as string) ?? "";
                        }
                        if (typeof item.index === "number") {
                            return (data.labels?.[item.index].valueOf() as string) ?? "";
                        }
                        return [];
                    },
                },
            },
        };

        data.labels = (data.labels.map(
            (key) => new CategoryWithTooltip(key as string, tooltip(new CategoryDataRow(analysed, [key as unknown])))
        ) as unknown) as string[];
    }

    function handleRef(component: ChartComponent<never> | null) {
        chartInstance = component?.chartInstance;
        setTimeout(() => {
            chartInstance?.update();
        }, 1);
    }

    if (!data.labels) {
        return <div />;
    }
    return (
        <div className="chart-box" id={nonNullChartKey.replace(/\s+/g, "")}>
            <div className="title">{title}</div>
            <div className="chart">
                <div className="canvas-container">{render(data, allOptions, handleRef, analysed)}</div>
            </div>
        </div>
    );
}

export function TinyBIPieChart(props: TinyBIChartProps) {
    return <TinyBIChart {...props} render={(data, options, ref) => <Pie data={data} options={options} ref={ref} />} />;
}

export function TinyBIBarChart(props: TinyBIChartProps) {
    return <TinyBIChart {...props} render={(data, options, ref) => <Bar data={data} options={options} ref={ref} />} />;
}

export function TinyBIStackedBarChart(props: TinyBIChartProps) {
    return (
        <TinyBIBarChart
            {...props}
            options={{
                scales: {
                    xAxes: [{ stacked: true }],
                    yAxes: [{ stacked: true }],
                },
            }}
        />
    );
}
