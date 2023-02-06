namespace FlowerBI;

using System.Collections.Generic;

public class YamlSchema
{
    public string schema { get; set; }
    public string name { get; set; }
    public IEnumerable<YamlTable> tables { get; set; }
}
