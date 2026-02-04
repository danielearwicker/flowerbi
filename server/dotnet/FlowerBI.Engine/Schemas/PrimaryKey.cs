using FlowerBI.Yaml;

namespace FlowerBI;

public class PrimaryKey : Column
{
    internal PrimaryKey(string dbName, string refName, DataType dataType, bool nullable)
        : base(dbName, refName, dataType, nullable)
    {
    }
}

public sealed class PrimaryForeignKey : PrimaryKey, IForeignKey
{
    public IColumn To { get; internal set; }

    internal PrimaryForeignKey(string dbName, string refName, DataType dataType, bool nullable)
        : base(dbName, refName, dataType, nullable)
    {
    }
}
