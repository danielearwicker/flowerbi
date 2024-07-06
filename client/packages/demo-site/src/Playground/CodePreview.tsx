import React from "react";
import { BuiltQuery } from "./QueryBuilder";
import { AggregationType } from "flowerbi";

function aggregationMethod(aggregation: AggregationType | undefined) {
    if (!aggregation) {
        return "";
    }
    return "." + aggregation[0].toLocaleLowerCase() + aggregation.substring(1) + "()";
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
        if (s.name.trim() && s.column) {
            lines.push(`        ${s.name.trim()}: ${s.table}.${s.column}${aggregationMethod(s.aggregation)}`);
        }
    }

    lines.push("    }");
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
