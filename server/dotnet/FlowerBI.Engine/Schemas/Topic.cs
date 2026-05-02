using System;
using System.Collections.Generic;

namespace FlowerBI;

public sealed class Topic : IDocumented
{
    public string DbName => null;

    public string RefName { get; }

    public string Doc { get; }

    public IReadOnlyList<IDocumented> See { get; internal set; } = Array.Empty<IDocumented>();

    internal Topic(string name, string doc)
    {
        RefName = name;
        Doc = doc;
    }

    public override string ToString() => RefName;
}
