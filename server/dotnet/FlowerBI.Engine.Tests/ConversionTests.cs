namespace FlowerBI.Engine.Tests;

using System.IO;
using FlowerBI.Conversion;
using FlowerBI.Yaml;
using FluentAssertions;
using Xunit;

public class ConversionTests
{
    private const string TestYaml = """
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
        """;

    [Fact]
    public void GeneratesTypeScriptFromYaml()
    {
        var schema = ResolvedSchema.Resolve(TestYaml);

        var writer = new StringWriter();
        var console = new StringWriter();
        TypeScript.FromSchema(schema, writer, console);

        var output = writer.ToString().Trim();

        // Verify the structure of the generated TypeScript
        output.Should().Contain("import {");
        output.Should().Contain("} from \"flowerbi\"");
        output.Should().Contain("export const Department = {");
        output.Should().Contain("Id: new IntegerQueryColumn<number>(\"Department.Id\"");
        output.Should().Contain("QueryColumnDataType.Long");
        output.Should().Contain("DepartmentName: new StringQueryColumn<string>(\"Department.DepartmentName\"");
        output.Should().Contain("export const Vendor = {");
        output.Should().Contain("VendorName: new StringQueryColumn<string>(\"Vendor.VendorName\"");
        output.Should().Contain("DepartmentId: new IntegerQueryColumn<number>(\"Vendor.DepartmentId\"");
        output.Should().Contain("\"Department.Id\""); // FK target
        output.Should().Contain("export const Invoice = {");
        output.Should().Contain("Amount: new NumericQueryColumn<number>(\"Invoice.Amount\"");
        output.Should().Contain("QueryColumnDataType.Decimal");
        output.Should().Contain("Paid: new QueryColumn<boolean | null>(\"Invoice.Paid\"");
        output.Should().Contain("export const TestSchema = {");
    }

    [Fact]
    public void GeneratesCSharpFromYaml()
    {
        var schema = ResolvedSchema.Resolve(TestYaml);

        var writer = new StringWriter();
        var console = new StringWriter();
        CSharp.FromSchema(schema, TestYaml, writer, "FlowerBI.Engine.Tests", console);

        var output = writer.ToString().Trim();

        // Verify the structure of the generated code
        output.Should().Contain("namespace FlowerBI.Engine.Tests;");
        output.Should().Contain("using FlowerBI;");
        output.Should().Contain("public static class TestSchema");
        output.Should().Contain("public static class Department");
        output.Should().Contain("public const string Id = \"Department.Id\";");
        output.Should().Contain("public static class Vendor");
        output.Should().Contain("public const string VendorName = \"Vendor.VendorName\";");
        output.Should().Contain("public static class Invoice");
        output.Should().Contain("public const string Amount = \"Invoice.Amount\";");
        output.Should().Contain("private const string YamlSchema = \"\"\"");
        output.Should().Contain("public static Schema Schema { get; } = FlowerBI.Schema.FromYaml(YamlSchema);");
    }
}
