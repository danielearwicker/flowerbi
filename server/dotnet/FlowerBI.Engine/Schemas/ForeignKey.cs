using System;

namespace FlowerBI;

public interface IForeignKey : IColumn
{
    IColumn To { get; }
}

public sealed class ForeignKey<T>(
    string name,
    PrimaryKey<T> to,
    Func<T, T> converter = null,
    Column<T> extends = null
) : Column<T>(name, converter, extends), IForeignKey
{
    public PrimaryKey<T> To { get; } = to;

    IColumn IForeignKey.To => To;
}
