using System;

namespace FlowerBI;

public class PrimaryKey<T>(string name, Func<T, T> converter = null, Column<T> extends = null)
    : Column<T>(name, converter, extends) { }

public class PrimaryForeignKey<T>(
    string name,
    PrimaryKey<T> to,
    Func<T, T> converter = null,
    Column<T> extends = null
) : PrimaryKey<T>(name, converter, extends), IForeignKey
{
    public PrimaryKey<T> To { get; } = to;

    IColumn IForeignKey.To => To;
}
