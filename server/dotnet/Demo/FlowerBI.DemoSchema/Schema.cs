using System.IO;

namespace FlowerBI.DemoSchema;

public static class DemoSchemaYaml
{
    public static readonly string Yaml = new StreamReader(
        typeof(DemoSchemaYaml).Assembly.GetManifestResourceStream(
            "FlowerBI.DemoSchema.demoSchema.yaml"
        )
    ).ReadToEnd();
}
