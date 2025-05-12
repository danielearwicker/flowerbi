import type { Event } from "./event";

export namespace FlowerBI {
    export interface ISqlFormatter {
        identifier(name: string): string;
        escapedIdentifierPair(id1: string, id2: string): string;
        skipAndTake(skip: bigint, take: number): string;
        conditional(predExpr: string, thenExpr: string, elseExpr: string): string;
        castToFloat(valueExpr: string): string;
        getParamPrefix(): string;
    }
}
export namespace FlowerBI.Bootsharp {
    export interface IFlowerBISchema {
        generateQuery(query: string): string;
    }
}

export namespace FlowerBI.Bootsharp.Program {
    export function schema(yaml: string, formatter: FlowerBI.ISqlFormatter): FlowerBI.Bootsharp.IFlowerBISchema | null;
}
