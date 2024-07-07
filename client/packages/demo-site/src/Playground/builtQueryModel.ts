import { AggregationType, FilterOperator, QueryColumnDataType } from "flowerbi";
import { BugSchema } from "../demoSchema";

export type TableName = keyof typeof BugSchema;

export interface BuiltFilter {
    operator?: FilterOperator;
    table?: TableName;
    column?: string;
    value: string;
}

export interface BuiltSelection {
    name: string;
    table?: TableName;
    column?: string;
    aggregation?: AggregationType;
    filters: BuiltFilter[];
}

export interface BuiltOrdering {
    name: string;
    desc: boolean;
}

export interface BuiltQuery {
    select: BuiltSelection[];
    ordering: BuiltOrdering[];
    filters: BuiltFilter[];
}

export const aggregationTypes: AggregationType[] = ["Count", "CountDistinct", "Sum", "Avg", "Min", "Max"];

const nonNumericAggregationTypes: AggregationType[] = ["Count", "CountDistinct", "Min", "Max"];

export const aggregationsForDataType: Record<QueryColumnDataType, AggregationType[]> = {
    [QueryColumnDataType.Bool]: nonNumericAggregationTypes,
    [QueryColumnDataType.Byte]: aggregationTypes,
    [QueryColumnDataType.DateTime]: nonNumericAggregationTypes,
    [QueryColumnDataType.Decimal]: aggregationTypes,
    [QueryColumnDataType.Double]: aggregationTypes,
    [QueryColumnDataType.Float]: aggregationTypes,
    [QueryColumnDataType.Int]: aggregationTypes,
    [QueryColumnDataType.Long]: aggregationTypes,
    [QueryColumnDataType.Short]: aggregationTypes,
    [QueryColumnDataType.String]: nonNumericAggregationTypes,
    [QueryColumnDataType.None]: [],
};

export function getColumnDataType(table: TableName | undefined, column: string | undefined): QueryColumnDataType {
    if (!table || !column) return QueryColumnDataType.None;
    const x = BugSchema[table];
    const y = x[column as keyof typeof x];
    return y?.type.dataType ?? QueryColumnDataType.None;
}

export function getTypedFilterValue(dataType: QueryColumnDataType, value: string) {
    if (dataType === QueryColumnDataType.None) {
        return undefined;
    }

    const numeric = parseFloat(value);
    if (dataType === QueryColumnDataType.Bool) {
        const lower = value.toLowerCase();
        if (numeric === 1 || lower === "true") {
            return true;
        }
        if (numeric === 0 || lower === "false") {
            return false;
        }
        return undefined;
    }
    if (dataType === QueryColumnDataType.DateTime) {
        try {
            return new Date(value);
        } catch (x) {
            return undefined;
        }
    }
    if (dataType === QueryColumnDataType.String) {
        return value;
    }
    return isNaN(numeric) ? undefined : numeric;
}

export const operators: FilterOperator[] = ["=", "<>", ">", "<", ">=", "<=", "IN", "NOT IN", "BITS IN", "LIKE"];

const boolOperators: FilterOperator[] = ["="];
const generalOperators: FilterOperator[] = boolOperators.concat(["<>", ">", "<", ">=", "<="]);
const stringOrNumberOperators: FilterOperator[] = generalOperators.concat(["IN", "NOT IN"]);
const intOperators: FilterOperator[] = stringOrNumberOperators.concat("BITS IN");
const stringOperators: FilterOperator[] = stringOrNumberOperators.concat("LIKE");

export const operatorsForDataType: Record<QueryColumnDataType, FilterOperator[]> = {
    [QueryColumnDataType.Bool]: boolOperators,
    [QueryColumnDataType.Byte]: intOperators,
    [QueryColumnDataType.DateTime]: stringOrNumberOperators,
    [QueryColumnDataType.Decimal]: stringOrNumberOperators,
    [QueryColumnDataType.Double]: stringOrNumberOperators,
    [QueryColumnDataType.Float]: stringOrNumberOperators,
    [QueryColumnDataType.Int]: intOperators,
    [QueryColumnDataType.Long]: intOperators,
    [QueryColumnDataType.Short]: intOperators,
    [QueryColumnDataType.String]: stringOperators,
    [QueryColumnDataType.None]: [],
};

export function getColumnsWithOffsets(query: BuiltQuery) {
    const columns = query.select.filter((x) => x.name.trim() && x.table && x.column);
    const result: { selection: BuiltSelection; offset: number }[] = [];
    const usedNames: { [name: string]: boolean } = {};

    let nextSelected = 0,
        nextAggregated = 0;
    for (const selection of columns) {
        if (usedNames[selection.name]) {
            continue;
        }
        usedNames[selection.name] = true;
        if (selection.aggregation) {
            result.push({ selection, offset: nextAggregated++ });
        } else {
            result.push({ selection, offset: nextSelected++ });
        }
    }
    return result;
}
