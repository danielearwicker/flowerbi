import { RuntimeConfig, RuntimeAPI } from "./modules";
import { BootResources } from "./resources";
/** Lifecycle status of the runtime module. */
export declare enum BootStatus {
    /** Ready to boot. */
    Standby = 0,
    /** Async boot process is in progress. */
    Booting = 1,
    /** Booted and ready for interop. */
    Booted = 2
}
/** Boot process configuration. */
export type BootOptions = {
    /** Absolute path to the directory where boot resources are hosted (eg, <code>/bin</code>). */
    readonly root?: string;
    /** Resources required to boot .NET runtime. */
    readonly resources?: BootResources;
    /** .NET runtime configuration. */
    readonly config?: RuntimeConfig;
    /** Creates .NET runtime instance. */
    readonly create?: (config: RuntimeConfig) => Promise<RuntimeAPI>;
    /** Binds imported C# APIs. */
    readonly import?: (runtime: RuntimeAPI) => Promise<void>;
    /** Starts .NET runtime. */
    readonly run?: (runtime: RuntimeAPI) => Promise<void>;
    /** Binds exported C# APIs. */
    readonly export?: (runtime: RuntimeAPI) => Promise<void>;
};
/** Returns current runtime module lifecycle state. */
export declare function getStatus(): BootStatus;
/** Initializes .NET runtime and binds C# APIs.
 *  @param options Specify to configure the boot process.
 *  @return Promise that resolves into .NET runtime instance. */
export declare function boot(options?: BootOptions): Promise<RuntimeAPI>;
/** Terminates .NET runtime and removes WASM module from memory.
 *  @param code Exit code; will use 0 (normal exit) by default.
 *  @param reason Exit reason description (optional). */
export declare function exit(code?: number, reason?: string): Promise<void>;
