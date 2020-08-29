import React, { useState } from "react";
import "./App.css";
import { BugReporting } from "./Reports/BugReporting";
import { VisualProps } from "./Reports/VisualProps";
import { usePageFilters } from "flowerbi-react";
import { useFilterPane, FilterPane } from "./FilterPane";
import Chart from "chart.js";
import { localFetch } from "./localFetch";

Chart.defaults.global.defaultFontFamily = "Segoe UI, 'Helvetica', 'Arial', sans-serif";
if (Chart.defaults.global.legend && Chart.defaults.global.legend.labels) {
    Chart.defaults.global.legend.labels.usePointStyle = true;
}

Chart.defaults.global.maintainAspectRatio = false;

const reports = {
    "Bug Tracking": (f: VisualProps) => <BugReporting {...f} />,    
}

type ReportName = keyof typeof reports;

const reportNames = Object.keys(reports) as ReportName[];

const defaultReport: ReportName = "Bug Tracking";

function App() {
    const [reportName, setReportName] = useState(defaultReport);
    const pageFilters = usePageFilters();
    const filterPane = useFilterPane(pageFilters);

    const report = reports[reportName];

    return (
        <div className="reports-site">
            <div className="list">
                {
                    reportNames.map(n => (
                        <div key={n}
                            className={`item ${n === reportName && "selected"}`}
                            onClick={() => setReportName(n)}>{n}</div>
                    ))
                }
            </div>
            <div className="content">
                <div className="title-bar">
                    <div className="title">{filterPane.title}</div>
                    <div className="filters-button" onClick={filterPane.toggle}>Filters</div>
                </div>
                <div className="report-with-filters">
                    <div className="report-itself" onClick={pageFilters.clearInteraction}>
                        {report({ fetch: localFetch, pageFilters })}
                    </div>
                    <FilterPane filterPane={filterPane} />
                </div>
            </div>
        </div>
    );
}

export default App;
