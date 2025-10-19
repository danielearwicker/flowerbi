import { createSchema, type FlowerBIFormatter } from "@flowerbi/engine";
import { yaml } from "./demoSchema";
import type { QueryJson, QueryResultJson } from "@flowerbi/client";
import { db } from "./db";
import type { BindingSpec } from "@sqlite.org/sqlite-wasm";

const formatter: FlowerBIFormatter = {
    identifier: (name) => name,
    escapedIdentifierPair: (id1, id2) => `${id1}.${id2}`,
    skipAndTake: (skip, take) => `
limit ${take} -- take
offset ${skip} -- skip
`,
    conditional: (predExpr, thenExpr, elseExpr) =>
        `case when ${predExpr} then ${thenExpr} else ${elseExpr} END`,
    castToFloat: (valueExpr) => `cast(${valueExpr} as real)`,
    getParamPrefix: () => ":",
};

const schemaCreation = createSchema(yaml, formatter);

export async function getSql(query: QueryJson): Promise<string> {
    const schema = await schemaCreation;

    const { sql, parameters } = schema.generateSql(query);

    return `${sql}
    /* 
    ${JSON.stringify(parameters)}
    */`;
}

export async function query(query: QueryJson): Promise<QueryResultJson> {
    const schema = await schemaCreation;
    const { sql, parameters } = schema.generateSql(query);

    const result = (await db).selectObjects(sql, parameters as BindingSpec);

    const records = result.map((r) => schema.interpretRecord(query, r));
    return { records };
}
