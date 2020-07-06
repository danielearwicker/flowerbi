import { QueryResult } from "./executeQuery";
import { distinct, groupBy } from "./arrayHelpers";

export type LabelMapperFunc = (key: string) => string;
export type LabelMappingTable = { [key: string]: string };

export type LabelMapper = LabelMapperFunc | LabelMappingTable | undefined;

export function mapLabel(labels: LabelMapper, key: string) {
    return (typeof labels === "function" ? labels(key) : typeof labels === "object" ? labels[key] : undefined) ?? key;
}

function makeResultKey(result: { selected: unknown[] }) {
    return result.selected.join("|");
}

export function analyseRecords({ rows, totals }: QueryResult) {
    const distinctSelected = rows[0]?.selected.map((_, i) => distinct(rows.map((r) => `${r.selected[i]}`))) ?? [];

    const lookup = groupBy(rows, (r) => makeResultKey(r));

    function getTotal(value: number, percentage: boolean) {
        if (percentage) {
            return 100;
        }

        if (!totals) throw new Error("Specify percentages=true");
        return totals.aggregated[value] ?? 0;
    }

    function getValue(selected: unknown[], value: number, percentage: boolean) {
        const resultKey = makeResultKey({ selected });
        const rawValue = lookup[resultKey]?.[0].aggregated[value] ?? 0;

        if (percentage) {
            if (!totals) throw new Error("Specify percentages=true");
            const total = totals.aggregated[value] ?? 0;
            const perc = total === 0 ? 0 : (rawValue / total) * 100;
            return Math.round(perc * 100) / 100;
        }

        return rawValue;
    }

    function datasetFromValue(valueIndex: 0 | 1 | 2 | 3, label: string, percentage: boolean, colours: string[] | string) {
        return {
            label,
            data: distinctSelected[0]?.map((c) => getValue([c], valueIndex, percentage)) ?? [],
            backgroundColor: colours,
        };
    }

    function datasetPerLegend(labels: LabelMapper, percentage: boolean, colours: string[] | string) {
        return (
            distinctSelected[1]?.map((l, i) => ({
                label: mapLabel(labels, l),
                data: distinctSelected[0].map((c) => getValue([c, l], 0, percentage)),
                backgroundColor: Array.isArray(colours) ? colours[i] : colours,
            })) ?? []
        );
    }

    return {
        rows,
        totals,
        distinctSelected,
        getValue,
        getTotal,
        datasetFromValue,
        datasetPerLegend,
    };
}

export type AnalysedRecords = ReturnType<typeof analyseRecords>;

export interface DataSet {
    data?: unknown[];
    label?: string;
}

export function decodeBitCategory(data: AnalysedRecords, legends: DataSet[], colours: string[], bits: string[]) {
    const bitCombinations = data.distinctSelected[0]?.map((c) => parseInt(c, 10)) ?? [];

    const totals = bits.map((_) => 0);

    function getNumber(set: DataSet, at: number) {
        const val = set.data?.[at];
        return typeof val === "number" ? val : 0;
    }

    for (const legend of legends) {
        legend.data = bits.map((_, index) => {
            const bit = 1 << index;
            let total = 0;

            const len = legend.data?.length ?? 0;
            for (let c = 0; c < len; c++) {
                if (bitCombinations[c] & bit) {
                    total += getNumber(legend, c);
                }
            }

            totals[index] += total;
            return total;
        });
    }

    const ordering = bits.map((_, o) => o).filter((o) => totals[o]);
    ordering.sort((o1, o2) => totals[o2] - totals[o1]);

    for (const legend of legends) {
        legend.data = ordering.map((o) => getNumber(legend, o));
    }

    return {
        labels: ordering.map((o) => bits[o]),
        datasets: legends.filter((l) => l.data?.some((d: unknown) => d)).map((d, i) => ({ 
            label: d.label,
            data: d.data as number[],
            backgroundColor: colours[i] 
        })),
    };
}
