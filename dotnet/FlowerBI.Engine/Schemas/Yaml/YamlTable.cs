namespace FlowerBI.Yaml;

using System.Collections.Generic;

public class YamlTable
{
    public string name { get; set; }
    public Dictionary<string, string[]> id { get; set; }
    public Dictionary<string, string[]> columns { get; set; }
    public string extends { get; set; }
    public bool conjoint { get; set; }
    public string[] associative { get; set; }
}
