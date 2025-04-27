using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowerBI.Engine.JsonModels;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace FlowerBI.Engine.Tests;

public class JsonTests
{
    private string MakeFilterJson(string name, string value) =>
        $@"{{
                ""column"": ""{name}"",
                ""operator"": ""="",
                ""value"": {value}
            }}";

    private object ParseFilterCore(string name, string value)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        };

        var parsed = System.Text.Json.JsonSerializer.Deserialize<FilterJson>(
            MakeFilterJson(name, value),
            jsonOptions
        );
        var filter = new Filter(parsed, ExecutionTests.Schema);
        return filter.Value;
    }

    private object ParseFilterNewtonsoft(string name, string value)
    {
        var jsonOptions = new JsonSerializerSettings { };

        var parsed = JsonConvert.DeserializeObject<FilterJson>(
            MakeFilterJson(name, value),
            jsonOptions
        );
        var filter = new Filter(parsed, ExecutionTests.Schema);
        return filter.Value;
    }

    [Fact]
    public void BooleanFilterCore()
    {
        ParseFilterCore("Invoice.Paid", "true").Should().Be(true);
    }

    [Fact]
    public void BooleanFilterNewtonsoft()
    {
        ParseFilterNewtonsoft("Invoice.Paid", "true").Should().Be(true);
    }

    [Fact]
    public void NumberFilterCore()
    {
        ParseFilterCore("Invoice.Paid", "12").Should().Be(12);
    }

    [Fact]
    public void NumberFilterNewtonsoft()
    {
        ParseFilterNewtonsoft("Invoice.Paid", "12").Should().Be(12);
    }

    [Fact]
    public void StringFilterCore()
    {
        ParseFilterCore("Invoice.Paid", "\"baba\"").Should().Be("baba");
    }

    [Fact]
    public void StringFilterNewtonsoft()
    {
        ParseFilterNewtonsoft("Invoice.Paid", "\"baba\"").Should().Be("baba");
    }

    [Fact]
    public void DateFilterCore()
    {
        ParseFilterCore("Invoice.Paid", "\"2020-11-04T10:10:00.000Z\"")
            .Should()
            .Be(new DateTime(2020, 11, 04, 10, 10, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void DateFilterNewtonsoft()
    {
        ParseFilterNewtonsoft("Invoice.Paid", "\"2020-11-04T10:10:00.000Z\"")
            .Should()
            .Be(new DateTime(2020, 11, 04, 10, 10, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void InFilterCore()
    {
        ParseFilterCore("Invoice.Id", "[2,4,6,8]").Should().BeEquivalentTo(new[] { 2, 4, 6, 8 });
    }
}
