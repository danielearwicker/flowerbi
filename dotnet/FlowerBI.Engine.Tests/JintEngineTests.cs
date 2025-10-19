using System;
using System.IO;
using FlowerBI;
using FlowerBI.Engine.JsonModels;
using FlowerBI.Jint;
using Xunit;

namespace FlowerBI.Engine.Tests;

public class JintEngineTests
{
    private const string TestYaml = @"
schema: TestSchema
tables:
  User:
    id:
      Id: [int]
    columns:
      Name: [string]
      Email: [string]
      IsActive: [bool]
  Order:
    id:
      Id: [int]
    columns:
      UserId: [User]
      Total: [decimal]
      OrderDate: [datetime]
";

    [Fact]
    public void CanCreateSchemaFromYaml()
    {
        using var schema = FlowerBIEngine.SchemaFromYaml(TestYaml);
        
        Assert.Equal("TestSchema", schema.Name);
        Assert.NotNull(schema.DbName);
    }

    [Fact]
    public void CanCreateQueryEngine()
    {
        using var schema = FlowerBIEngine.SchemaFromYaml(TestYaml);
        var queryEngine = schema.CreateQueryEngine("sqlite");
        
        Assert.NotNull(queryEngine);
    }

    [Fact]
    public void CanPrepareQuery()
    {
        using var schema = FlowerBIEngine.SchemaFromYaml(TestYaml);
        using var query = FlowerBIEngine.QueryFromJson(new QueryJson
        {
            Select = new[] { "User.Name" },
            Aggregations = new[] { new AggregationJson { Function = AggregationType.Count, Column = "Order.Id" } },
            Filters = new[] { new FilterJson { Column = "User.IsActive", Operator = "=", Value = true } }
        }, schema, "sqlite");

        var prepared = query.GetPreparedQuery();
        
        Assert.NotNull(prepared.Sql);
        Assert.NotEmpty(prepared.Sql);
        Assert.NotNull(prepared.Parameters);
    }

    [Fact]
    public void CanGenerateTypeScript()
    {
        var tsCode = FlowerBIEngine.GenerateTypeScript(TestYaml);
        
        Assert.NotNull(tsCode);
        Assert.Contains("export const User", tsCode);
        Assert.Contains("import {", tsCode);
    }

    [Fact]
    public void CanGenerateCSharp()
    {
        var csCode = FlowerBIEngine.GenerateCSharp(TestYaml, "MyApp.Schema");
        
        Assert.NotNull(csCode);
        Assert.Contains("namespace MyApp.Schema", csCode);
        Assert.Contains("public static class User", csCode);
    }

    [Fact]
    public void CanGetVersion()
    {
        var version = FlowerBIEngine.GetVersion();
        
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }

    [Fact]
    public void CanGetSupportedDatabaseTypes()
    {
        var dbTypes = FlowerBIEngine.GetSupportedDatabaseTypes();
        
        Assert.NotNull(dbTypes);
        Assert.NotEmpty(dbTypes);
        Assert.Contains("sqlite", dbTypes);
        Assert.Contains("sqlserver", dbTypes);
    }

    [Fact]
    public void ThrowsOnInvalidYaml()
    {
        Assert.Throws<FlowerBIException>(() => FlowerBIEngine.SchemaFromYaml("invalid yaml"));
    }

    [Fact]
    public void ThrowsOnMissingBundle()
    {
        Assert.Throws<FileNotFoundException>(() => 
            FlowerBIEngine.SchemaFromYaml(TestYaml, "/nonexistent/bundle.js"));
    }
}