import { boot, exit, getStatus, BootStatus } from "./boot";
import { getMain, getNative, getRuntime } from "./modules";
import { buildConfig } from "./config";
declare const _default: {
    boot: typeof boot;
    exit: typeof exit;
    getStatus: typeof getStatus;
    BootStatus: typeof BootStatus;
    resources: import("./resources").BootResources;
    /** .NET internal modules and associated utilities. */
    dotnet: {
        getMain: typeof getMain;
        getNative: typeof getNative;
        getRuntime: typeof getRuntime;
        buildConfig: typeof buildConfig;
    };
};
export default _default;
export * from "./event";
export * from "./bindings.g";
export type { BootOptions } from "./boot";
export type { BootResources, BinaryResource } from "./resources";
