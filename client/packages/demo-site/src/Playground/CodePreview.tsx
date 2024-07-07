import React from "react";
import { AggregationType } from "flowerbi";
import { BuiltFilter, BuiltQuery, getColumnDataType, getTypedFilterValue } from "./builtQueryModel";

function generateFilters(lines: string[], indent: string, filters: BuiltFilter[]) {
    for (const f of filters) {
        if (f.operator && f.value) {
            const dataType = getColumnDataType(f.table, f.column);
            const typedValue = getTypedFilterValue(dataType, f.value);
            if (typedValue !== undefined) {
                const literal = JSON.stringify(typedValue);
                lines.push(`${indent}{ column: ${f.table}.${f.column}, operator: "${f.operator}", value: ${literal} },`);
            }
        }
    }
}

function aggregationMethod(aggregation: AggregationType | undefined, filters: BuiltFilter[]) {
    if (!aggregation) {
        return "";
    }

    var filterText = "";
    if (filters.length) {
        const filterLines: string[] = ["["];
        generateFilters(filterLines, "            ", filters);
        filterLines.push("        ]");
        filterText = filterLines.join("\n");
    }

    return `.${aggregation[0].toLocaleLowerCase()}${aggregation.substring(1)}(${filterText})`;
}

export interface CodePreviewProps {
    query: BuiltQuery;
    sql: string;
}

export function CodePreview({ query, sql }: CodePreviewProps) {
    const lines = [];
    lines.push("const data = useFlowerBI(fetch, {");
    lines.push("    select: {");
    for (const s of query.select) {
        if (s.name.trim() && s.table && s.column) {
            lines.push(`        ${s.name.trim()}: ${s.table}.${s.column}${aggregationMethod(s.aggregation, s.filters)},`);
        }
    }
    lines.push("    }");

    if (query.filters.length) {
        lines.push("    filters: [");
        generateFilters(lines, "        ", query.filters);
        lines.push("    ],");
    }

    if (query.ordering.length) {
        lines.push("    orderBy: [");
        for (const o of query.ordering) {
            lines.push(`        { select: ${o.name} }, descending: ${o.desc} },`);
        }
        lines.push("    ],");
    }

    lines.push("});");
    lines.push("");
    lines.push("/* generated SQL:");
    lines.push(sql.trim());
    lines.push("*/");

    return <pre>{lines.join("\n")}</pre>;
}
