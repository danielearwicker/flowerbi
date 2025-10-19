using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jint;
using Jint.Native;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI.JintEngine;

/// <summary>
/// FlowerBI engine implementation using Jint to execute JavaScript bundle
/// </summary>
public class FlowerBIJintEngine : IDisposable
{
    private readonly global::Jint.Engine _jintEngine;
    private readonly string _bundleCode;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public FlowerBIJintEngine(string bundlePath = null)
    {
        if (bundlePath != null)
        {
            if (!File.Exists(bundlePath))
            {
                throw new FileNotFoundException($"FlowerBI bundle not found at: {bundlePath}");
            }
            _bundleCode = File.ReadAllText(bundlePath);
        }
        else
        {
            _bundleCode = LoadEmbeddedBundle();
        }
        
        // Configure JSON serializer for camelCase naming
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        
        // Create Jint engine with optimized settings
        _jintEngine = new global::Jint.Engine(options =>
        {
            options.AllowClr()
                   .LimitRecursion(1000)
                   .TimeoutInterval(TimeSpan.FromSeconds(30));
        });

        // Add console support for debugging
        _jintEngine.SetValue("console", new
        {
            log = new Action<object>(obj => { /* Could log to ILogger if needed */ })
        });

        // Execute the bundle
        _jintEngine.Execute(_bundleCode);

        // Verify FlowerBI global object is available
        // Handle both FlowerBI and FlowerBIModule naming
        var flowerBI = _jintEngine.GetValue("FlowerBI");
        if (flowerBI.IsUndefined())
        {
            flowerBI = _jintEngine.GetValue("FlowerBIModule");
            if (flowerBI.IsUndefined())
            {
                throw new InvalidOperationException("FlowerBI global object not found in bundle");
            }
            // Create FlowerBI alias for consistency
            _jintEngine.SetValue("FlowerBI", flowerBI);
        }
    }

