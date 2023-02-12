namespace FlowerBI.Yaml;

public record ResolvedColumn(ResolvedTable Table, string Name, string[] YamlType)
{
    public string NameInDb { get; set; }

    public DataType DataType { get; set; }

    public bool Nullable { get; set; }

    public ResolvedColumn Target { get; set; }

    public ResolvedColumn Extends { get; set; }
}
