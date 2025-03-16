// Named exports are auto-generated on C# build.
import bootsharp, { FlowerBI } from "flowerbi-bootsharp";

FlowerBI.Bootsharp.Program.onMainInvoked.subscribe(console.log);

// Binding 'Program.GetFrontendName' endpoint invoked in C#.
FlowerBI.Bootsharp.Program.getFrontendName = () => "Bleh";

// Subscribing to 'Program.OnMainInvoked' C# event.
FlowerBI.Bootsharp.Program.onMainInvoked.subscribe(console.log);

// Initializing dotnet runtime and invoking entry point.
await bootsharp.boot();

// Invoking 'Program.GetBackendName' C# method.
const schema = await FlowerBI.Bootsharp.Program.schema("diddle");

console.log(`Query result: ${schema.query("jonkins")}!`);
