import React from "react";
import { Layout, Column } from "flowerbi-react-utils";
import { VisualProps } from "./VisualProps";
import { BugsGrid } from "./BugsGrid";

export function BugsGridReport(props: VisualProps) {
    return (
        <Layout>
            <Column>
                <BugsGrid {...props} />
            </Column>
        </Layout>
    );
}
