using System;
using System.IO;
using FlowerBI.Jint;

namespace FlowerBI.Conversion;

/// <summary>
/// Code generation that uses the existing C# implementation as fallback when Jint fails
/// </summary>
public static class JintTypeScript
{
    public static void FromYaml(string yamlFile, string tsFile, TextWriter console)
    {
        try
        {
            var yamlText = File.ReadAllText(yamlFile);
            
            // First try the Jint-based approach
            try
            {
                var tsCode = FlowerBIEngine.GenerateTypeScript(yamlText);
                if (!string.IsNullOrEmpty(tsCode))
                {
                    File.WriteAllText(tsFile, tsCode);
                    console.WriteLine($"TypeScript generated: {tsFile}");
                    return;
                }
            }
            catch (Exception jintEx)
            {
                console.WriteLine($"Jint TypeScript generation failed: {jintEx.Message}, falling back to existing implementation");
            }
            
            // Fallback to existing TypeScript implementation
            TypeScript.FromYaml(yamlFile, tsFile, console);
        }
        catch (Exception ex)
        {
            console.WriteLine($"Error generating TypeScript: {ex.Message}");
            throw;
        }
    }

    public static void FromSchema(string yamlText, TextWriter outputWriter, TextWriter console)
    {
        try
        {
            // First try the Jint-based approach
            try
            {
                var tsCode = FlowerBIEngine.GenerateTypeScript(yamlText);
                if (!string.IsNullOrEmpty(tsCode))
                {
                    outputWriter.Write(tsCode);
                    console.WriteLine("TypeScript generated successfully");
                    return;
                }
            }
            catch (Exception jintEx)
            {
                console.WriteLine($"Jint TypeScript generation failed: {jintEx.Message}, falling back to existing implementation");
            }
            
            // Fallback to existing TypeScript implementation
            TypeScript.FromSchema(yamlText, outputWriter, console);
        }
        catch (Exception ex)
        {
            console.WriteLine($"Error generating TypeScript: {ex.Message}");
            throw;
        }
    }
}

public static class JintCSharp
{
    public static void FromYaml(string yamlFile, string csFile, string csNamespace, TextWriter console)
    {
        try
        {
            var yamlText = File.ReadAllText(yamlFile);
            
            // First try the Jint-based approach
            try
            {
                var csCode = FlowerBIEngine.GenerateCSharp(yamlText, csNamespace);
                if (!string.IsNullOrEmpty(csCode))
                {
                    File.WriteAllText(csFile, csCode);
                    console.WriteLine($"C# generated: {csFile}");
                    return;
                }
            }
            catch (Exception jintEx)
            {
                console.WriteLine($"Jint C# generation failed: {jintEx.Message}, falling back to existing implementation");
            }
            
            // Fallback to existing C# implementation
            CSharp.FromYaml(yamlFile, csFile, csNamespace, console);
        }
        catch (Exception ex)
        {
            console.WriteLine($"Error generating C#: {ex.Message}");
            throw;
        }
    }

    public static void FromSchema(string yamlText, string csNamespace, TextWriter outputWriter, TextWriter console)
    {
        try
        {
            // First try the Jint-based approach
            try
            {
                var csCode = FlowerBIEngine.GenerateCSharp(yamlText, csNamespace);
                if (!string.IsNullOrEmpty(csCode))
                {
                    outputWriter.Write(csCode);
                    console.WriteLine("C# generated successfully");
                    return;
                }
            }
            catch (Exception jintEx)
            {
                console.WriteLine($"Jint C# generation failed: {jintEx.Message}, falling back to existing implementation");
            }
            
            // Fallback to existing C# implementation
            CSharp.FromYamlText(yamlText, csNamespace, outputWriter, console);
        }
        catch (Exception ex)
        {
            console.WriteLine($"Error generating C#: {ex.Message}");
            throw;
        }
    }
}