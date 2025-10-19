using System;
using System.IO;
using System.Text.Json;
using Jint.Native;
using FlowerBI.Engine.JsonModels;
using FlowerBI.JintEngine;

namespace FlowerBI.Jint;

/// <summary>
/// Jint-based implementation of FlowerBI Schema
/// Maintains compatibility with existing API while using JavaScript bundle
/// </summary>
public class JintSchema : IDisposable
{
    private readonly FlowerBIJintEngine _engine;
    private readonly string _yamlText;
    private readonly string _schemaJson;
    private bool _disposed;

    public string Name { get; }
    public string DbName { get; }

    private JintSchema(FlowerBIJintEngine engine, string yamlText, string schemaJson, string name, string dbName)
    {
        _engine = engine;
        _yamlText = yamlText;
        _schemaJson = schemaJson;
        Name = name;
        DbName = dbName;
    }

    /// <summary>
    /// Create a schema from YAML text using the JavaScript bundle
    /// </summary>
    public static JintSchema FromYaml(string yamlText, string bundlePath = null)
    {
        var engine = new FlowerBIJintEngine(bundlePath);
        try
        {
            var schemaJson = engine.ParseSchema(yamlText);
            
            // Parse schema JSON to extract name and dbName
            using var doc = JsonDocument.Parse(schemaJson);
            var root = doc.RootElement;
            
            // Handle both camelCase (new) and PascalCase (legacy) property names
            var name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() :
                       root.TryGetProperty("Name", out var nameElementOld) ? nameElementOld.GetString() : "Unknown";
            var dbName = root.TryGetProperty("nameInDb", out var dbNameElement) ? dbNameElement.GetString() :
                         root.TryGetProperty("NameInDb", out var dbNameElementOld) ? dbNameElementOld.GetString() : name;

            return new JintSchema(engine, yamlText, schemaJson, name, dbName);
        }
        catch
        {
            engine.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Create a query engine for the specified database type
    /// </summary>
    public JintQueryEngine CreateQueryEngine(string databaseType)
    {
        var queryEngine = _engine.CreateQueryEngine(_yamlText, databaseType);
        return new JintQueryEngine(_engine, queryEngine);
    }

    /// <summary>
    /// Generate TypeScript code from this schema
    /// </summary>
    public string GenerateTypeScript()
    {
        return _engine.GenerateTypeScript(_yamlText);
    }

    /// <summary>
    /// Generate C# code from this schema
    /// </summary>
    public string GenerateCSharp(string namespaceName)
    {
        return _engine.GenerateCSharp(_yamlText, namespaceName);
    }

    /// <summary>
    /// Get the parsed schema as JSON
    /// </summary>
    public string GetSchemaJson() => _schemaJson;


    public void Dispose()
    {
        if (!_disposed)
        {
            _engine?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Jint-based query engine for executing FlowerBI queries
/// </summary>
public class JintQueryEngine
{
    private readonly FlowerBIJintEngine _engine;
    private readonly JsValue _queryEngineRef;

    internal JintQueryEngine(FlowerBIJintEngine engine, JsValue queryEngineRef)
    {
        _engine = engine;
        _queryEngineRef = queryEngineRef;
    }

    /// <summary>
    /// Prepare a query for execution
    /// </summary>
    public PreparedQuery PrepareQuery(QueryJson query)
    {
        return _engine.PrepareQuery(_queryEngineRef, query);
    }

    /// <summary>
    /// Map database results to FlowerBI format
    /// </summary>
    public QueryResultJson MapResults(QueryJson query, DatabaseResult databaseResult)
    {
        return _engine.MapResults(_queryEngineRef, query, databaseResult);
    }
}