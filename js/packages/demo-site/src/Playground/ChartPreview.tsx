import { type QueryResultJson } from "@flowerbi/client";
import { type BuiltQuery, getColumnsWithOffsets } from "./builtQueryModel";
import { Chart } from "react-chartjs-2";
import { dataColours } from "../Reports/dataColours";

export interface ChartPreviewProps {
    query: BuiltQuery;
    data: QueryResultJson;
    error: string;
}

export function ChartPreview({ query, data, error }: ChartPreviewProps) {
    if (error) {
        try {
            const parsed = JSON.parse(error);
            if (parsed.message && parsed.stackTrace) {
                return (
                    <div>
                        <div>{parsed.message}</div>
                        <pre>{parsed.stackTrace}</pre>
                    </div>
                );
            }
        } catch (x) {}
        return <pre>{error}</pre>;
    }

    const columns = getColumnsWithOffsets(query);

    const barGrouping = columns.find((x) => !x.selection.aggregation);
    const aggregations = columns.filter((x) => !!x.selection.aggregation);

    if (!barGrouping || !aggregations.length) {
        return <div />;
    }

    const chart = {
        labels: data.records.map((r) => r.selected[barGrouping.offset]),
        datasets: aggregations.map((a, i) => ({
            data: data.records.map((r) => r.aggregated[a.offset]),
            label: a.selection.name,
            backgroundColor: dataColours[i],
        })),
    };

    return <Chart type="bar" data={chart} />;
}
