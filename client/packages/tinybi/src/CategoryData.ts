import { AnalysedRecords } from "./analyseRecords";

export class CategoryWithTooltip {
    constructor(private name: string, private tooltip: string[] | string) {}

    valueOf() {
        return this.tooltip;
    }
    toString() {
        return this.name;
    }
}

export interface CategoryData {
    data: AnalysedRecords;
    keys: unknown[];
    value(index: number): number;
    share(index: number): number;
}

export class CategoryDataRow implements CategoryData {
    constructor(public readonly data: AnalysedRecords, public readonly keys: unknown[]) {}

    value(index: number) {
        return this.data.getValue(this.keys, index, false);
    }

    share(index: number) {
        return this.data.getValue(this.keys, index, true);
    }
}

export class CategoryDataTotal implements CategoryData {
    keys = ["Total"];

    constructor(public readonly data: AnalysedRecords) {}

    value(index: number) {
        return this.data.getTotal(index, false);
    }

    share(index: number) {
        return this.data.getTotal(index, true);
    }
}
