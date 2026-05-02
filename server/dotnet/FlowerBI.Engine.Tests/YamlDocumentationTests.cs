namespace FlowerBI.Engine.Tests;

using System;
using System.Linq;
using FlowerBI.Yaml;
using FluentAssertions;
using Xunit;

public class YamlDocumentationTests
{
    [Fact]
    public void TableCanHaveDocAndSee()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            tables:
              trilby:
                doc: A formal hat with a narrow brim.
                see: [bowler]
                id:
                  id: [int]
                columns:
                  brim: [string]
              bowler:
                id:
                  id: [int]
                columns:
                  height: [int]
            """
        );
        var trilby = schema.GetTable("trilby");
        trilby.Doc.Should().Be("A formal hat with a narrow brim.");
        trilby.See.Should().HaveCount(1);
        trilby.See[0].Should().BeSameAs(schema.GetTable("bowler"));
    }

    [Fact]
    public void ColumnLongFormCarriesDocAndSee()
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
                    name: BrimColumn
                    doc: Width of the brim in centimetres.
                    see: [trilby.id]
            """
        );
        var brim = schema.GetTable("trilby").GetColumn("brim");
        brim.DataType.Should().Be(DataType.String);
        brim.DbName.Should().Be("BrimColumn");
        brim.Doc.Should().Be("Width of the brim in centimetres.");
        brim.See.Should().HaveCount(1);
        brim.See[0].Should().BeSameAs(schema.GetTable("trilby").Id);
    }

    [Fact]
    public void TopicsShortAndLongFormBothWork()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            topics:
              shortform: |
                Short-form topic, just prose.
              longform:
                doc: |
                  Long-form topic with a see list.
                see: [shortform]
            tables:
              trilby:
                id:
                  id: [int]
                columns:
                  brim: [string]
            """
        );
        schema.Topics.Should().HaveCount(2);
        var shortTopic = schema.GetTopic("shortform");
        shortTopic.Doc.Should().Be("Short-form topic, just prose.\n");
        shortTopic.See.Should().BeEmpty();

        var longTopic = schema.GetTopic("longform");
        longTopic.Doc.Should().Be("Long-form topic with a see list.\n");
        longTopic.See.Should().HaveCount(1);
        longTopic.See[0].Should().BeSameAs(shortTopic);
    }

    [Fact]
    public void SeeCanReferenceTopicsTablesAndColumns()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            topics:
              billing: Currency notes.
            tables:
              trilby:
                doc: A trilby.
                see: [billing, bowler, bowler.height]
                id:
                  id: [int]
                columns:
                  brim: [string]
              bowler:
                id:
                  id: [int]
                columns:
                  height: [int]
            """
        );
        var trilby = schema.GetTable("trilby");
        trilby.See.Should().HaveCount(3);
        trilby.See[0].Should().BeOfType<Topic>().Which.RefName.Should().Be("billing");
        trilby.See[1].Should().BeSameAs(schema.GetTable("bowler"));
        trilby.See[2].Should().BeSameAs(schema.GetTable("bowler").GetColumn("height"));
    }

    [Fact]
    public void TopicNameMustNotCollideWithTableName()
    {
        Action a = () =>
            Schema.FromYaml(
                """
                schema: hats
                topics:
                  trilby: Topic prose.
                tables:
                  trilby:
                    id:
                      id: [int]
                    columns:
                      brim: [string]
                """
            );
        a.Should()
            .Throw<FlowerBIException>()
            .WithMessage("*topic and table names must not collide*");
    }

    [Fact]
    public void UnknownSeeReferenceFails()
    {
        Action a = () =>
            Schema.FromYaml(
                """
                schema: hats
                tables:
                  trilby:
                    see: [nope]
                    id:
                      id: [int]
                    columns:
                      brim: [string]
                """
            );
        a.Should().Throw<FlowerBIException>().WithMessage("*'nope'*neither*");
    }

    [Fact]
    public void UnknownColumnInDottedSeeFails()
    {
        Action a = () =>
            Schema.FromYaml(
                """
                schema: hats
                tables:
                  trilby:
                    see: [trilby.missing]
                    id:
                      id: [int]
                    columns:
                      brim: [string]
                """
            );
        a.Should().Throw<FlowerBIException>().WithMessage("*unknown column*");
    }

    [Fact]
    public void ColumnLongFormUnknownPropertyFails()
    {
        Action a = () =>
            Schema.FromYaml(
                """
                schema: hats
                tables:
                  trilby:
                    id:
                      id: [int]
                    columns:
                      brim:
                        type: string
                        nonsense: oops
                """
            );
        a.Should().Throw<FlowerBIException>().WithMessage("*unknown property 'nonsense'*");
    }

    [Fact]
    public void ColumnLongFormMustHaveType()
    {
        Action a = () =>
            Schema.FromYaml(
                """
                schema: hats
                tables:
                  trilby:
                    id:
                      id: [int]
                    columns:
                      brim:
                        doc: Missing type.
                """
            );
        a.Should().Throw<FlowerBIException>().WithMessage("*must specify a 'type'*");
    }

    [Fact]
    public void TopicsCanCrossReferenceEachOther()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            topics:
              a:
                doc: A.
                see: [b]
              b:
                doc: B.
                see: [a]
            tables:
              trilby:
                id:
                  id: [int]
                columns:
                  brim: [string]
            """
        );
        var a = schema.GetTopic("a");
        var b = schema.GetTopic("b");
        a.See.Single().Should().BeSameAs(b);
        b.See.Single().Should().BeSameAs(a);
    }

    [Fact]
    public void DocAndSeeAreInheritedViaExtends()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            topics:
              wear: How to wear it.
            tables:
              base:
                doc: Base hat.
                see: [wear]
                id:
                  id: [int]
                columns:
                  brim:
                    type: string
                    doc: The brim.
                    see: [wear]
              child:
                extends: base
                name: ChildTable
            """
        );
        var child = schema.GetTable("child");
        child.Doc.Should().Be("Base hat.");
        child.See.Single().RefName.Should().Be("wear");

        var brim = child.GetColumn("brim");
        brim.Doc.Should().Be("The brim.");
        brim.See.Single().RefName.Should().Be("wear");
    }

    [Fact]
    public void ChildTableCanOverrideDocAndSee()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            topics:
              w1: First topic.
              w2: Second topic.
            tables:
              base:
                doc: Base doc.
                see: [w1]
                id:
                  id: [int]
                columns:
                  brim: [string]
              child:
                extends: base
                doc: Child doc.
                see: [w2]
            """
        );
        var child = schema.GetTable("child");
        child.Doc.Should().Be("Child doc.");
        child.See.Single().RefName.Should().Be("w2");
    }

    [Fact]
    public void ColumnsAndTablesImplementIDocumented()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            tables:
              trilby:
                doc: A trilby.
                id:
                  id: [int]
                columns:
                  brim: [string]
            """
        );
        IDocumented table = schema.GetTable("trilby");
        IDocumented col = schema.GetTable("trilby").GetColumn("brim");
        IDocumented topic = new Topic_Probe();

        table.Doc.Should().Be("A trilby.");
        col.Doc.Should().BeNull();
        // Just exercising the interface contract; topic check is structural.
        topic.Should().NotBeNull();
    }

    private sealed class Topic_Probe : IDocumented
    {
        public string DbName => null;
        public string RefName => "probe";
        public string Doc => null;
        public System.Collections.Generic.IReadOnlyList<IDocumented> See =>
            Array.Empty<IDocumented>();
    }

    [Fact]
    public void EmptySeeListOnChildClearsInheritedSee()
    {
        var schema = Schema.FromYaml(
            """
            schema: hats
            topics:
              w1: Topic.
            tables:
              base:
                see: [w1]
                id:
                  id: [int]
                columns:
                  brim: [string]
              child:
                extends: base
                see: []
            """
        );
        schema.GetTable("base").See.Should().HaveCount(1);
        schema.GetTable("child").See.Should().BeEmpty();
    }
}
