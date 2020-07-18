import { RefObject } from "react";
import ChartComponent from "react-chartjs-2";
import Chart, { ChartOptions } from "chart.js";
import { FilterJson, QueryColumn } from "tinybi";
import { PageFilters } from "tinybi-react";

function getEventData(evt: MouseEvent | undefined, chart: Chart | null | undefined) {
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

export type ClickedFilterHandler = (keys: any[]) => FilterJson[];

export function makeClickHandler(
    id: string,
    chartRef: RefObject<ChartComponent<any>>,
    funcOrColumns: ClickedFilterHandler | QueryColumn<any>[], 
    pageFilters?: PageFilters,
): ChartOptions {

    const func: ClickedFilterHandler = typeof funcOrColumns === "function" ? funcOrColumns :
        keys => funcOrColumns.map((c, i) => c.equalTo(keys[i]));

    return {
        onClick(evt) {
            const data = getEventData(evt, chartRef.current?.chartInstance);
            if (data && pageFilters) {
                const filters = func([data.category, data.legend]);
                if (filters) {
                    pageFilters.setInteraction(id, filters);
                    evt?.stopPropagation();
                }                
            }
        }
    }
}
