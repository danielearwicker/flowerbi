using System;

namespace FlowerBI;

public interface IForeignKey : IColumn
{
    IColumn To { get; }
}
