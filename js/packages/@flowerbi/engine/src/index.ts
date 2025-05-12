import bootsharp, { FlowerBI } from "@flowerbi/bootsharp";

import type {
    QueryJson,
    QueryRecordJson,
    QuerySelectValue,
} from "@flowerbi/client";

const boot = bootsharp.boot();

export type FlowerBIFormatter = FlowerBI.ISqlFormatter;

export interface FlowerBISchema {
    generateSql(query: QueryJson): {
        sql: string;
        parameters: Record<string, QuerySelectValue>;
    };
    interpretRecord(query: QueryJson, row: object): QueryRecordJson;
}

export async function createSchema(
    yaml: string,
    formatter: FlowerBIFormatter
): Promise<FlowerBISchema> {
    await boot;

    const impl = FlowerBI.Bootsharp.Program.schema(yaml, formatter)!;

    return {
        generateSql(query: QueryJson) {
            return JSON.parse(impl.generateQuery(JSON.stringify(query)));
        },
        interpretRecord(query: QueryJson, row: object): QueryRecordJson {
            const selectedCount = query.select?.length ?? 0;
            const aggregatedCount =
                (query.aggregations?.length ?? 0) +
                (query.calculations?.length ?? 0);

            const selected: QuerySelectValue[] = [];
            const aggregated: number[] = [];
            const rowAsAny = row as any;
            for (let n = 0; n < selectedCount; n++) {
                selected[n] = rowAsAny[`Select${n}`];
            }
            for (let n = 0; n < aggregatedCount; n++) {
                aggregated[n] = rowAsAny[`Value${n}`];
            }
            return {
                selected,
                aggregated,
            };
        },
    };
}
