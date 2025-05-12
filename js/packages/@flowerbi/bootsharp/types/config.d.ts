import { RuntimeConfig } from "./modules";
import { BootResources } from "./resources";
/** Builds .NET runtime configuration.
 *  @param resources Resources required for runtime initialization.
 *  @param root When specified, assumes boot resources are side-loaded from the specified root. */
export declare function buildConfig(resources: BootResources, root?: string): Promise<RuntimeConfig>;
