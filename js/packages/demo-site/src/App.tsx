import { useState } from "react";
import "./App.css";
import { BugReporting } from "./Reports/BugReporting";
import {
    Chart,
    ArcElement,
    CategoryScale,
    LinearScale,
    BarElement,
    PointElement,
    LineElement,
    Legend,
    LineController,
} from "chart.js";
import { Playground } from "./Playground";
import type { FetchProps } from "./Reports/VisualProps";
import { query } from "./query";

Chart.registry.add(
    ArcElement,
    CategoryScale,
    LinearScale,
    BarElement,
    PointElement,
    LineElement,
    Legend,
    LineController
);

Chart.defaults.font.family = "Segoe UI, 'Helvetica', 'Arial', sans-serif";
if (Chart.defaults.plugins.legend && Chart.defaults.plugins.legend.labels) {
    Chart.defaults.plugins.legend.labels.usePointStyle = true;
}

Chart.defaults.maintainAspectRatio = false;

const reports = {
    Playground: (f: FetchProps) => <Playground {...f} />,
    "Bug Tracking": (f: FetchProps) => <BugReporting {...f} />,
};

type ReportName = keyof typeof reports;

const reportNames = Object.keys(reports) as ReportName[];

const defaultReport: ReportName = "Playground";

function App() {
    const [reportName, setReportName] = useState(defaultReport);

    const report = reports[reportName];

    return (
        <div className="reports-site">
            <div className="list">
                {reportNames.map((n) => (
                    <div
                        key={n}
                        className={`item ${n === reportName && "selected"}`}
                        onClick={() => setReportName(n)}
                    >
                        {n}
                    </div>
                ))}
            </div>
            <div className="content">{report({ fetch: query })}</div>
        </div>
    );
}

export default App;
