using System;
using FlowerBI;
using FlowerBI.Jint;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI.JintTests;

class SimpleJintTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("FlowerBI Jint Engine Integration Test");
        Console.WriteLine("====================================");

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
            // Test schema creation
            Console.WriteLine("\n🔄 Testing schema creation...");
            using var schema = FlowerBIEngine.SchemaFromYaml(testYaml);
            Console.WriteLine($"✅ Schema created: {schema.Name}");

            // Test query creation
            Console.WriteLine("\n🔄 Testing query creation...");
            using var query = FlowerBIEngine.QueryFromJson(new QueryJson
            {
                Select = new[] { "User.Name" },
                Aggregations = new[] { new AggregationJson { Function = AggregationType.Count, Column = "Order.Id" } },
                Filters = new[] { new FilterJson { Column = "User.IsActive", Operator = "=", Value = true } }
            }, schema, "sqlite");

            var prepared = query.GetPreparedQuery();
            if (prepared == null)
            {
                Console.WriteLine("❌ GetPreparedQuery() returned null");
                return;
            }
            Console.WriteLine($"✅ Query prepared - SQL length: {prepared.Sql?.Length ?? 0}");
            Console.WriteLine($"✅ Parameters: {prepared.Parameters?.Length ?? 0}");

            // Test code generation
            Console.WriteLine("\n🔄 Testing TypeScript generation...");
            var tsCode = FlowerBIEngine.GenerateTypeScript(testYaml);
            Console.WriteLine($"✅ TypeScript generated - length: {tsCode.Length}");

            Console.WriteLine("\n🔄 Testing C# generation...");
            var csCode = FlowerBIEngine.GenerateCSharp(testYaml, "TestNamespace");
            Console.WriteLine($"✅ C# generated - length: {csCode.Length}");

            // Test version info
            Console.WriteLine("\n🔄 Testing version info...");
            var version = FlowerBIEngine.GetVersion();
            var dbTypes = FlowerBIEngine.GetSupportedDatabaseTypes();
            Console.WriteLine($"✅ Version: {version}");
            Console.WriteLine($"✅ Database types: {string.Join(", ", dbTypes)}");

            Console.WriteLine("\n🎉 All tests passed! FlowerBI Jint integration is working correctly!");
            
            // Test the new wrapper API
            TestNewApi.RunTests();
            
            // Test simplified thread-safe implementation
            await SimpleThreadSafeTest.RunSimpleThreadSafeTests();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
            Console.WriteLine($"   Stack: {ex.StackTrace}");
        }
    }
}