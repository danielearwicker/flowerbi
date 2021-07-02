using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowerBI.Engine.JsonModels;
using FluentAssertions;
using Moq;
using Xunit;

namespace FlowerBI.Engine.Tests
{
    public class IntegrationTests
    {
        public static readonly Schema Schema = QueryGenerationTests.Schema;

        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        [Fact]
        public void SqlAndDapperWithListFilter()
        {
            var queryJson = new QueryJson
            {
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Vendor.VendorName",
                    }
                },
                Filters = new List<FilterJson>
                {
                    JsonSerializer.Deserialize<FilterJson>($@"{{
                        ""column"": ""Invoice.Id"",
                        ""operator"": ""IN"",
                        ""value"": [2,4,6,8]
                    }}", JsonOptions)
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DapperFilterParameters();
            var querySql = query.ToSql(new SqlServerFormatter(), filterParams, Enumerable.Empty<Filter>());

            querySql.Should().Contain("[tbl1].[Id] IN (@filter0_0, @filter0_1, @filter0_2, @filter0_3)");
            filterParams.ToString().Should().Be("filter0_0 = 2, filter0_1 = 4, filter0_2 = 6, filter0_3 = 8");
        }
    }
}