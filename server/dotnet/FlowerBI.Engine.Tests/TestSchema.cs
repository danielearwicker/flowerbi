namespace FlowerBI.Engine.Tests
{
    [DbSchema("Testing")]
    public static class TestSchema
    {
        [DbTable("Supplier")]
        public static class Vendor
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> VendorName = new Column<string>("VendorName", x => $"[{x}]");
        }

        [DbTable("Department")]
        public static class Department
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> DepartmentName = new Column<string>("DepartmentName");
        }

        [DbTable("Invoice")]
        public static class Invoice
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly ForeignKey<int> VendorId = new ForeignKey<int>("VendorId", Vendor.Id, x => x * 2);
            public static readonly ForeignKey<int> DepartmentId = new ForeignKey<int>("DepartmentId", Department.Id, x => x * 2);
            public static readonly Column<decimal> Amount = new Column<decimal>("FancyAmount");
            public static readonly Column<bool?> Paid = new Column<bool?>("Paid");
        }

        [DbTable("Tag")]
        public static class Tag
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> TagName = new Column<string>("TagName");
        }

        [DbTable("InvoiceTag")]
        public static class InvoiceTag
        {
            public static readonly ForeignKey<int> InvoiceId = new ForeignKey<int>("InvoiceId", Invoice.Id);
            public static readonly ForeignKey<int> TagId = new ForeignKey<int>("TagId", Tag.Id);
        }

        [DbTable("Category")]
        public static class Category
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> CategoryName = new Column<string>("CategoryName");
        }

        [DbTable("InvoiceCategory")]
        public static class InvoiceCategory
        {
            public static readonly ForeignKey<int> InvoiceId = new ForeignKey<int>("InvoiceId", Invoice.Id);
            public static readonly ForeignKey<int> CategoryId = new ForeignKey<int>("CategoryId", Category.Id);
        }
    }
}
