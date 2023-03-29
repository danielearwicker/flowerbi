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

        [Fact]
        public void SqlServerCalculations()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    },

                    new AggregationJson
                    {
                        Column = "Invoice.Id",
                        Function = AggregationType.Count
                    }
                },
                Calculations = new List<CalculationJson>
                {
                    new() { Aggregation = 1 },
                    new()
                    {
                        Operator = "+",
                        First = new() { Aggregation = 0 },
                        Second = new() { Value = 3 },
                    },
                    new()
                    {
                        Operator = "/",
                        First = new() { Aggregation = 0 },
                        Second = new() { Aggregation = 1 },
                    }                    
                },
                OrderBy = new List<OrderingJson> 
                {
                     new OrderingJson { Type = OrderingType.Calculation, Index = 1 } 
                },
                Skip = 5,
                Take = 10
            };

            var filterParams = new DictionaryFilterParameters();

            var query = new Query(queryJson, Schema);

            var sql = query.ToSql(new SqlServerFormatter(), filterParams, Enumerable.Empty<Filter>());

            QueryGenerationTests.AssertSameSql(sql, @"            
                with Aggregation0 as (        
                    select
                        [tbl00].[VendorName] Select0, 
                        Sum([tbl01].[FancyAmount]) Value0
                    from [Testing].[Supplier] tbl00
                    join [Testing].[Invoice] tbl01 on [tbl01].[VendorId] = [tbl00].[Id]
                    group by [tbl00].[VendorName]
                ) ,
                Aggregation1 as (        
                    select
                        [tbl00].[VendorName] Select0, 
                        Count([tbl01].[Id]) Value0
                    from [Testing].[Supplier] tbl00
                    join [Testing].[Invoice] tbl01 on [tbl01].[VendorId] = [tbl00].[Id]
                    group by [tbl00].[VendorName]
                )
                select
                    a0.Select0,
                    a0.Value0 Value0 ,
                    a1.Value0 Value1 ,
                    a1.Value0 Value2 ,
                    (a0.Value0 + 3) Value3 , 
                    iif(a1.Value0 = 0, 0, a0.Value0 / cast(a1.Value0 as float)) Value4
                from Aggregation0 a0
                left join Aggregation1 a1 on
                    a1.Select0 = a0.Select0
                order by 4 asc
                offset 5 rows
                fetch next 10 rows only");
        }
    }
}