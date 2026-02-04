using FlowerBI.Yaml;

namespace FlowerBI;

public interface IForeignKey : IColumn
{
    IColumn To { get; }
}

public sealed class ForeignKey : Column, IForeignKey
{
    public IColumn To { get; internal set; }

    internal ForeignKey(string dbName, string refName, DataType dataType, bool nullable)
        : base(dbName, refName, dataType, nullable)
    {
    }
}
