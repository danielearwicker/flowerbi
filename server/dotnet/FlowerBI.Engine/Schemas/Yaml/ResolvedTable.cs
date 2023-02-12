namespace FlowerBI.Yaml;

using System.Collections.Generic;

public record ResolvedTable(string Name)
{
    public string NameInDb { get; set; }

    // Can be null, e.g. for fact table or association table never referenced by FKs
    public ResolvedColumn IdColumn { get; set; }

    public List<ResolvedColumn> Columns { get; } = new List<ResolvedColumn>();
}
