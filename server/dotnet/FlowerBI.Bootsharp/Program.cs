namespace FlowerBI.Bootsharp;

using System;
using global::Bootsharp;

public interface IFlowerBISchema
{
    public string Query(string query);
}

public class FlowerBISchema(string yaml) : IFlowerBISchema
{
    public string Query(string query)
    {
        return $"Querying: {query} with {yaml}";
    }
}

public static partial class Program
{
    public static void Main()
    {
        OnMainInvoked($"Hello {GetFrontendName()}, .NET here!");
    }

    [JSEvent] // Used in JS as Program.onMainInvoked.subscribe(..)
    public static partial void OnMainInvoked(string message);

    [JSFunction] // Set in JS as Program.getFrontendName = () => ..
    public static partial string GetFrontendName();

    [JSInvokable] // Invoked from JS as Program.GetBackendName()
    public static string GetBackendName() => Environment.OSVersion.ToString();

    [JSInvokable]
    public static IFlowerBISchema Schema(string yaml)
    {
        return new FlowerBISchema(yaml);
    }
}
