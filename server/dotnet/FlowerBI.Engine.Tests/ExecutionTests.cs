#define START_DOCKER_SQL
#define STOP_DOCKER_SQL

using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using Dapper;
using FluentAssertions;
using FlowerBI.Engine.JsonModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FlowerBI.Engine.Tests
{
    public class ExecutionTests(DatabaseFixture Fixture) : IClassFixture<DatabaseFixture>
    {        
        public static readonly Schema Schema = QueryGenerationTests.Schema;

        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private QueryResultJson ExecuteQuery(QueryJson json, params Filter[] outerFilters)
            => new Query(json, Schema).Run(
                new SqlServerFormatter(),
                Fixture.Db,
                str => Debug.WriteLine(str),
                outerFilters);
        
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void MinimalSelectOneColumn(bool allowDuplicates, bool fullJoins)
        {
            var results = ExecuteQuery(new()
            {
                Aggregations =
                [
                    new() { Column = "Vendor.VendorName" }
                ],
                Skip = 0,
                Take = 1,
                AllowDuplicates = allowDuplicates,
                FullJoins = fullJoins,
            });

            results.Records.Single().Aggregated.Single().Should().Be(14);
            results.Totals.Should().BeNull();
        }

        [Fact]
        public void FilterByPrimaryKeyOfOtherTable()
        {
            var queryJson = new QueryJson
            {
                Aggregations =
                [
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    }
                ],
                Filters =
                [
                    new FilterJson
                    {
                        Column = "Vendor.Id",
                        Operator = "=",
                        Value = JsonDocument.Parse("2").RootElement
                    }
                ]
            };

            var results = ExecuteQuery(queryJson);
            results.Records.Single().Aggregated.Single().Should().Be(164.36);
            results.Totals.Should().BeNull();
        }

        [Theory]
        [InlineData(false, "suspicious \r\ncomment */ drop tables;", "/* suspicious \r\ncomment drop tables */")]
        [InlineData(false, "", "")]
        [InlineData(false, null, "")]
        [InlineData(true, "suspicious \r\ncomment */ drop tables;", "/* suspicious \r\ncomment drop tables */")]
        [InlineData(true, "", "")]
        [InlineData(true, null, "")]
        public void SingleAggregation(bool allowDuplicates, string comment, string expectedComment)
        {
            var queryJson = new QueryJson
            {
                Select = ["Vendor.VendorName"],
                Aggregations =
                [
                    new()
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    }
                ],
                Skip = 2,
                Take = 10,
                AllowDuplicates = allowDuplicates,
                Comment = comment,
            };

            var results = ExecuteQuery(queryJson);

            results.Records.Select(x => (x.Selected.Single(), x.Aggregated.Single()))
                .Should().BeEquivalentTo(new[]
                {
                    ("[Steve Makes Sandwiches]", 176.24m),
                    ("[Manchesterford Supplies Inc]", 164.36m),
                    ("[Disgusting Ltd]", 156.24m),
                    ("[Statues While You Wait]", 156.24m),
                    ("[Tiles Tiles Tiles]", 106.24m),
                    ("[Uranium 4 Less]", 88.12m),
                    ("[Awnings-R-Us]", 88.12m),
                    ("[Pleasant Plc]", 88.12m),
                    ("[Mats and More]", 76.24m),
                    ("[Party Hats 4 U]", 58.12m),                    
                });

            results.Totals.Should().BeNull();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleAggregationOrderBySelect(bool fullJoins)
        {
            var queryJson = new QueryJson
            {
                Select = ["Vendor.VendorName"],
                Aggregations =
                [
                    new()
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    }
                ],
                Skip = 2,
                Take = 10,
                OrderBy = [ new() { Column = "Vendor.VendorName" } ],
                FullJoins = fullJoins
            };

            var results = ExecuteQuery(queryJson);

            results.Records.Select(x => (x.Selected.Single(), x.Aggregated.Single()))
                .Should().BeEquivalentTo(new[]
                {
                    ("[Handbags-a-Plenty]", 252.48m),
                    ("[Manchesterford Supplies Inc]", 164.36m),
                    ("[Mats and More]", 76.24m),
                    ("[Party Hats 4 U]", 58.12m),
                    ("[Pleasant Plc]", 88.12m),
                    ("[Stationary Stationery]", 28.12m),
                    ("[Statues While You Wait]", 156.24m),
                    ("[Steve Makes Sandwiches]", 176.24m),
                    ("[Tiles Tiles Tiles]", 106.24m),
                    ("[United Cheese]", 406.84m),                    
                });

            results.Totals.Should().BeNull();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleAggregationTotals(bool fullJoins)
        {
            var queryJson = new QueryJson
            {
                Select = ["Vendor.VendorName"],
                Aggregations =
                [
                    new()
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    }
                ],
                Skip = 2,
                Take = 10,
                FullJoins = fullJoins,
                Totals = true,
            };

            var results = ExecuteQuery(queryJson);

            results.Records.Select(x => (x.Selected.Single(), x.Aggregated.Single()))
                .Should().BeEquivalentTo(new[]
                {
                    ("[Steve Makes Sandwiches]", 176.24m),
                    ("[Manchesterford Supplies Inc]", 164.36m),
                    ("[Disgusting Ltd]", 156.24m),
                    ("[Statues While You Wait]", 156.24m),
                    ("[Tiles Tiles Tiles]", 106.24m),
                    ("[Uranium 4 Less]", 88.12m),
                    ("[Awnings-R-Us]", 88.12m),
                    ("[Pleasant Plc]", 88.12m),
                    ("[Mats and More]", 76.24m),
                    ("[Party Hats 4 U]", 58.12m),                    
                });

            results.Totals.Aggregated.Single().Should().Be(1845.48m);
        }

        [Theory]
        [InlineData(AggregationType.Min)]
        [InlineData(AggregationType.Max)]
        public void AggregationFunctions(AggregationType type)
        {
            var queryJson = new QueryJson
            {
                Select = ["Vendor.VendorName"],
                Aggregations =
                [
                    new()
                    {
                        Column = "Invoice.Amount",
                        Function = type
                    }
                ],
                OrderBy = [ new() { Column = "Vendor.VendorName" } ],
                Skip = 0,
                Take = 100,
            };

            var results = ExecuteQuery(queryJson);

            var expected = new[]
            {
                ("[Awnings-R-Us]", 88.12, 88.12),
                ("[Disgusting Ltd]", 68.12, 88.12),
                ("[Handbags-a-Plenty]", 28.12, 98.12),
                ("[Manchesterford Supplies Inc]", 18.12, 88.12),
                ("[Mats and More]", 18.12, 58.12),
                ("[Party Hats 4 U]", 58.12, 58.12),
                ("[Pleasant Plc]", 88.12, 88.12),
                ("[Stationary Stationery]", 28.12, 28.12),
                ("[Statues While You Wait]", 78.12, 78.12),
                ("[Steve Makes Sandwiches]", 88.12, 88.12),
                ("[Tiles Tiles Tiles]", 38.12, 68.12),
                ("[United Cheese]", 18.12, 98.12),
                ("[Uranium 4 Less]", 88.12, 88.12),
            };

            var expectedForType = expected.Select(x => (x.Item1, 
                type == AggregationType.Min ? x.Item2 : x.Item3));

            results.Records.Select(x => (x.Selected.Single(), x.Aggregated.Single()))
                .Should().BeEquivalentTo(expectedForType);                

            results.Totals.Should().BeNull();
       }
    }
}