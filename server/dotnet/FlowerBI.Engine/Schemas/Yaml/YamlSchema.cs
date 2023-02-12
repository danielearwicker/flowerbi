namespace FlowerBI.Yaml;

using System.Collections.Generic;

public class YamlSchema
{
    public string schema { get; set; }
    public string name { get; set; }
    public IDictionary<string, YamlTable> tables { get; set; }
}
