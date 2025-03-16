import type { Event } from "./event";

export namespace FlowerBI.Bootsharp {
    export interface IFlowerBISchema {
        query(query: string): string;
    }
}

export namespace FlowerBI.Bootsharp.Program {
    export const onMainInvoked: Event<[message: string]>;
    export let getFrontendName: () => string;
    export function getBackendName(): string;
    export function schema(yaml: string): FlowerBI.Bootsharp.IFlowerBISchema;
}
