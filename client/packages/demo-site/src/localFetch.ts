import { jsonDateParser } from "json-date-parser";
import { getDb } from "./database";
import { QueryResultJson, QuerySelectValue, QueryJson } from "flowerbi";

export let latestSql = "";

async function querySql(sql: string) {
    const db = await getDb();

    const started = new Date();
    const result = JSON.stringify(db.exec(sql));
    const finished = new Date();
    console.log(`SQL query took ${finished.getTime() - started.getTime()} ms`);
    if (sql.includes("allbugs")) {
        console.log(sql);
    }

    latestSql = sql;

    return result;
}

(window as any).querySql = querySql;

const blazorReady = new Promise((done) => ((window as any).notifyBlazorReady = done));

export async function localFetch(queryJson: QueryJson): Promise<QueryResultJson> {
    await blazorReady;

    const started = new Date();
    const json = (await DotNet.invokeMethodAsync("FlowerBI.WasmHost", "Query", JSON.stringify(queryJson))) as string;
    const finished = new Date();
    console.log(queryJson.comment, `Blazor + SQL query took ${finished.getTime() - started.getTime()} ms`, queryJson);

    const parsed = JSON.parse(json, jsonDateParser);

    if (parsed.stackTrace) {
        console.error(parsed);
        return { records: [] };
    }

    if (!parsed[0]) {
        console.error(parsed);
        return { records: [] };
    }

    const columns = parsed[0].columns;
    const values = parsed[0].values as QuerySelectValue[][];
    const firstValueIndex = columns.indexOf("Value0");
    const endOfSelects = firstValueIndex === -1 ? columns.length : firstValueIndex;

    const result: QueryResultJson = {
        records: values.map((x) => ({
            selected: x.slice(0, endOfSelects),
            aggregated: x.slice(endOfSelects) as number[],
        })),
    };

    if (parsed[1]) {
        result.totals = {
            selected: [],
            aggregated: parsed[1].values,
        };
    }

    return result;
}
