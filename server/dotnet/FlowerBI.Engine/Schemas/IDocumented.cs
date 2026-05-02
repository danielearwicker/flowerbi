using System.Collections.Generic;

namespace FlowerBI;

public interface IDocumented : INamed
{
    string Doc { get; }

    IReadOnlyList<IDocumented> See { get; }
}
