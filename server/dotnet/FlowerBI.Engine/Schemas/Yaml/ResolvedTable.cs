namespace FlowerBI;

using System.Collections.Generic;

public record ResolvedTable(string Name)
{
    public string NameInDb { get; set; }

    public ResolvedColumn IdColumn { get; set; }

    public List<ResolvedColumn> Columns { get; } = new List<ResolvedColumn>();
}
