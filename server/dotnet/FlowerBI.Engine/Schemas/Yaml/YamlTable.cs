namespace FlowerBI.Yaml;

using System.Collections.Generic;

public class YamlTable
{
    public string name { get; set; }
    public Dictionary<string, object> id { get; set; }
    public Dictionary<string, object> columns { get; set; }
    public string extends { get; set; }
    public bool conjoint { get; set; }
    public string[] associative { get; set; }
    public string doc { get; set; }
    public string[] see { get; set; }
}
