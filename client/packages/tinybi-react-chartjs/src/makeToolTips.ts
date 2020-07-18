import { ChartOptions, ChartData } from "chart.js";
import { AnalysedRecords } from "tinybi";
import { CategoryData, CategoryDataRow, CategoryWithTooltip } from "tinybi";

export type ToolTipHandler = (data: CategoryData) => string | string[];

export function makeToolTips(
    tooltip: ToolTipHandler, 
    analysed: AnalysedRecords,
    data: ChartData): ChartOptions {

    if (tooltip && data.labels) {
        data.labels = (data.labels.map(
            (key) => new CategoryWithTooltip(key as string, 
                tooltip(new CategoryDataRow(analysed, [key as unknown])))
        ) as unknown) as string[];

        return {
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
    }

    return {};
}
