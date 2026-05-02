namespace FlowerBI.Yaml;

using System;
using System.Collections.Generic;

public record ResolvedTopic(string Name, string Doc)
{
    public IReadOnlyList<string> See { get; set; } = Array.Empty<string>();
}
