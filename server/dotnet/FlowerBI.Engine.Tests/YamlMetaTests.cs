namespace FlowerBI.Engine.Tests;

using System.IO;
using FlowerBI.Conversion;
using FlowerBI.Yaml;
using FluentAssertions;
using Xunit;

public class YamlMetaTests
{
    [Fact]
    public void TableCanHaveMeta()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            tables:
              trilby:
                meta:
                  owner: millinery
                  pii: "false"
                id:
                  id: [int]
                columns:
                  brim: [string]
            """
        );

        var trilby = schema.GetTable("trilby");
        trilby.Meta.Should().HaveCount(2);
        trilby.Meta["owner"].Should().Be("millinery");
        trilby.Meta["pii"].Should().Be("false");
    }

    [Fact]
    public void ColumnLongFormCanHaveMeta()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            tables:
              trilby:
                id:
                  id: [int]
                columns:
                  brim:
                    type: string
                    meta:
                      unit: cm
                      sensitive: "true"
            """
        );

        var brim = schema.GetTable("trilby").GetColumn("brim");
        brim.Meta.Should().HaveCount(2);
        brim.Meta["unit"].Should().Be("cm");
        brim.Meta["sensitive"].Should().Be("true");
    }

    [Fact]
    public void ShortFormColumnHasEmptyMeta()
    {
        var schema = Schema.FromYaml(
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

        schema.GetTable("trilby").GetColumn("brim").Meta.Should().BeEmpty();
        schema.GetTable("trilby").Meta.Should().BeEmpty();
    }

    [Fact]
    public void DerivedTableMergesMetaWithBasePerKey()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            tables:
              base:
                meta:
                  owner: shared
                  layer: base
                id:
                  id: [int]
                columns:
                  brim: [string]
              derived:
                extends: base
                meta:
                  layer: derived
                  extra: yes
            """
        );

        var derived = schema.GetTable("derived");
        derived.Meta.Should().HaveCount(3);
        derived.Meta["owner"].Should().Be("shared"); // inherited from base
        derived.Meta["layer"].Should().Be("derived"); // overridden by derived
        derived.Meta["extra"].Should().Be("yes"); // added by derived

        // The base is untouched by the merge.
        schema.GetTable("base").Meta.Should().HaveCount(2);
        schema.GetTable("base").Meta["layer"].Should().Be("base");
    }

    [Fact]
    public void DerivedTableInheritsBaseColumnMeta()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            tables:
              base:
                id:
                  id: [int]
                columns:
                  brim:
                    type: string
                    meta:
                      unit: cm
              derived:
                extends: base
            """
        );

        var brim = schema.GetTable("derived").GetColumn("brim");
        brim.Meta.Should().HaveCount(1);
        brim.Meta["unit"].Should().Be("cm");
    }

    [Fact]
    public void MetaMustBeAMapping()
    {
        System.Action a = () =>
            ResolvedSchema.Resolve(
                """
                schema: hats
                tables:
                  trilby:
                    id:
                      id: [int]
                    columns:
                      brim:
                        type: string
                        meta: not-a-mapping
                """
            );

        a.Should().Throw<FlowerBIException>().WithMessage("*meta must be a mapping*");
    }

    private const string MetaYaml = """
        schema: TestSchema
        tables:
            Widget:
                meta:
                    owner: platform
                id:
                    Id: [int]
                columns:
                    Name:
                        type: string
                        meta:
                            unit: none
                            pii: "true"
        """;

    [Fact]
    public void GeneratesTypeScriptWithMeta()
    {
        var schema = ResolvedSchema.Resolve(MetaYaml);

        var writer = new StringWriter();
        var console = new StringWriter();
        TypeScript.FromSchema(schema, writer, console);

        var output = writer.ToString();

        // Column meta becomes a third argument to QueryColumnRuntimeType.
        output.Should().Contain("\"unit\": \"none\"");
        output.Should().Contain("\"pii\": \"true\"");
        // Table meta becomes a $meta key on the table object literal.
        output.Should().Contain("$meta: { \"owner\": \"platform\" }");
    }

    [Fact]
    public void GeneratedCSharpEmbedsMetaForRuntime()
    {
        var schema = ResolvedSchema.Resolve(MetaYaml);

        var writer = new StringWriter();
        var console = new StringWriter();
        CSharp.FromSchema(schema, MetaYaml, writer, "FlowerBI.Engine.Tests", console);

        var output = writer.ToString();

        // C# surfaces meta at runtime via the embedded YAML round-trip, so the meta
        // must appear inside the embedded schema string.
        output.Should().Contain("owner: platform");
        output.Should().Contain("pii: \"true\"");

        // And the round-trip actually reconstructs it.
        var roundTripped = Schema.FromYaml(MetaYaml);
        roundTripped.GetTable("Widget").Meta["owner"].Should().Be("platform");
        roundTripped.GetTable("Widget").GetColumn("Name").Meta["pii"].Should().Be("true");
    }
}
