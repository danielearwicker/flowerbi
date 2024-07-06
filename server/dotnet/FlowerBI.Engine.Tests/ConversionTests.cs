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
        var yaml = Reflection.ToYaml(ExecutionTests.Schema);

        var writer = new StringWriter();
        Reflection.Serialize(yaml, writer);

        writer.ToString().Should().Be(
@"schema: TestSchema
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
        associative: [InvoiceId, AnnotationValueId]
        columns:
            InvoiceId: [Invoice]
            AnnotationValueId: [AnnotationValue]
");
    }

    [Fact]
    public void GeneratesTypeScriptFromYaml()
    {   
        var yaml = Reflection.ToYaml(ExecutionTests.Schema);

        var writer = new StringWriter();
        var console = new StringWriter();
        TypeScript.FromSchema(ResolvedSchema.Resolve(yaml), writer, console);

        writer.ToString().Trim().Should().Be(
"""
import { IntegerQueryColumn, NumericQueryColumn, QueryColumn, QueryColumnDataType, QueryColumnRuntimeType, StringQueryColumn } from "flowerbi";

// Important: this file is auto-generated by flowerbi.

export const Department = {
    Id: new IntegerQueryColumn<number>("Department.Id",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        ""
    )
), 
    DepartmentName: new StringQueryColumn<string>("Department.DepartmentName",
    new QueryColumnRuntimeType(
        QueryColumnDataType.String,
        ""
    )
), 
};

export const Vendor = {
    Id: new IntegerQueryColumn<number>("Vendor.Id",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        ""
    )
), 
    VendorName: new StringQueryColumn<string>("Vendor.VendorName",
    new QueryColumnRuntimeType(
        QueryColumnDataType.String,
        ""
    )
), 
    DepartmentId: new IntegerQueryColumn<number>("Vendor.DepartmentId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "Department.Id"
    )
), 
};

export const Invoice = {
    Id: new IntegerQueryColumn<number>("Invoice.Id",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        ""
    )
), 
    VendorId: new IntegerQueryColumn<number>("Invoice.VendorId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "Vendor.Id"
    )
), 
    DepartmentId: new IntegerQueryColumn<number>("Invoice.DepartmentId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "Department.Id"
    )
), 
    Amount: new NumericQueryColumn<number>("Invoice.Amount",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Decimal,
        ""
    )
), 
    Paid: new QueryColumn<boolean | null>("Invoice.Paid",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Bool,
        ""
    )
), 
};

export const Tag = {
    Id: new IntegerQueryColumn<number>("Tag.Id",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        ""
    )
), 
    TagName: new StringQueryColumn<string>("Tag.TagName",
    new QueryColumnRuntimeType(
        QueryColumnDataType.String,
        ""
    )
), 
};

export const InvoiceTag = {
    InvoiceId: new IntegerQueryColumn<number>("InvoiceTag.InvoiceId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "Invoice.Id"
    )
), 
    TagId: new IntegerQueryColumn<number>("InvoiceTag.TagId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "Tag.Id"
    )
), 
};

export const Category = {
    Id: new IntegerQueryColumn<number>("Category.Id",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        ""
    )
), 
    CategoryName: new StringQueryColumn<string>("Category.CategoryName",
    new QueryColumnRuntimeType(
        QueryColumnDataType.String,
        ""
    )
), 
};

export const InvoiceCategory = {
    InvoiceId: new IntegerQueryColumn<number>("InvoiceCategory.InvoiceId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "Invoice.Id"
    )
), 
    CategoryId: new IntegerQueryColumn<number>("InvoiceCategory.CategoryId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "Category.Id"
    )
), 
};

export const AnnotationName = {
    Id: new IntegerQueryColumn<number>("AnnotationName.Id",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        ""
    )
), 
    Name: new StringQueryColumn<string>("AnnotationName.Name",
    new QueryColumnRuntimeType(
        QueryColumnDataType.String,
        ""
    )
), 
};

export const AnnotationValue = {
    Id: new IntegerQueryColumn<number>("AnnotationValue.Id",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        ""
    )
), 
    AnnotationNameId: new IntegerQueryColumn<number>("AnnotationValue.AnnotationNameId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "AnnotationName.Id"
    )
), 
    Value: new StringQueryColumn<string>("AnnotationValue.Value",
    new QueryColumnRuntimeType(
        QueryColumnDataType.String,
        ""
    )
), 
};

export const InvoiceAnnotation = {
    InvoiceId: new IntegerQueryColumn<number>("InvoiceAnnotation.InvoiceId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "Invoice.Id"
    )
), 
    AnnotationValueId: new IntegerQueryColumn<number>("InvoiceAnnotation.AnnotationValueId",
    new QueryColumnRuntimeType(
        QueryColumnDataType.Long,
        "AnnotationValue.Id"
    )
), 
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
""");
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
        var yaml = Reflection.ToYaml(ExecutionTests.Schema);

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
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>(""Id"");
        public static readonly Column<string> DepartmentName = new Column<string>(""DepartmentName"");
    }
    [DbTable(""Supplier"")]
    public static class Vendor
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>(""Id"");
        public static readonly Column<string> VendorName = new Column<string>(""VendorName"");
        public static readonly ForeignKey<long> DepartmentId = new ForeignKey<long>(""DepartmentId"", Department.Id);
    }
    [DbTable(""Invoice"")]
    public static class Invoice
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>(""Id"");
        public static readonly ForeignKey<long> VendorId = new ForeignKey<long>(""VendorId"", Vendor.Id);
        public static readonly ForeignKey<long> DepartmentId = new ForeignKey<long>(""DepartmentId"", Department.Id);
        public static readonly Column<decimal> Amount = new Column<decimal>(""FancyAmount"");
        public static readonly Column<bool?> Paid = new Column<bool?>(""Paid"");
    }
    [DbTable(""Tag"")]
    public static class Tag
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>(""Id"");
        public static readonly Column<string> TagName = new Column<string>(""TagName"");
    }
    [DbTable(""InvoiceTag"")]
    public static class InvoiceTag
    {
        public static readonly ForeignKey<long> InvoiceId = new ForeignKey<long>(""InvoiceId"", Invoice.Id);
        public static readonly ForeignKey<long> TagId = new ForeignKey<long>(""TagId"", Tag.Id);
    }
    [DbTable(""Category"")]
    public static class Category
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>(""Id"");
        public static readonly Column<string> CategoryName = new Column<string>(""CategoryName"");
    }
    [DbTable(""InvoiceCategory"")]
    public static class InvoiceCategory
    {
        public static readonly ForeignKey<long> InvoiceId = new ForeignKey<long>(""InvoiceId"", Invoice.Id);
        public static readonly ForeignKey<long> CategoryId = new ForeignKey<long>(""CategoryId"", Category.Id);
    }
    [DbTable(""AnnotationName"", true)]
    public static class AnnotationName
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>(""Id"");
        public static readonly Column<string> Name = new Column<string>(""Name"");
    }
    [DbTable(""AnnotationValue"", true)]
    public static class AnnotationValue
    {
        public static readonly PrimaryKey<long> Id = new PrimaryKey<long>(""Id"");
        public static readonly ForeignKey<long> AnnotationNameId = new ForeignKey<long>(""AnnotationNameId"", AnnotationName.Id);
        public static readonly Column<string> Value = new Column<string>(""Value"");
    }
    [DbTable(""InvoiceAnnotation"", true)]
    public static class InvoiceAnnotation
    {
        [DbAssociative]
        public static readonly ForeignKey<long> InvoiceId = new ForeignKey<long>(""InvoiceId"", Invoice.Id);
        [DbAssociative]
        public static readonly ForeignKey<long> AnnotationValueId = new ForeignKey<long>(""AnnotationValueId"", AnnotationValue.Id);
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
