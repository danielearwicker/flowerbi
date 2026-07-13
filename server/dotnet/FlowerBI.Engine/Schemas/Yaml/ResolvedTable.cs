namespace FlowerBI.Yaml;

using System;
using System.Collections.Generic;

public record ResolvedTable(string Name, bool conjoint)
{
    public string NameInDb { get; set; }

    // Can be null, e.g. for fact table or association table never referenced by FKs
    public ResolvedColumn IdColumn { get; set; }

    public List<ResolvedColumn> Columns { get; } = new List<ResolvedColumn>();

    public List<ResolvedColumn> Associative { get; } = new List<ResolvedColumn>();

    public string Doc { get; set; }

    public IReadOnlyList<string> See { get; set; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, string> Meta { get; set; } =
        new Dictionary<string, string>();
}
