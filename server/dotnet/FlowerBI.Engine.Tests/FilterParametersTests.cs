namespace FlowerBI.Engine.Tests;

using FluentAssertions;
using Xunit;

public class FilterParametersTests
{
    private static Filter MakeFilter(object val)
        => new(new LabelledColumn("x", new Column<string>("c1")), "=", val, null);

    [Fact]
    public void DapperFilterParameters_String_GeneratesActualParam()
    {   
        var p = new DapperFilterParameters();
        var result = p[MakeFilter("hi")];
        result.Should().Be("@filter0");
        p.DapperParams.ParameterNames.Should().BeEquivalentTo(["filter0"]);
    }

    [Fact]
    public void DapperFilterParameters_ListWithString_GeneratesActualParam()
    {   
        var p = new DapperFilterParameters();
        var result = p[MakeFilter("hi")];
        result.Should().Be("@filter0");
        p.DapperParams.ParameterNames.Should().BeEquivalentTo(["filter0"]);
    }

    [Theory]
    [InlineData(42, "42")]
    [InlineData((short)42, "42")]
    [InlineData((long)42, "42")]
    [InlineData(3.14, "3.14")]
    [InlineData((float)3.14, "3.14")]
    [InlineData(true, "1")]
    [InlineData(false, "0")]
    public void DapperFilterParameters_SimpleNumber_GeneratesLiteral(object val, string expected)
    {   
        var p = new DapperFilterParameters();
        var result = p[MakeFilter(val)];
        result.Should().Be(expected);
        p.DapperParams.ParameterNames.Should().BeEmpty();
    }

    [Fact]
    public void DapperFilterParameters_Decimal_GeneratesLiteral()
    {   
        var p = new DapperFilterParameters();
        var result = p[MakeFilter(3.14m)];
        result.Should().Be("3.14");
        p.DapperParams.ParameterNames.Should().BeEmpty();
    }

    [Theory]
    [InlineData(42, "(42, 42)")]
    [InlineData((short)42, "(42, 42)")]
    [InlineData((long)42, "(42, 42)")]
    [InlineData(3.14, "(3.14, 3.14)")]
    [InlineData((float)3.14, "(3.14, 3.14)")]
    [InlineData(true, "(1, 1)")]
    [InlineData(false, "(0, 0)")]
    public void DapperFilterParameters_SimpleNumberList_GeneratesLiteral(object val, string expected)
    {   
        var p = new DapperFilterParameters();
        var result = p[MakeFilter(new[] {val, val})];
        result.Should().Be(expected);
        p.DapperParams.ParameterNames.Should().BeEmpty();
    }

    [Fact]
    public void DapperFilterParameters_DecimalList_GeneratesLiteral()
    {   
        var p = new DapperFilterParameters();
        var result = p[MakeFilter(new object[] {3.14, 8, (short)3})];
        result.Should().Be("(3.14, 8, 3)");
        p.DapperParams.ParameterNames.Should().BeEmpty();
    }
}
