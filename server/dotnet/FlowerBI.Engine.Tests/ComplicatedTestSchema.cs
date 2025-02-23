namespace FlowerBI.Engine.Tests;

[DbSchema("Testing")]
public static class ComplicatedTestSchema
{
    [DbTable("Department")]
    public static class Department
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>("Id");
        public static readonly Column<string> DepartmentName = new Column<string>("DepartmentName");
    }

    [DbTable("Supplier")]
    public static class Vendor
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>("Id");
        public static readonly Column<string> VendorName = new Column<string>("VendorName");
        public static readonly ForeignKey<long> DepartmentId = new ForeignKey<long>(
            "DepartmentId",
            Department.Id
        );
    }

    [DbTable("Invoice")]
    public static class Invoice
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>("Id");
        public static readonly ForeignKey<long> VendorId = new ForeignKey<long>(
            "VendorId",
            Vendor.Id
        );
        public static readonly ForeignKey<long> DepartmentId = new ForeignKey<long>(
            "DepartmentId",
            Department.Id
        );
        public static readonly Column<decimal> Amount = new Column<decimal>("FancyAmount");
        public static readonly Column<bool?> Paid = new Column<bool?>("Paid");
    }

    [DbTable("Tag")]
    public static class Tag
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>("Id");
        public static readonly Column<string> TagName = new Column<string>("TagName");
    }

    [DbTable("InvoiceTag")]
    public static class InvoiceTag
    {
        [DbAssociative]
        public static readonly ForeignKey<long> InvoiceId = new ForeignKey<long>(
            "InvoiceId",
            Invoice.Id
        );

        [DbAssociative]
        public static readonly ForeignKey<long> TagId = new ForeignKey<long>("TagId", Tag.Id);
    }

    [DbTable("Category")]
    public static class Category
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>("Id");
        public static readonly ForeignKey<long> DepartmentId = new ForeignKey<long>(
            "DepartmentId",
            Department.Id
        );
        public static readonly Column<string> CategoryName = new Column<string>("CategoryName");
    }

    [DbTable("InvoiceCategory")]
    public static class InvoiceCategory
    {
        [DbAssociative]
        public static readonly ForeignKey<long> InvoiceId = new ForeignKey<long>(
            "InvoiceId",
            Invoice.Id
        );

        [DbAssociative]
        public static readonly ForeignKey<long> CategoryId = new ForeignKey<long>(
            "CategoryId",
            Category.Id
        );
    }

    [DbTable("AnnotationName", true)]
    public static class AnnotationName
    {
        public static readonly ForeignKey<long> DepartmentId = new ForeignKey<long>(
            "DepartmentId",
            Department.Id
        );
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>("Id");
        public static readonly Column<string> Name = new Column<string>("Name");
    }

    [DbTable("AnnotationValue", true)]
    public static class AnnotationValue
    {
        public static readonly ForeignKey<long> DepartmentId = new ForeignKey<long>(
            "DepartmentId",
            Department.Id
        );
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>("Id");
        public static readonly ForeignKey<long> AnnotationNameId = new ForeignKey<long>(
            "AnnotationNameId",
            AnnotationName.Id
        );
        public static readonly Column<string> Value = new Column<string>("Value");
    }

    [DbTable("InvoiceAnnotation", true)]
    public static class InvoiceAnnotation
    {
        [DbAssociative]
        public static readonly ForeignKey<long> InvoiceId = new ForeignKey<long>(
            "InvoiceId",
            Invoice.Id
        );

        [DbAssociative]
        public static readonly ForeignKey<long> AnnotationValueId = new ForeignKey<long>(
            "AnnotationValueId",
            AnnotationValue.Id
        );
    }

    // Another entity that could exist with FKs into above tables, and yet it's not an
    // associative table because it doesn't use [DbAssociative]
    [DbTable("VendorRevision")]
    public static class VendorRevision
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>("Id");
        public static readonly ForeignKey<long> VendorId = new ForeignKey<long>(
            "VendorId",
            Vendor.Id
        );
        public static readonly ForeignKey<long> DepartmentId = new ForeignKey<long>(
            "DepartmentId",
            Department.Id
        );
    }
}
