namespace FlowerBI.Bootsharp;

using System.Text.Json;
using System.Text.Json.Serialization;
using global::Bootsharp;
using FlowerBI.Engine.JsonModels;
using FlowerBI.Yaml;

public interface IFlowerBISchema
{
    public string? GenerateQuery(string query);
}

public class FlowerBISchema(string yaml, ISqlFormatter formatter) : IFlowerBISchema
{
    private readonly Schema _schema = new(ResolvedSchema.Resolve(yaml));

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string? GenerateQuery(string queryJson)
    {
        var queryDefinition = JsonSerializer.Deserialize<QueryJson>(queryJson, _jsonOptions);

        var query = new Query(queryDefinition, _schema);

        var filterParams = new DictionaryFilterParameters(
            formatter.GetParamPrefix()
        ).WithEmbedded();

        var sql = query.ToSql(formatter, filterParams, []);

        return JsonSerializer.Serialize(new { sql, parameters = filterParams.Inner.Values });
    }
}

public static partial class Program
{
    public static void Main() { }

    [JSInvokable]
    public static IFlowerBISchema? Schema(string yaml, ISqlFormatter formatter) =>
        new FlowerBISchema(yaml, formatter);
}
