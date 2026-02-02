namespace FlowerBI.Engine.Tests;

public static class TestSchema
{
    public static class Department
    {
        public const string Id = "Department.Id";
        public const string DepartmentName = "Department.DepartmentName";
    }

    public static class Vendor
    {
        public const string Id = "Vendor.Id";
        public const string VendorName = "Vendor.VendorName";
        public const string DepartmentId = "Vendor.DepartmentId";
    }

    public static class Invoice
    {
        public const string Id = "Invoice.Id";
        public const string VendorId = "Invoice.VendorId";
        public const string DepartmentId = "Invoice.DepartmentId";
        public const string Amount = "Invoice.Amount";
        public const string Paid = "Invoice.Paid";
    }

    public static class Tag
    {
        public const string Id = "Tag.Id";
        public const string TagName = "Tag.TagName";
    }

    public static class InvoiceTag
    {
        public const string InvoiceId = "InvoiceTag.InvoiceId";
        public const string TagId = "InvoiceTag.TagId";
    }

    public static class Category
    {
        public const string Id = "Category.Id";
        public const string CategoryName = "Category.CategoryName";
    }

    public static class InvoiceCategory
    {
        public const string InvoiceId = "InvoiceCategory.InvoiceId";
        public const string CategoryId = "InvoiceCategory.CategoryId";
    }

    public static class AnnotationName
    {
        public const string Id = "AnnotationName.Id";
        public const string Name = "AnnotationName.Name";
    }

    public static class AnnotationValue
    {
        public const string Id = "AnnotationValue.Id";
        public const string AnnotationNameId = "AnnotationValue.AnnotationNameId";
        public const string Value = "AnnotationValue.Value";
    }

    public static class InvoiceAnnotation
    {
        public const string InvoiceId = "InvoiceAnnotation.InvoiceId";
        public const string AnnotationValueId = "InvoiceAnnotation.AnnotationValueId";
    }

    private const string YamlSchema = """
        schema: TestSchema
        name: Testing
        tables:
            Department:
                id:
                    Id: [long]
                columns:
                    DepartmentName: [string]

            Vendor:
                name: Supplier
                id:
                    Id: [long]
                columns:
                    VendorName: [string]
                    DepartmentId: [Department]

            Invoice:
                id:
                    Id: [long]
                columns:
                    VendorId: [Vendor]
                    DepartmentId: [Department]
                    Amount: [decimal, FancyAmount]
                    Paid: [bool?]

            Tag:
                id:
                    Id: [long]
                columns:
                    TagName: [string]

            InvoiceTag:
                columns:
                    InvoiceId: [Invoice]
                    TagId: [Tag]

            Category:
                id:
                    Id: [long]
                columns:
                    CategoryName: [string]

            InvoiceCategory:
                columns:
                    InvoiceId: [Invoice]
                    CategoryId: [Category]

            AnnotationName:
                conjoint: true
                id:
                    Id: [long]
                columns:
                    Name: [string]

            AnnotationValue:
                conjoint: true
                id:
                    Id: [long]
                columns:
                    AnnotationNameId: [AnnotationName]
                    Value: [string]

            InvoiceAnnotation:
                conjoint: true
                columns:
                    InvoiceId: [Invoice]
                    AnnotationValueId: [AnnotationValue]
                associative:
                    - InvoiceId
                    - AnnotationValueId
        """;

    public static Schema Schema { get; } = FlowerBI.Schema.FromYaml(YamlSchema);
}
