using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FlowerBI.JintEngine;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI.JintTests;

/// <summary>
/// Demonstrates the simplified thread-safe query engine usage patterns
/// </summary>
public static class SimpleThreadSafeTest
{
    // Example: Static field with thread-safe engine (recommended pattern)
    private static readonly ThreadSafeQueryEngine UserAnalyticsEngine = CreateUserAnalyticsEngine();

    // Example: Another static engine for a different schema
    private static readonly ThreadSafeQueryEngine OrdersEngine = CreateOrdersEngine();

    private static ThreadSafeQueryEngine CreateUserAnalyticsEngine()
    {
        const string userAnalyticsSchema = @"
schema: UserAnalytics
tables:
  User:
    id:
      Id: [int]
    columns:
      Name: [string]
      Email: [string]
      RegistrationDate: [datetime]
  Session:
    id:
      Id: [int]
    columns:
      UserId: [User]
      Duration: [int]
      PageViews: [int]
";
        return new ThreadSafeQueryEngine(userAnalyticsSchema, "sqlserver");
    }

    private static ThreadSafeQueryEngine CreateOrdersEngine()
    {
        const string ordersSchema = @"
schema: Orders
tables:
  Order:
    id:
      Id: [int]
    columns:
      CustomerId: [int]
      Total: [decimal]
      OrderDate: [datetime]
  OrderItem:
    id:
      Id: [int]
    columns:
      OrderId: [Order]
      ProductName: [string]
      Quantity: [int]
      Price: [decimal]
";
        return new ThreadSafeQueryEngine(ordersSchema, "postgresql");
    }

    public static async Task RunSimpleThreadSafeTests()
    {
        Console.WriteLine("\n🔄 Testing simplified thread-safe design...");

        try
        {
            // Test 1: Basic functionality
            await TestBasicFunctionality();

            // Test 2: Concurrent access to the same engine
            await TestConcurrentAccess();

            // Test 3: Multiple engines simultaneously
            await TestMultipleEngines();

            // Test 4: Static utility methods
            TestStaticUtilities();

            Console.WriteLine("🎉 All simplified thread-safe tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Simplified thread-safe test failed: {ex.Message}");
            throw;
        }
    }

    private static async Task TestBasicFunctionality()
    {
        Console.WriteLine("  ▶ Testing basic functionality...");

        var query = new QueryJson
        {
            Select = new[] { "User.Name", "User.Email" },
            Aggregations = new[]
            {
                new AggregationJson { Function = AggregationType.Count, Column = "Session.Id" },
                new AggregationJson { Function = AggregationType.Avg, Column = "Session.Duration" }
            },
            Filters = new[]
            {
                new FilterJson { Column = "User.RegistrationDate", Operator = ">=", Value = DateTime.Now.AddDays(-30) }
            }
        };

        // Test async version
        var preparedAsync = await UserAnalyticsEngine.PrepareQueryAsync(query);
        Console.WriteLine($"    ✅ Async PrepareQuery - SQL length: {preparedAsync.Sql?.Length ?? 0}");

        // Test sync version
        var preparedSync = UserAnalyticsEngine.PrepareQuery(query);
        Console.WriteLine($"    ✅ Sync PrepareQuery - SQL length: {preparedSync.Sql?.Length ?? 0}");

        // Test result mapping
        var mockResult = new DatabaseResult("array-of-arrays", new object[][]
        {
            new object[] { "John Doe", "john@example.com", 25, 180.5 },
            new object[] { "Jane Smith", "jane@example.com", 18, 220.3 }
        });

        var mapped = await UserAnalyticsEngine.MapResultsAsync(query, mockResult);
        Console.WriteLine($"    ✅ MapResults - {mapped.Records?.Count ?? 0} records mapped");
    }

    private static async Task TestConcurrentAccess()
    {
        Console.WriteLine("  ▶ Testing concurrent access to single engine...");

        const int threadCount = 8;
        const int operationsPerThread = 3;
        var query = new QueryJson
        {
            Select = new[] { "User.Name" },
            Aggregations = new[] { new AggregationJson { Function = AggregationType.Count, Column = "Session.Id" } }
        };

        var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(async () =>
        {
            for (int i = 0; i < operationsPerThread; i++)
            {
                // Multiple threads using the same static engine simultaneously
                var prepared = await UserAnalyticsEngine.PrepareQueryAsync(query);
                
                if (string.IsNullOrEmpty(prepared.Sql))
                {
                    throw new Exception($"Thread {threadId} operation {i} failed - no SQL generated");
                }

                // Simulate result mapping
                var mockResult = new DatabaseResult("array-of-arrays", new object[][]
                {
                    new object[] { $"User-{threadId}-{i}", 5 }
                });

                var mapped = await UserAnalyticsEngine.MapResultsAsync(query, mockResult);
                if (mapped.Records?.Count != 1)
                {
                    throw new Exception($"Thread {threadId} operation {i} failed - expected 1 record, got {mapped.Records?.Count}");
                }
            }
        }));

