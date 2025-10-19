import {
    createQueryEngineFromYaml,
    type QueryJson as QueryJsonBackend,
} from "@flowerbi/query-generation";
import {
    type QueryJson as QueryJsonFrontend,
    type QueryResultJson as QueryResultJsonFrontend,
} from "@flowerbi/client";
import { yaml } from "./demoSchema";
import { db } from "./db";
import type { BindingSpec, SqlValue } from "@sqlite.org/sqlite-wasm";

const queryEngine = createQueryEngineFromYaml(yaml, "sqlite");

export async function getSql(query: QueryJsonFrontend): Promise<string> {
    const { sql, parameters } = queryEngine.prepareQuery(
        query as QueryJsonBackend
    );

    return `${sql}
    /* 
    ${JSON.stringify(parameters)}
    */`;
}

export async function query(
    query: QueryJsonFrontend
): Promise<QueryResultJsonFrontend> {
    console.log("query", query);

    const { sql, parameters } = queryEngine.prepareQuery(
        query as QueryJsonBackend
    );

    console.log({ sql, parameters });

    const sdb = await db;

    let result: { [columnName: string]: SqlValue }[];

    try {
        result = sdb.selectObjects(sql, parameters as BindingSpec);
    } catch (error) {
        console.error("Error executing query:", error);
        throw error;
    }
    console.log({ sql, parameters, result });

    // Convert the result to the format expected by QueryEngine.mapResults
    const dbResult = {
        type: "array-of-objects" as const,
        rows: result,
    };

    return queryEngine.mapResults(query as QueryJsonBackend, dbResult);
}
