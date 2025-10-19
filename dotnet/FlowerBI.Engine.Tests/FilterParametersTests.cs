namespace FlowerBI.Engine.Tests;

using System;
using FlowerBI.Engine.JsonModels;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

public class FilterParametersTests
{
    private static Filter MakeFilter(object val, bool newtonsoft)
    {
        var valAsJson = JsonConvert.SerializeObject(val);

        var filterJson = $$"""
            {
                "Column": "Vendor.VendorName",
                "Operator": "=",
                "Value": {{valAsJson}}
            }
            """;

        var parsedFilter = newtonsoft
            ? JsonConvert.DeserializeObject<FilterJson>(filterJson)
            : System.Text.Json.JsonSerializer.Deserialize<FilterJson>(filterJson);

        return new Filter(parsedFilter, ExecutionTests.Schema);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DapperFilterParameters_String_GeneratesActualParam(bool newtonSoft)
    {
        var p = new DapperFilterParameters().WithEmbedded();
        var result = p[MakeFilter("hi", newtonSoft)];
        result.Should().Be("@filter0");
        p.Inner.DapperParams.ParameterNames.Should().BeEquivalentTo(["filter0"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DapperFilterParameters_ListWithString_GeneratesActualParam(bool newtonSoft)
    {
        var p = new DapperFilterParameters().WithEmbedded();
        var result = p[MakeFilter("hi", newtonSoft)];
        result.Should().Be("@filter0");
        p.Inner.DapperParams.ParameterNames.Should().BeEquivalentTo(["filter0"]);
    }

    [Theory]
    [InlineData(42, "42", false)]
    [InlineData((short)42, "42", false)]
    [InlineData((long)42, "42", false)]
    [InlineData(3.14, "3.14", false)]
    [InlineData((float)3.14, "3.14", false)]
    [InlineData(true, "1", false)]
    [InlineData(false, "0", false)]
    [InlineData(42, "42", true)]
    [InlineData((short)42, "42", true)]
    [InlineData((long)42, "42", true)]
    [InlineData(3.14, "3.14", true)]
    [InlineData((float)3.14, "3.14", true)]
    [InlineData(true, "1", true)]
    [InlineData(false, "0", true)]
    public void DapperFilterParameters_SimpleNumber_GeneratesLiteral(
        object val,
        string expected,
        bool newtonSoft
    )
    {
        var p = new DapperFilterParameters().WithEmbedded();
        var result = p[MakeFilter(val, newtonSoft)];
        result.Should().Be(expected);
        p.Inner.DapperParams.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DapperFilterParameters_Decimal_GeneratesLiteral(bool newtonSoft)
    {
        var p = new DapperFilterParameters().WithEmbedded();
        var result = p[MakeFilter(3.14m, newtonSoft)];
        result.Should().Be("3.14");
        p.Inner.DapperParams.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData(42, "(42, 42)", false)]
    [InlineData((short)42, "(42, 42)", false)]
    [InlineData((long)42, "(42, 42)", false)]
    [InlineData(3.14, "(3.14, 3.14)", false)]
    [InlineData((float)3.14, "(3.14, 3.14)", false)]
    [InlineData(true, "(1, 1)", false)]
    [InlineData(false, "(0, 0)", false)]
    [InlineData(42, "(42, 42)", true)]
    [InlineData((short)42, "(42, 42)", true)]
    [InlineData((long)42, "(42, 42)", true)]
    [InlineData(3.14, "(3.14, 3.14)", true)]
    [InlineData((float)3.14, "(3.14, 3.14)", true)]
    [InlineData(true, "(1, 1)", true)]
    [InlineData(false, "(0, 0)", true)]
    public void DapperFilterParameters_SimpleNumberList_GeneratesLiteral(
        object val,
        string expected,
        bool newtonSoft
    )
    {
        var p = new DapperFilterParameters().WithEmbedded();
        var result = p[MakeFilter(new[] { val, val }, newtonSoft)];
        result.Should().Be(expected);
        p.Inner.DapperParams.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DapperFilterParameters_DecimalList_GeneratesLiteral(bool newtonSoft)
    {
        var p = new DapperFilterParameters().WithEmbedded();
        var result = p[MakeFilter(new object[] { 3.14, 8, (short)3 }, newtonSoft)];
        result.Should().Be("(3.14, 8, 3)");
        p.Inner.DapperParams.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DapperFilterParameters_EmptyList_CaughtEarly(bool newtonSoft)
    {
        var p = new DapperFilterParameters().WithEmbedded();

        Func<string> a = () => p[MakeFilter(Array.Empty<double>(), newtonSoft)];

        a.Should().Throw<FlowerBIException>().WithMessage("Filter JSON contains empty array");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DapperFilterParameters_ObjectWithEmptyArrayProperty_CaughtEarly(bool newtonSoft)
    {
        var p = new DapperFilterParameters().WithEmbedded();

        Func<string> a = () => p[MakeFilter(new { x = Array.Empty<double>() }, newtonSoft)];

        a.Should().Throw<FlowerBIException>().WithMessage("Unsupported filter value");
    }
}
