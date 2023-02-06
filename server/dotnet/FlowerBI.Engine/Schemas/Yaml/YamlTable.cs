namespace FlowerBI;

using System.Collections.Generic;

public class YamlTable
{
    public string table { get; set; }
    public string name { get; set; }
    public Dictionary<string, string[]> id { get; set; }
    public Dictionary<string, string[]> columns { get; set; }
    public string extends { get; set; }
}
