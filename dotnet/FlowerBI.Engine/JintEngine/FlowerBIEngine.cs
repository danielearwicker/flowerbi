using System;
using System.IO;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI.Jint;

/// <summary>
/// Main entry point for FlowerBI Engine with Jint integration
/// Provides both new Jint-based API and backward compatibility
/// </summary>
public static class FlowerBIEngine
{
    /// <summary>
    /// Create a schema from YAML text using Jint JavaScript bundle
    /// </summary>
    public static JintSchema SchemaFromYaml(string yamlText, string bundlePath = null)
    {
        return JintSchema.FromYaml(yamlText, bundlePath);
    }

    /// <summary>
    /// Create a query from JSON and schema using Jint
    /// </summary>
    public static JintQuery QueryFromJson(QueryJson queryJson, JintSchema schema, string databaseType = "sqlite")
    {
        return new JintQuery(queryJson, schema, databaseType);
    }

    /// <summary>
    /// Generate TypeScript code from YAML schema
    /// </summary>
    public static string GenerateTypeScript(string yamlText, string bundlePath = null)
    {
        using var engine = new FlowerBI.JintEngine.FlowerBIJintEngine(bundlePath);
        return engine.GenerateTypeScript(yamlText);
    }

    /// <summary>
    /// Generate C# code from YAML schema
    /// </summary>
    public static string GenerateCSharp(string yamlText, string namespaceName, string bundlePath = null)
    {
        using var engine = new FlowerBI.JintEngine.FlowerBIJintEngine(bundlePath);
        return engine.GenerateCSharp(yamlText, namespaceName);
    }

    /// <summary>
    /// Write TypeScript code to file (maintains compatibility with existing tools)
    /// </summary>
    public static void WriteTypeScriptToFile(string yamlText, string outputPath, string bundlePath = null)
    {
        var code = GenerateTypeScript(yamlText, bundlePath);
        File.WriteAllText(outputPath, code);
    }

    /// <summary>
    /// Write C# code to file (maintains compatibility with existing tools)
    /// </summary>
    public static void WriteCSharpToFile(string yamlText, string outputPath, string namespaceName, string bundlePath = null)
    {
        var code = GenerateCSharp(yamlText, namespaceName, bundlePath);
        File.WriteAllText(outputPath, code);
    }

    /// <summary>
    /// Get FlowerBI version from JavaScript bundle
    /// </summary>
    public static string GetVersion(string bundlePath = null)
    {
        using var engine = new FlowerBI.JintEngine.FlowerBIJintEngine(bundlePath);
        return engine.GetVersion();
    }

    /// <summary>
    /// Get supported database types
    /// </summary>
    public static string[] GetSupportedDatabaseTypes(string bundlePath = null)
    {
        using var engine = new FlowerBI.JintEngine.FlowerBIJintEngine(bundlePath);
        return engine.GetSupportedDatabaseTypes();
    }

}

/// <summary>
/// Legacy compatibility - provides static methods that match original API
/// </summary>
public static class SchemaExtensions
{
    /// <summary>
    /// Backward compatibility: Create schema from YAML (static method)
    /// </summary>
    public static JintSchema ParseYaml(string yamlText)
    {
        return FlowerBIEngine.SchemaFromYaml(yamlText);
    }
}