import { jsonDateParser } from "json-date-parser";
import { getDb } from "./database";
import { QueryResult } from "tinybi";

async function querySql(sql: string) {
    
    const db = await getDb();

    sql = sql.replace(/[[\]]/g, "`")
             .replace(/top\s+[\d]+/g, " ")
             .replace(/`BugTracking`\./g, "");

    return JSON.stringify(db.exec(sql));
}

(window as any).querySql = querySql;

const blazorReady = new Promise(done => (window as any).notifyBlazorReady = done);

export async function localFetch(queryJson: string): Promise<QueryResult> {

    await blazorReady;

    const json = await DotNet.invokeMethodAsync("TinyBI.WasmHost", "Query", queryJson) as string;
    const parsed = JSON.parse(json, jsonDateParser);

    if (parsed.stackTrace) {
        console.error(parsed);
        return { records: [] };
    }

    const columns = parsed[0].columns;
    const values = parsed[0].values as unknown[][];
    const firstValueIndex = columns.indexOf("Value0");
    const endOfSelects = firstValueIndex === -1 ? columns.length : firstValueIndex;

    const result: QueryResult = {
        records: values.map(x => ({
            selected: x.slice(0, endOfSelects),
            aggregated: x.slice(endOfSelects) as number[]
        }))
    };
    
    if (parsed[1]) {
        result.totals = {
            selected: [],
            aggregated: parsed[1].values
        };
    }

    return result;
}
