import React from "react";
import { Layout, Column, Row } from "flowerbi-react-utils";
import { FetchProps } from "./VisualProps";
import { ResolvedPerCustomer } from "./ResolvedPerCustomer";
import { SourceOfErrors } from "./SourceOfErrors";
import { TypesOfError } from "./TypesOfError";
import { RecoverySummary } from "./RecoverySummary";
import { BugsOverTime } from "./BugsOverTime";
import { FilterPane, useFilterPane } from "../FilterPane";
import { usePageFilters } from "flowerbi-react";

export function BugReporting({ fetch }: FetchProps) {
    const pageFilters = usePageFilters();
    const filterPane = useFilterPane(pageFilters);
    const props = { fetch, pageFilters };

    return (
        <>
            <div className="title-bar">
                <div className="title">{filterPane.title}</div>
                <div className="filters-button" onClick={filterPane.toggle}>
                    Filters
                </div>
            </div>
            <div className="report-with-filters">
                <div className="report-itself" onClick={pageFilters.clearInteraction}>
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
                </div>
                <FilterPane filterPane={filterPane} />
            </div>
        </>
    );
}
