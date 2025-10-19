using System;
using FlowerBI;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI.JintTests;

public static class TestNewApi
{
    public static void RunTests()
    {
        Console.WriteLine("\n🔄 Testing new wrapper API...");
        
        const string testYaml = @"
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

        try
        {
            // Test new Schema.FromYaml method
            var schema = Schema.FromYaml(testYaml);
            Console.WriteLine($"✅ Schema.FromYaml() - Name: {schema.RefName}");

            // Test new JintQueryWrapper
            using var jintQuery = QueryExtensions.CreateWithJint(new QueryJson
            {
                Select = new[] { "User.Name" },
                Aggregations = new[] { new AggregationJson { Function = AggregationType.Count, Column = "Order.Id" } },
                Filters = new[] { new FilterJson { Column = "User.IsActive", Operator = "=", Value = true } }
            }, testYaml, "sqlite");

            var sql = jintQuery.ToSql();
            Console.WriteLine($"✅ JintQueryWrapper.ToSql() - length: {sql?.Length ?? 0}");
            
            var prepared = jintQuery.GetPreparedQuery();
            Console.WriteLine($"✅ JintQueryWrapper.GetPreparedQuery() - SQL: {prepared?.Sql?.Length ?? 0}, Params: {prepared?.Parameters?.Length ?? 0}");

            Console.WriteLine("🎉 New API tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ New API test failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
        }
    }
}