import type { ModuleAPI, MonoConfig, AssetEntry } from "./dotnet.g.d.ts";
export type * from "./dotnet.g.d.ts";
export type RuntimeConfig = MonoConfig & {
    assets?: AssetEntry[];
};
/** Fetches main dotnet module (<code>dotnet.js</code>). */
export declare function getMain(root?: string): Promise<ModuleAPI & {
    embedded?: boolean;
}>;
/** Fetches dotnet native module (<code>dotnet.native.js</code>). */
export declare function getNative(root?: string): Promise<unknown & {
    embedded?: boolean;
}>;
/** Fetches dotnet runtime module (<code>dotnet.runtime.js</code>). */
export declare function getRuntime(root?: string): Promise<unknown & {
    embedded?: boolean;
}>;
