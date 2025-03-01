namespace FlowerBI.Engine.Tests;

[DbSchema("ParquetTest")]
public static class ParquetTestSchema
{
    [DbTable("Business")]
    public static class Business
    {
        public static readonly Column<double> Amount = new("Amount");
        public static readonly Column<string> Product = new("Product");
    }
}
