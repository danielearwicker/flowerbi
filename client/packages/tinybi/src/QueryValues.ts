import { 
    QuerySelect, 
    ExpandedQueryRecord, 
    AggregateValuesOnly, 
    AggregatePropsOnly, 
    ExpandedQueryRecordWithOptionalColumns 
} from "./queryModel";

export interface QueryValues<S extends QuerySelect> {
    values: ExpandedQueryRecordWithOptionalColumns<S>;
    percentage<K extends AggregatePropsOnly<S>>(key: K): number;
}

export class QueryValuesRow<S extends QuerySelect> implements QueryValues<S> {
    constructor(
        public readonly values: ExpandedQueryRecord<S>, 
        public readonly totals: AggregateValuesOnly<S> | undefined) {}

    percentage<K extends AggregatePropsOnly<S>>(key: K) {
        if (!this.totals) return 0;

        const rawValue = this.values[key];
        const total = this.totals[key] ?? 0;
        const percent = total === 0 ? 0 : (rawValue / total) * 100;
        return Math.round(percent * 100) / 100;
    }
}

export class QueryValuesTotal<S extends QuerySelect> implements QueryValues<S> {
    public readonly values: ExpandedQueryRecordWithOptionalColumns<S>;

    constructor(totals: AggregateValuesOnly<S>) {
        this.values = totals as ExpandedQueryRecordWithOptionalColumns<S>;
    }

    percentage() {
        return 100;
    }
}
