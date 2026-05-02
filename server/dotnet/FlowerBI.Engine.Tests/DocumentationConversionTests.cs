namespace FlowerBI.Engine.Tests;

using System.IO;
using FlowerBI.Conversion;
using FlowerBI.Yaml;
using FluentAssertions;
using Xunit;

public class DocumentationConversionTests
{
    private const string DocumentedYaml = """
        schema: TestSchema
        topics:
            billing: |
                Monetary columns are in original currency.
            tenancy:
                doc: Row-level security applies.
                see: [Customer]
        tables:
            Customer:
                doc: One row per billed customer.
                see: [tenancy, Invoice]
                id:
                    Id: [int]
                columns:
                    CustomerName:
                        type: string
                        doc: Display name.
            Invoice:
                id:
                    Id: [int]
                columns:
                    CustomerId: [Customer]
                    Amount:
                        type: decimal
                        doc: Gross amount in original currency.
                        see: [billing, Customer.CustomerName]
        """;

    [Fact]
    public void TypeScriptEmitsJsDocForTablesAndColumns()
    {
        var schema = ResolvedSchema.Resolve(DocumentedYaml);
        var writer = new StringWriter();
        TypeScript.FromSchema(schema, writer, new StringWriter());
        var output = writer.ToString().Replace("\r\n", "\n");

        output.Should().Contain("/**\n * One row per billed customer.\n");
        output.Should().Contain("export const Customer = {");

        output.Should().Contain(" * Display name.");
        output.Should().Contain("CustomerName: new StringQueryColumn");

        output.Should().Contain(" * Gross amount in original currency.");
    }

    [Fact]
    public void TypeScriptRewritesTopicSeeRefsToTopicsConst()
    {
        var schema = ResolvedSchema.Resolve(DocumentedYaml);
        var writer = new StringWriter();
        TypeScript.FromSchema(schema, writer, new StringWriter());
        var output = writer.ToString();

        // Topic ref in table see list is rewritten to Topics.tenancy
        output.Should().Contain("@see {@link Topics.tenancy}");
        // Table ref stays as-is
        output.Should().Contain("@see {@link Invoice}");
        // Column ref stays dotted
        output.Should().Contain("@see {@link Customer.CustomerName}");
        // Topic ref in column see also rewritten
        output.Should().Contain("@see {@link Topics.billing}");
    }

    [Fact]
    public void TypeScriptEmitsTopicsConstWithDocs()
    {
        var schema = ResolvedSchema.Resolve(DocumentedYaml);
        var writer = new StringWriter();
        TypeScript.FromSchema(schema, writer, new StringWriter());
        var output = writer.ToString();

        output.Should().Contain("export const Topics = {");
        output.Should().Contain("billing: \"billing\"");
        output.Should().Contain("tenancy: \"tenancy\"");
        output.Should().Contain(" * Monetary columns are in original currency.");
        output.Should().Contain(" * Row-level security applies.");
    }

    [Fact]
    public void TypeScriptOmitsJsDocForUndocumentedItems()
    {
        var schema = ResolvedSchema.Resolve(
            """
            schema: hats
            tables:
              trilby:
                id:
                  id: [int]
                columns:
                  brim: [string]
            """
        );
        var writer = new StringWriter();
        TypeScript.FromSchema(schema, writer, new StringWriter());
        var output = writer.ToString();

        output.Should().NotContain("/**");
        output.Should().NotContain("export const Topics");
    }

    [Fact]
    public void CSharpEmitsXmlDocForTablesAndColumns()
    {
        var schema = ResolvedSchema.Resolve(DocumentedYaml);
        var writer = new StringWriter();
        CSharp.FromSchema(schema, DocumentedYaml, writer, "TestNs", new StringWriter());
        var output = writer.ToString();

        output.Should().Contain("/// <summary>One row per billed customer.</summary>");
        output.Should().Contain("public static class Customer");
        output.Should().Contain("/// <summary>Display name.</summary>");
        output.Should().Contain("public const string CustomerName = \"Customer.CustomerName\";");
        output.Should().Contain("/// <summary>Gross amount in original currency.</summary>");
    }

    [Fact]
    public void CSharpRewritesTopicSeeRefsToTopicsConst()
    {
        var schema = ResolvedSchema.Resolve(DocumentedYaml);
        var writer = new StringWriter();
        CSharp.FromSchema(schema, DocumentedYaml, writer, "TestNs", new StringWriter());
        var output = writer.ToString();

        output.Should().Contain("/// <seealso cref=\"Topics.tenancy\"/>");
        output.Should().Contain("/// <seealso cref=\"Invoice\"/>");
        output.Should().Contain("/// <seealso cref=\"Customer.CustomerName\"/>");
        output.Should().Contain("/// <seealso cref=\"Topics.billing\"/>");
    }

    [Fact]
    public void CSharpEmitsTopicsClassWithConstantsAndDocs()
    {
        var schema = ResolvedSchema.Resolve(DocumentedYaml);
        var writer = new StringWriter();
        CSharp.FromSchema(schema, DocumentedYaml, writer, "TestNs", new StringWriter());
        var output = writer.ToString();

        output.Should().Contain("public static class Topics");
        output.Should().Contain("public const string billing = \"billing\";");
        output.Should().Contain("public const string tenancy = \"tenancy\";");
        output
            .Should()
            .Contain("/// <summary>Monetary columns are in original currency.</summary>");
        output.Should().Contain("/// <summary>Row-level security applies.</summary>");
    }

    [Fact]
    public void CSharpEscapesXmlSpecialCharsInDoc()
    {
        var schema = ResolvedSchema.Resolve(
            """
            schema: hats
            tables:
              trilby:
                doc: "A < B & C > D"
                id:
                  id: [int]
                columns:
                  brim: [string]
            """
        );
        var writer = new StringWriter();
        CSharp.FromSchema(schema, "", writer, "TestNs", new StringWriter());
        var output = writer.ToString();

        output.Should().Contain("/// <summary>A &lt; B &amp; C &gt; D</summary>");
    }

    [Fact]
    public void TopicNameWithHyphenSanitisedInCSharpButPreservedAsString()
    {
        var schema = ResolvedSchema.Resolve(
            """
            schema: hats
            topics:
              currency-codes: ISO 4217.
            tables:
              trilby:
                see: [currency-codes]
                id:
                  id: [int]
                columns:
                  brim: [string]
            """
        );
        var writer = new StringWriter();
        CSharp.FromSchema(schema, "", writer, "TestNs", new StringWriter());
        var output = writer.ToString();

        output.Should().Contain("public const string currency_codes = \"currency-codes\";");
        output.Should().Contain("/// <seealso cref=\"Topics.currency_codes\"/>");
    }

    [Fact]
    public void TopicNameWithHyphenQuotedAsTsObjectKey()
    {
        var schema = ResolvedSchema.Resolve(
            """
            schema: hats
            topics:
              currency-codes: ISO 4217.
            tables:
              trilby:
                see: [currency-codes]
                id:
                  id: [int]
                columns:
                  brim: [string]
            """
        );
        var writer = new StringWriter();
        TypeScript.FromSchema(schema, writer, new StringWriter());
        var output = writer.ToString();

        // Quoted key for invalid identifier
        output.Should().Contain("\"currency-codes\": \"currency-codes\"");
        // @see uses bracket notation for non-identifier topic names
        output.Should().Contain("@see {@link Topics[\"currency-codes\"]}");
    }
}
