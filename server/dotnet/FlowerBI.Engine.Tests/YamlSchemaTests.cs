namespace FlowerBI.Engine.Tests;

using FluentAssertions;
using Xunit;
using System;
using YamlDotNet.Core;
using System.Linq;
using FlowerBI.Yaml;

public class YamlSchemaTests
{
    [Fact]
    public void UnexpectedPropertyOnSchema()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema: something
anything: random
");
        a.Should().Throw<YamlException>();
    }

    [Fact]
    public void SchemaHasName()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema:
");
        a.Should().Throw<FlowerBIException>("Schema must have non-empty schema property");
    }

    [Fact]
    public void SchemaHasTables()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema: hats
");
        a.Should().Throw<FlowerBIException>().WithMessage("Schema must have non-empty tables property");
    }

    [Fact]
    public void TableHasName()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema: hats
tables:
  """": {}
");
        a.Should().Throw<FlowerBIException>().WithMessage("Table must have non-empty key");
    }

    [Fact]
    public void TableIdMustHaveSingleColumn()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema: hats
tables:
  trilby:
    id:
      a: [int]
      b: [int]
");
        a.Should().Throw<FlowerBIException>().WithMessage("Table trilby id must have a single column");
    }

    [Fact]
    public void TableMustHaveColumns()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema: hats
tables:
  trilby:
    id:
      id: [int]
");
        a.Should().Throw<FlowerBIException>().WithMessage("Table trilby must have columns (or use 'extends')");
    }

    [Fact]
    public void ColumnTypeMustNotBeEmpty()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema: hats
tables:
  trilby:
    id:
      id: [int]
    columns:
      brim: []
");
        a.Should().Throw<FlowerBIException>().WithMessage("Table trilby column brim type must be an array of length 1 or 2");
    }

    [Fact]
    public void ColumnTypeMustNotHaveMoreThanTwoElements()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema: hats
tables:
  trilby:
    id:
      id: [int]
    columns:
      brim: [short, extra, nonsense]
");
        a.Should().Throw<FlowerBIException>().WithMessage("Table trilby column brim type must be an array of length 1 or 2");
    }

    [Fact]
    public void TableIdFirstElementMustBeAType()
    {
        Action a = () => ResolvedSchema.Resolve(@"
schema: hats
tables:
  trilby:
    id: 
      id: [lemon]
    columns:
      brim: [string] 
");
        a.Should().Throw<FlowerBIException>().WithMessage("lemon is neither a data type nor a table, in trilby.id");
    }

    [Fact]
    public void MinimalSchemaPasses()
    {
        var schema = ResolvedSchema.Resolve(@"
schema: hats
tables:
  trilby:
    id:
      id: [int]
    columns:
      brim: [string]
");
        schema.Name.Should().Be("hats");
        schema.NameInDb.Should().Be("hats");
        var t = schema.Tables.Single();
        t.Name.Should().Be("trilby");        
        t.NameInDb.Should().Be("trilby");
        t.IdColumn.Name.Should().Be("id");
        t.IdColumn.DataType.Should().Be(DataType.Int);
        t.IdColumn.NameInDb.Should().Be("id");
        var c = t.Columns.Single();
        c.Name.Should().Be("brim");
        c.DataType.Should().Be(DataType.String);
        c.Nullable.Should().BeFalse();
        c.NameInDb.Should().Be("brim");
        c.Extends.Should().BeNull();
    }

    [Fact]
    public void IdIsOptional()
    {
        var schema = ResolvedSchema.Resolve(@"
schema: hats
tables:
  trilby:
    columns:
      brim: [string]
");
        schema.Name.Should().Be("hats");
        schema.NameInDb.Should().Be("hats");
        var t = schema.Tables.Single();
        t.Name.Should().Be("trilby");        
        t.NameInDb.Should().Be("trilby");
        t.IdColumn.Should().BeNull();        
        var c = t.Columns.Single();
        c.Name.Should().Be("brim");
        c.DataType.Should().Be(DataType.String);
        c.Nullable.Should().BeFalse();
        c.NameInDb.Should().Be("brim");
        c.Extends.Should().BeNull();
    }

    [Fact]
    public void ColumnTypeCanBeNullable()
    {
        var schema = ResolvedSchema.Resolve(@"
schema: hats
tables:
  trilby:
    id:
      id: [int]
    columns:
      brim: [string?]
");
        var c = schema.Tables.Single().Columns.Single();
        c.Name.Should().Be("brim");
        c.DataType.Should().Be(DataType.String);
        c.Nullable.Should().BeTrue();
    }

    [Fact]
    public void DbNamesCanBeOverridden()
    {
        var schema = ResolvedSchema.Resolve(@"
schema: hats
name: NiceHats
tables:
  trilby:
    name: Trilby
    id:
      id: [int]
    columns:
      brim: [string, Brim]
");
        schema.Name.Should().Be("hats");
        schema.NameInDb.Should().Be("NiceHats");
        var t = schema.Tables.Single();
        t.Name.Should().Be("trilby");
        t.NameInDb.Should().Be("Trilby");
        t.IdColumn.Name.Should().Be("id");
        t.IdColumn.DataType.Should().Be(DataType.Int);
        var c = t.Columns.Single();
        c.Name.Should().Be("brim");
        c.DataType.Should().Be(DataType.String);
        c.NameInDb.Should().Be("Brim");
    }

    [Fact]
    public void TableCanExtendOtherTable()
    {
        var schema = ResolvedSchema.Resolve(@"
schema: hats
name: NiceHats
tables:

  trilby:
    name: Trilby
    id:
      id: [int]
    columns:
      brim: [string] 

  trilby2:
    extends: trilby
");
        schema.Name.Should().Be("hats");
        schema.NameInDb.Should().Be("NiceHats");
        var t = schema.Tables.Single(x => x.Name == "trilby");
        t.Name.Should().Be("trilby");
        t.NameInDb.Should().Be("Trilby");
        t.IdColumn.Name.Should().Be("id");
        t.IdColumn.DataType.Should().Be(DataType.Int);
        t.IdColumn.Extends.Should().Be(null);
        var c = t.Columns.Single();
        c.Name.Should().Be("brim");
        c.DataType.Should().Be(DataType.String);
        c.Extends.Should().Be(null);
        var t2 = schema.Tables.Single(x => x.Name == "trilby2");
        t2.Name.Should().Be("trilby2");
        t2.NameInDb.Should().Be("Trilby");
        t2.IdColumn.Name.Should().Be("id");
        t2.IdColumn.DataType.Should().Be(DataType.Int);
        t2.IdColumn.Extends.Should().Be(t.IdColumn);
        var c2 = t2.Columns.Single();
        c2.Name.Should().Be("brim");
        c2.DataType.Should().Be(DataType.String);
        c2.Extends.Should().Be(c);
    }

    [Fact]
    public void TableCanExtendOtherTableOverridingDbNameAndAddingColumns()
    {
        var schema = ResolvedSchema.Resolve(@"
schema: hats
name: NiceHats
tables:

  trilby:
    name: Trilby
    id:
      id: [int]
    columns:
      brim: [string]

  trilby2:
    extends: trilby
    name: Trilby2
    columns:
      extra: [decimal]
");
        schema.Name.Should().Be("hats");
        schema.NameInDb.Should().Be("NiceHats");
        var t = schema.Tables.Single(x => x.Name == "trilby2");
        t.NameInDb.Should().Be("Trilby2");
        var c = t.Columns.Single(x => x.Name == "brim");        
        c.DataType.Should().Be(DataType.String);
        c = t.Columns.Single(x => x.Name == "extra");        
        c.DataType.Should().Be(DataType.Decimal);
    }

    [Fact]
    public void TableCanHaveForeignKey()
    {
        var schema = ResolvedSchema.Resolve(@"
schema: hats
name: NiceHats
tables:

  trilby:
    name: Trilby
    id:
      id: [int]
    columns:
      brim: [brimfo]

  brimfo:
    id:
      bid: [short]
    columns:
      extra: [decimal]
");
        var t = schema.Tables.Single(x => x.Name == "trilby");
        var b = schema.Tables.Single(x => x.Name == "brimfo");
        
        var e = b.Columns.Single(x => x.Name == "extra");
        e.DataType.Should().Be(DataType.Decimal);

        var c = t.Columns.Single(x => x.Name == "brim");
        c.DataType.Should().Be(DataType.Short);
        c.Target.Should().Be(b.IdColumn);
        c.Nullable.Should().BeFalse();
    }

    [Fact]
    public void ForeignKeyCanBeNullable()
    {
        var schema = ResolvedSchema.Resolve(@"
schema: hats
name: NiceHats
tables:
  trilby:
    name: Trilby
    id:
      id: [int]
    columns:
      brim: [brimfo?]
  brimfo:
    id:
      bid: [short]
    columns:
      extra: [decimal]
");
        var t = schema.Tables.Single(x => x.Name == "trilby");
        var b = schema.Tables.Single(x => x.Name == "brimfo");
        
        var e = b.Columns.Single(x => x.Name == "extra");
        e.DataType.Should().Be(DataType.Decimal);

        var c = t.Columns.Single(x => x.Name == "brim");
        c.DataType.Should().Be(DataType.Short);
        c.Target.Should().Be(b.IdColumn);
        c.Nullable.Should().BeTrue();
    }
}