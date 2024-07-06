import { FetchProps } from "../Reports/VisualProps";
import React, { PropsWithChildren, useState } from "react";
import { BuiltQuery, QueryBuilder } from "./QueryBuilder";
import { CodePreview } from "./CodePreview";
import { DataPreview } from "./DataPreview";
import { useBuiltQuery } from "./useBuiltQuery";

function PlaygroundPanel({ title, children }: PropsWithChildren<{ title: string }>) {
    return (
        <div className="panel">
            <div className="title">{title}</div>
            <div className="content">{children}</div>
        </div>
    );
}

export function Playground({ fetch }: FetchProps) {
    const [builtQuery, setBuiltQuery] = useState<BuiltQuery>({
        select: [
            { name: "customer", table: "Customer", column: "CustomerName" },
            { name: "assigned", table: "CoderAssigned", column: "FullName" },
            { name: "bugCount", table: "Bug", column: "Id", aggregation: "Count" },
        ],
        ordering: [],
    });

    const data = useBuiltQuery(builtQuery, fetch);

    function headerClicked(name: string) {
        setBuiltQuery((previous) => {
            const ordering = previous.ordering.slice();
            const index = ordering.findIndex((x) => x.name === name);
            if (index !== -1) {
                const existing = ordering[index];
                if (existing.desc) {
                    ordering.splice(index, 1);
                } else {
                    ordering[index] = { ...existing, desc: true };
                }
            } else {
                ordering.push({ name, desc: false });
            }
            return { ...previous, ordering };
        });
    }

    return (
        <div className="playground">
            <div className="row">
                <PlaygroundPanel title="Query Builder">
                    <QueryBuilder value={builtQuery} onChange={setBuiltQuery} />
                </PlaygroundPanel>
                <PlaygroundPanel title="Code">
                    <CodePreview query={builtQuery} sql={data.sql} />
                </PlaygroundPanel>
            </div>
            <div className="row">
                <PlaygroundPanel title="Data">
                    <DataPreview query={builtQuery} data={data} onHeaderClick={headerClicked} />
                </PlaygroundPanel>
                <PlaygroundPanel title="Chart"></PlaygroundPanel>
            </div>
        </div>
    );
}