    /// <summary>
    /// Load the JavaScript bundle from embedded resource
    /// </summary>
    private static string LoadEmbeddedBundle()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "flowerbi-query-generation.js";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found in assembly {assembly.FullName}");
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Parse YAML schema and return JSON representation
    /// </summary>
    public string ParseSchema(string yamlText)
    {
        try
        {
            return _jintEngine.Evaluate($"FlowerBI.parseSchema(`{EscapeForJavaScript(yamlText)}`)").AsString();
        }
        catch (Exception ex)
        {
            var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
            throw new FlowerBIException($"Schema parsing failed: {error ?? ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create a query engine for the specified database type
    /// </summary>
    public JsValue CreateQueryEngine(string yamlText, string databaseType)
    {
        try
        {
            return _jintEngine.Evaluate($"FlowerBI.createQueryEngine(`{EscapeForJavaScript(yamlText)}`, '{databaseType}')");
        }
        catch (Exception ex)
        {
            var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
            throw new FlowerBIException($"Query engine creation failed: {error ?? ex.Message}", ex);
        }
    }

    /// <summary>
    /// Prepare a query and return SQL with parameters
    /// </summary>
    public PreparedQuery PrepareQuery(JsValue queryEngine, QueryJson query)
    {
        try
        {
            var queryJson = JsonSerializer.Serialize(query, _jsonOptions);
            _jintEngine.SetValue("tempQueryEngine", queryEngine);
            _jintEngine.SetValue("tempQueryJson", queryJson);
            var resultJson = _jintEngine.Evaluate("JSON.stringify(tempQueryEngine.prepareQuery(JSON.parse(tempQueryJson)))").AsString();
            
            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
            {
                var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
                throw new FlowerBIException($"Query preparation failed: {error}");
            }

            return JsonSerializer.Deserialize<PreparedQuery>(resultJson, _jsonOptions) ?? 
                   throw new FlowerBIException("Failed to deserialize prepared query");
        }
        catch (Exception ex) when (!(ex is FlowerBIException))
        {
            var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
            throw new FlowerBIException($"Query preparation failed: {error ?? ex.Message}", ex);
        }
    }

    /// <summary>
    /// Map database results to FlowerBI format
    /// </summary>
    public QueryResultJson MapResults(JsValue queryEngine, QueryJson query, DatabaseResult databaseResult)
    {
        try
        {
            var queryJson = JsonSerializer.Serialize(query, _jsonOptions);
            var dbResultJson = JsonSerializer.Serialize(databaseResult, _jsonOptions);
            _jintEngine.SetValue("tempQueryEngine", queryEngine);
            _jintEngine.SetValue("tempQueryJson", queryJson);
            _jintEngine.SetValue("tempDbResultJson", dbResultJson);
            
            var resultJson = _jintEngine.Evaluate("JSON.stringify(tempQueryEngine.mapResults(JSON.parse(tempQueryJson), JSON.parse(tempDbResultJson)))").AsString();
            
            if (string.IsNullOrEmpty(resultJson) || resultJson == "{}")
            {
                var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
                throw new FlowerBIException($"Result mapping failed: {error}");
            }

            return JsonSerializer.Deserialize<QueryResultJson>(resultJson, _jsonOptions) ?? 
                   throw new FlowerBIException("Failed to deserialize query result");
        }
        catch (Exception ex) when (!(ex is FlowerBIException))
        {
            var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
            throw new FlowerBIException($"Result mapping failed: {error ?? ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate TypeScript code from YAML schema
    /// </summary>
    public string GenerateTypeScript(string yamlText)
    {
        try
        {
            var result = _jintEngine.Evaluate($"FlowerBI.generateTypeScript(`{EscapeForJavaScript(yamlText)}`)").AsString();
            
            if (string.IsNullOrEmpty(result))
            {
                var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
                throw new FlowerBIException($"TypeScript generation failed: {error}");
            }

            return result;
        }
        catch (Exception ex) when (!(ex is FlowerBIException))
        {
            var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
            throw new FlowerBIException($"TypeScript generation failed: {error ?? ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate C# code from YAML schema
    /// </summary>
    public string GenerateCSharp(string yamlText, string namespaceName)
    {
        try
        {
            var result = _jintEngine.Evaluate($"FlowerBI.generateCSharp(`{EscapeForJavaScript(yamlText)}`, '{namespaceName}')").AsString();
            
            if (string.IsNullOrEmpty(result))
            {
                var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
                throw new FlowerBIException($"C# generation failed: {error}");
            }

            return result;
        }
        catch (Exception ex) when (!(ex is FlowerBIException))
        {
            var error = _jintEngine.Evaluate("FlowerBI.getLastError()").AsString();
            throw new FlowerBIException($"C# generation failed: {error ?? ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get supported database types
    /// </summary>
    public string[] GetSupportedDatabaseTypes()
    {
        try
        {
            var result = _jintEngine.Evaluate("JSON.stringify(FlowerBI.getSupportedDatabaseTypes())").AsString();
            return JsonSerializer.Deserialize<string[]>(result, _jsonOptions) ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            throw new FlowerBIException($"Failed to get supported database types: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get the FlowerBI bundle version
    /// </summary>
    public string GetVersion()
    {
        try
        {
            return _jintEngine.Evaluate("FlowerBI.getVersion()").AsString();
        }
        catch (Exception ex)
        {
            throw new FlowerBIException($"Failed to get version: {ex.Message}", ex);
        }
    }

    private static string EscapeForJavaScript(string input)
    {
        return input?.Replace("`", "\\`")
                   .Replace("\\", "\\\\")
                   .Replace("\r", "\\r")
                   .Replace("\n", "\\n") ?? string.Empty;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _jintEngine?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a prepared SQL query with parameters
/// </summary>
public record PreparedQuery(
    [property: JsonPropertyName("sql")] string Sql, 
    [property: JsonPropertyName("parameters")] object[] Parameters);

/// <summary>
/// Represents database result in various formats
/// </summary>
public record DatabaseResult(
    [property: JsonPropertyName("type")] string Type, 
    [property: JsonPropertyName("rows")] object[][] Rows);

// Note: FlowerBIException is defined in the main FlowerBI namespace