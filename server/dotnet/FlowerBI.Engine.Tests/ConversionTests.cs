﻿namespace FlowerBI.Engine.Tests;

using FluentAssertions;
using Xunit;
using System.IO;
using FlowerBI.Conversion;
using FlowerBI.Yaml;

public class ConversionTests
{
    [Fact]
    public void GeneratesYamlFromReflection()
    {   
        var yaml = Reflection.ToYaml(QueryGenerationTests.Schema);

        var writer = new StringWriter();
        Reflection.Serialize(yaml, writer);

        writer.ToString().Should().Be(
@"schema: TestSchema
name: Testing
tables:

    Department:
        id:
            Id: [int]
        columns:
            DepartmentName: [string]

    Vendor:
        name: Supplier
        id:
            Id: [int]
        columns:
            VendorName: [string]
            DepartmentId: [Department]

    Invoice:
        id:
            Id: [int]
        columns:
            VendorId: [Vendor]
            DepartmentId: [Department]
            Amount: [decimal, FancyAmount]
            Paid: [bool?]

    Tag:
        id:
            Id: [int]
        columns:
            TagName: [string]

    InvoiceTag:
        columns:
            InvoiceId: [Invoice]
            TagId: [Tag]

    Category:
        id:
            Id: [int]
        columns:
            CategoryName: [string]

    InvoiceCategory:
        columns:
            InvoiceId: [Invoice]
            CategoryId: [Category]

    AnnotationName:
        conjoint: true
        id:
            Id: [int]
        columns:
            Name: [string]

    AnnotationValue:
        conjoint: true
        id:
            Id: [int]
        columns:
            AnnotationNameId: [AnnotationName]
            Value: [string]

    InvoiceAnnotation:
        conjoint: true
        associative: [InvoiceId, AnnotationValueId]
        columns:
            InvoiceId: [Invoice]
            AnnotationValueId: [AnnotationValue]
");
    }

    [Fact]
    public void GeneratesTypeScriptFromYaml()
    {   
        var yaml = Reflection.ToYaml(QueryGenerationTests.Schema);

        var writer = new StringWriter();
        var console = new StringWriter();
        TypeScript.FromSchema(ResolvedSchema.Resolve(yaml), writer, console);

        writer.ToString().Should().Be(
@"import { IntegerQueryColumn, NumericQueryColumn, QueryColumn, StringQueryColumn } from ""flowerbi"";

// Important: this file is auto-generated by flowerbi.

export const Department = {
    Id: new IntegerQueryColumn<number>(""Department.Id""),
    DepartmentName: new StringQueryColumn<string>(""Department.DepartmentName""),
};

export const Vendor = {
    Id: new IntegerQueryColumn<number>(""Vendor.Id""),
    VendorName: new StringQueryColumn<string>(""Vendor.VendorName""),
    DepartmentId: new IntegerQueryColumn<number>(""Vendor.DepartmentId""),
};

export const Invoice = {
    Id: new IntegerQueryColumn<number>(""Invoice.Id""),
    VendorId: new IntegerQueryColumn<number>(""Invoice.VendorId""),
    DepartmentId: new IntegerQueryColumn<number>(""Invoice.DepartmentId""),
    Amount: new NumericQueryColumn<number>(""Invoice.Amount""),
    Paid: new QueryColumn<boolean | null>(""Invoice.Paid""),
};

export const Tag = {
    Id: new IntegerQueryColumn<number>(""Tag.Id""),
    TagName: new StringQueryColumn<string>(""Tag.TagName""),
};

export const InvoiceTag = {
    InvoiceId: new IntegerQueryColumn<number>(""InvoiceTag.InvoiceId""),
    TagId: new IntegerQueryColumn<number>(""InvoiceTag.TagId""),
};

export const Category = {
    Id: new IntegerQueryColumn<number>(""Category.Id""),
    CategoryName: new StringQueryColumn<string>(""Category.CategoryName""),
};

export const InvoiceCategory = {
    InvoiceId: new IntegerQueryColumn<number>(""InvoiceCategory.InvoiceId""),
    CategoryId: new IntegerQueryColumn<number>(""InvoiceCategory.CategoryId""),
};

export const AnnotationName = {
    Id: new IntegerQueryColumn<number>(""AnnotationName.Id""),
    Name: new StringQueryColumn<string>(""AnnotationName.Name""),
};

export const AnnotationValue = {
    Id: new IntegerQueryColumn<number>(""AnnotationValue.Id""),
    AnnotationNameId: new IntegerQueryColumn<number>(""AnnotationValue.AnnotationNameId""),
    Value: new StringQueryColumn<string>(""AnnotationValue.Value""),
};

export const InvoiceAnnotation = {
    InvoiceId: new IntegerQueryColumn<number>(""InvoiceAnnotation.InvoiceId""),
    AnnotationValueId: new IntegerQueryColumn<number>(""InvoiceAnnotation.AnnotationValueId""),
};

export const TestSchema = {
    Department,
    Vendor,
    Invoice,
    Tag,
    InvoiceTag,
    Category,
    InvoiceCategory,
    AnnotationName,
    AnnotationValue,
    InvoiceAnnotation,
};
");
        console.ToString().Should().Be(
@"Exporting table Department
Exporting table Vendor
Exporting table Invoice
Exporting table Tag
Exporting table InvoiceTag
Exporting table Category
Exporting table InvoiceCategory
Exporting table AnnotationName
Exporting table AnnotationValue
Exporting table InvoiceAnnotation
Done.
");     
    }

    [Fact]
    public void GeneratesCSharpFromYaml()
    {   
        var yaml = Reflection.ToYaml(QueryGenerationTests.Schema);

        var writer = new StringWriter();
        var console = new StringWriter();
        CSharp.FromSchema(ResolvedSchema.Resolve(yaml), writer, "FlowerBI.Engine.Tests", console);

        writer.ToString().Should().Be(
@"#nullable enable
namespace FlowerBI.Engine.Tests;
using System;
using FlowerBI;


// Important: this file is auto-generated by flowerbi.

[DbSchema(""Testing"")]
public static class TestSchema
{
    [DbTable(""Department"")]
    public static class Department
    {
        public static readonly PrimaryKey<int> Id = new PrimaryKey<int>(""Id"");
        public static readonly Column<string> DepartmentName = new Column<string>(""DepartmentName"");
    }
    [DbTable(""Supplier"")]
    public static class Vendor
    {
        public static readonly PrimaryKey<int> Id = new PrimaryKey<int>(""Id"");
        public static readonly Column<string> VendorName = new Column<string>(""VendorName"");
        public static readonly ForeignKey<int> DepartmentId = new ForeignKey<int>(""DepartmentId"", Department.Id);
    }
    [DbTable(""Invoice"")]
    public static class Invoice
    {
        public static readonly PrimaryKey<int> Id = new PrimaryKey<int>(""Id"");
        public static readonly ForeignKey<int> VendorId = new ForeignKey<int>(""VendorId"", Vendor.Id);
        public static readonly ForeignKey<int> DepartmentId = new ForeignKey<int>(""DepartmentId"", Department.Id);
        public static readonly Column<decimal> Amount = new Column<decimal>(""FancyAmount"");
        public static readonly Column<bool?> Paid = new Column<bool?>(""Paid"");
    }
    [DbTable(""Tag"")]
    public static class Tag
    {
        public static readonly PrimaryKey<int> Id = new PrimaryKey<int>(""Id"");
        public static readonly Column<string> TagName = new Column<string>(""TagName"");
    }
    [DbTable(""InvoiceTag"")]
    public static class InvoiceTag
    {
        public static readonly ForeignKey<int> InvoiceId = new ForeignKey<int>(""InvoiceId"", Invoice.Id);
        public static readonly ForeignKey<int> TagId = new ForeignKey<int>(""TagId"", Tag.Id);
    }
    [DbTable(""Category"")]
    public static class Category
    {
        public static readonly PrimaryKey<int> Id = new PrimaryKey<int>(""Id"");
        public static readonly Column<string> CategoryName = new Column<string>(""CategoryName"");
    }
    [DbTable(""InvoiceCategory"")]
    public static class InvoiceCategory
    {
        public static readonly ForeignKey<int> InvoiceId = new ForeignKey<int>(""InvoiceId"", Invoice.Id);
        public static readonly ForeignKey<int> CategoryId = new ForeignKey<int>(""CategoryId"", Category.Id);
    }
    [DbTable(""AnnotationName"", true)]
    public static class AnnotationName
    {
        public static readonly PrimaryKey<int> Id = new PrimaryKey<int>(""Id"");
        public static readonly Column<string> Name = new Column<string>(""Name"");
    }
    [DbTable(""AnnotationValue"", true)]
    public static class AnnotationValue
    {
        public static readonly PrimaryKey<int> Id = new PrimaryKey<int>(""Id"");
        public static readonly ForeignKey<int> AnnotationNameId = new ForeignKey<int>(""AnnotationNameId"", AnnotationName.Id);
        public static readonly Column<string> Value = new Column<string>(""Value"");
    }
    [DbTable(""InvoiceAnnotation"", true)]
    public static class InvoiceAnnotation
    {
        [DbAssociative]
        public static readonly ForeignKey<int> InvoiceId = new ForeignKey<int>(""InvoiceId"", Invoice.Id);
        [DbAssociative]
        public static readonly ForeignKey<int> AnnotationValueId = new ForeignKey<int>(""AnnotationValueId"", AnnotationValue.Id);
    }
}
");

    console.ToString().Should().Be(
@"Exporting table Department
Exporting table Vendor
Exporting table Invoice
Exporting table Tag
Exporting table InvoiceTag
Exporting table Category
Exporting table InvoiceCategory
Exporting table AnnotationName
Exporting table AnnotationValue
Exporting table InvoiceAnnotation
Done.
");
    }
}
