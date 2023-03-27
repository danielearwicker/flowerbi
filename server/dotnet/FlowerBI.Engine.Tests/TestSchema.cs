namespace FlowerBI.Engine.Tests
{
    [DbSchema("Testing")]
    public static class TestSchema
    {
        [DbTable("Department")]
        public static class Department
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> DepartmentName = new Column<string>("DepartmentName");
        }

        [DbTable("Supplier")]
        public static class Vendor
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> VendorName = new Column<string>("VendorName", x => $"[{x}]");
            public static readonly ForeignKey<int> DepartmentId = new ForeignKey<int>("DepartmentId", Department.Id);
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

        [DbTable("AnnotationName", true)]
        public static class AnnotationName
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly Column<string> Name = new Column<string>("Name");
        }

        [DbTable("AnnotationValue", true)]
        public static class AnnotationValue
        {
            public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
            public static readonly ForeignKey<int> AnnotationNameId = new ForeignKey<int>("AnnotationNameId", AnnotationName.Id);
            public static readonly Column<string> Value = new Column<string>("Value");
        }

        [DbTable("InvoiceAnnotation", true)]
        public static class InvoiceAnnotation
        {
            public static readonly ForeignKey<int> InvoiceId = new ForeignKey<int>("InvoiceId", Invoice.Id);
            public static readonly ForeignKey<int> AnnotationValueId = new ForeignKey<int>("AnnotationValueId", AnnotationValue.Id);
        }
    }
}
