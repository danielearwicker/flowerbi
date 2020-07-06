namespace TinyBI.Engine.Tests
{
    [DbSchema("TestSchema")]
    public static class TestSchema
    {
        [DbTable("Vendor")]
        public static class Vendor
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> VendorName = new Column<string>("VendorName");
        }

        [DbTable("Invoice")]
        public static class Invoice
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly ForeignKey<int> VendorId = new ForeignKey<int>("VendorId", Vendor.Id);
            public static readonly Column<decimal> Amount = new Column<decimal>("Amount");
            public static readonly Column<bool> Paid = new Column<bool>("Paid");
        }
    }
}
