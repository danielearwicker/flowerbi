import React from "react";
import { Layout, Column, Row } from "flowerbi-react-utils";
import { VisualProps } from "./VisualProps";
import { ResolvedPerCustomer } from "./ResolvedPerCustomer";
import { SourceOfErrors } from "./SourceOfErrors";
import { TypesOfError } from "./TypesOfError";
import { RecoverySummary } from "./RecoverySummary";
import { BugsOverTime } from "./BugsOverTime";

export function BugReporting(props: VisualProps) {
    return (
        <Layout>
            <Column>
                <Row sizes={[1, 1, 2]}>
                    <ResolvedPerCustomer {...props} />
                    <SourceOfErrors {...props} />
                    <TypesOfError {...props} />
                </Row>
                <Row>
                    <BugsOverTime {...props} />
                    <Column>
                        <RecoverySummary {...props} title="Progress Summary" fixedByCustomer={false} />
                        <RecoverySummary {...props} title="Fixed By Customers" fixedByCustomer={true} />
                    </Column>
                </Row>
            </Column>
        </Layout>
    );
}