        await Task.WhenAll(tasks);
        Console.WriteLine($"    ✅ {threadCount} threads × {operationsPerThread} operations = {threadCount * operationsPerThread} total operations completed");
    }

    private static async Task TestMultipleEngines()
    {
        Console.WriteLine("  ▶ Testing multiple engines simultaneously...");

        // Create tasks that use different engines concurrently
        var userTask = Task.Run(async () =>
        {
            var query = new QueryJson
            {
                Select = new[] { "User.Name" },
                Aggregations = new[] { new AggregationJson { Function = AggregationType.Count, Column = "Session.Id" } }
            };
            return await UserAnalyticsEngine.PrepareQueryAsync(query);
        });

        var orderTask = Task.Run(async () =>
        {
            var query = new QueryJson
            {
                Select = new[] { "Order.OrderDate" },
                Aggregations = new[] { new AggregationJson { Function = AggregationType.Sum, Column = "Order.Total" } }
            };
            return await OrdersEngine.PrepareQueryAsync(query);
        });

        var results = await Task.WhenAll(userTask, orderTask);
        
        Console.WriteLine($"    ✅ User analytics query SQL length: {results[0].Sql?.Length ?? 0}");
        Console.WriteLine($"    ✅ Orders query SQL length: {results[1].Sql?.Length ?? 0}");
    }

    private static void TestStaticUtilities()
    {
        Console.WriteLine("  ▶ Testing static utility methods...");

        const string testSchema = @"
schema: TestSchema
tables:
  Product:
    id:
      Id: [int]
    columns:
      Name: [string]
      Price: [decimal]
";

        // Test static methods (these create temporary engines)
        var schemaJson = FlowerBIThreadSafe.ParseSchema(testSchema);
        Console.WriteLine($"    ✅ ParseSchema - JSON length: {schemaJson.Length}");

        var tsCode = FlowerBIThreadSafe.GenerateTypeScript(testSchema);
        Console.WriteLine($"    ✅ GenerateTypeScript - code length: {tsCode.Length}");

        var csCode = FlowerBIThreadSafe.GenerateCSharp(testSchema, "TestNamespace");
        Console.WriteLine($"    ✅ GenerateCSharp - code length: {csCode.Length}");

        var version = FlowerBIThreadSafe.GetVersion();
        Console.WriteLine($"    ✅ Version: {version}");

        var dbTypes = FlowerBIThreadSafe.GetSupportedDatabaseTypes();
        Console.WriteLine($"    ✅ Database types: {string.Join(", ", dbTypes)}");

        // Test factory method
        using var tempEngine = FlowerBIThreadSafe.CreateQueryEngine(testSchema, "sqlite");
        var tempQuery = new QueryJson
        {
            Select = new[] { "Product.Name" },
            Aggregations = new[] { new AggregationJson { Function = AggregationType.Avg, Column = "Product.Price" } }
        };
        var tempPrepared = tempEngine.PrepareQuery(tempQuery);
        Console.WriteLine($"    ✅ Factory-created engine - SQL length: {tempPrepared.Sql?.Length ?? 0}");
    }
}

/// <summary>
/// Example class showing recommended usage patterns in a real application
/// </summary>
public class RecommendedUsagePatterns
{
    // Pattern 1: Static engines for application-wide schemas (recommended)
    private static readonly ThreadSafeQueryEngine UserEngine = 
        new ThreadSafeQueryEngine(UserSchema, "sqlserver");
    
    private static readonly ThreadSafeQueryEngine ProductEngine = 
        new ThreadSafeQueryEngine(ProductSchema, "postgresql");
    
    // Pattern 2: Instance engines for component-specific schemas
    private readonly ThreadSafeQueryEngine _auditEngine;

    private const string UserSchema = @"
schema: Users
tables:
  User:
    id:
      Id: [int]
    columns:
      Username: [string]
      Email: [string]
      CreatedAt: [datetime]
";

    private const string ProductSchema = @"
schema: Products
tables:
  Product:
    id:
      Id: [int]
    columns:
      Name: [string]
      Price: [decimal]
      CategoryId: [int]
";

    private const string AuditSchema = @"
schema: Audit
tables:
  AuditLog:
    id:
      Id: [int]
    columns:
      Action: [string]
      UserId: [int]
      Timestamp: [datetime]
";

    public RecommendedUsagePatterns()
    {
        // Initialize instance-specific engine
        _auditEngine = new ThreadSafeQueryEngine(AuditSchema, "mysql");
    }

    // Methods that can be called from any thread safely
    public async Task<PreparedQuery> GetActiveUsersQueryAsync()
    {
        var query = new QueryJson
        {
            Select = new[] { "User.Username", "User.Email" },
            Filters = new[] { new FilterJson { Column = "User.CreatedAt", Operator = ">=", Value = DateTime.Now.AddDays(-30) } }
        };
        
        // Safe to call from multiple threads - UserEngine manages its own pool
        return await UserEngine.PrepareQueryAsync(query);
    }

    public async Task<PreparedQuery> GetTopProductsQueryAsync(int categoryId)
    {
        var query = new QueryJson
        {
            Select = new[] { "Product.Name", "Product.Price" },
            Filters = new[] { new FilterJson { Column = "Product.CategoryId", Operator = "=", Value = categoryId } },
            Aggregations = new[] { new AggregationJson { Function = AggregationType.Count, Column = "Product.Id" } }
        };
        
        // Different engine, different pool - can run concurrently with user queries
        return await ProductEngine.PrepareQueryAsync(query);
    }

    public async Task<PreparedQuery> GetAuditTrailQueryAsync(int userId)
    {
        var query = new QueryJson
        {
            Select = new[] { "AuditLog.Action", "AuditLog.Timestamp" },
            Filters = new[] { new FilterJson { Column = "AuditLog.UserId", Operator = "=", Value = userId } }
        };
        
        // Instance engine - each instance of this class has its own engine
        return await _auditEngine.PrepareQueryAsync(query);
    }

    public void Dispose()
    {
        // Only dispose instance engines - static engines live for application lifetime
        _auditEngine?.Dispose();
    }
}