using System;
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
                Select = new List<string>
                {
                    "Invoice.VendorId",
                    "Invoice.DepartmentId",
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

            querySql.Should().Contain("[tbl00].[Id] IN @filter0");
            filterParams.ToString().Should().Be("@filter0 = 2, 4, 6, 8");

            var db = new Mock<IDbConnection>();
            
            var cmd = new Mock<IDbCommand>();            
            db.Setup(x => x.CreateCommand()).Returns(cmd.Object);
    
            var dpc = new Mock<IDataParameterCollection>();            
            cmd.SetupGet(x => x.Parameters).Returns(dpc.Object);
            
            cmd.SetupGet(x => x.CommandText).Returns(string.Empty);

            var parameters = new List<Mock<IDbDataParameter>>();
            cmd.Setup(x => x.CreateParameter()).Returns(() =>
            {
                var parameter = new Mock<IDbDataParameter>();
                parameters.Add(parameter);
                return parameter.Object;
            });

            var reader = new Mock<IDataReader>();
            cmd.Setup(x => x.ExecuteReader(It.IsAny<CommandBehavior>()))
               .Returns(reader.Object);

            reader.SetupGet(x => x.FieldCount).Returns(3);

            reader.Setup(x => x.GetName(0)).Returns("Value0");
            reader.Setup(x => x.GetName(1)).Returns("Select0");
            reader.Setup(x => x.GetName(2)).Returns("Select1");

            reader.SetupSequence(x => x.Read()).Returns(true).Returns(false);
            reader.Setup(x => x.GetValue(0)).Returns("chips");
            reader.Setup(x => x.GetValue(1)).Returns(DBNull.Value);
            reader.Setup(x => x.GetValue(2)).Returns(13);

            var log = new List<string>();            
            var result = query.Run(new SqlServerFormatter(), db.Object, log.Add);

            var record = result.Records.Single();
            record.Aggregated.Single().Should().Be("[chips]");
            record.Selected.First().Should().Be(null);
            record.Selected.Last().Should().Be(26);
        }
    }
}