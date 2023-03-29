import {
    QuerySelect,
    ExpandedQueryRecord,
    AggregateValuesOnly,
    AggregatePropsOnly,
    ExpandedQueryRecordWithOptionalColumns,
    QueryCalculations,
    CalculationValues,
} from "./queryModel";

/**
 * An abstract interface representing either a row from a dataset or
 * the {@link ExpandedQueryResult.totals} row, so that generic code can
 * format either of them in a consistent way.
 */
export interface QueryValues<S extends QuerySelect, C extends QueryCalculations<S>> {
    /**
     * The plain values of columns, which may be `undefined` if this
     * refers to the {@link ExpandedQueryResult.totals} record.
     */
    values: ExpandedQueryRecordWithOptionalColumns<S, C>;
    percentage<K extends AggregatePropsOnly<S>>(key: K): number;
}

export class QueryValuesRow<S extends QuerySelect, C extends QueryCalculations<S>> implements QueryValues<S, C> {
    constructor(public readonly values: ExpandedQueryRecord<S, C>, public readonly totals: (AggregateValuesOnly<S> & CalculationValues<C>) | undefined) {}

    percentage<K extends AggregatePropsOnly<S>>(key: K) {
        if (!this.totals) return 0;

        const rawValue = this.values[key];
        const total = this.totals[key] ?? 0;
        const percent = total === 0 ? 0 : (rawValue / total) * 100;
        return Math.round(percent * 100) / 100;
    }
}

export class QueryValuesTotal<S extends QuerySelect, C extends QueryCalculations<S>> implements QueryValues<S, C> {
    public readonly values: ExpandedQueryRecordWithOptionalColumns<S, C>;

    constructor(totals: AggregateValuesOnly<S> & CalculationValues<C>) {
        this.values = totals as ExpandedQueryRecordWithOptionalColumns<S, C>;
    }

    percentage() {
        return 100;
    }
}
