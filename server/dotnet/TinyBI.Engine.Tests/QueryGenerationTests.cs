using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using TinyBI.Engine.JsonModels;
using FluentAssertions;
using Xunit;

namespace TinyBI.Engine.Tests
{
    public class QueryGenerationTests
    {
        private static readonly Schema Schema = new Schema(typeof(TestSchema));

        [Fact]
        public void RejectsBadColumnName()
        {
            var queryJson = new QueryJson
            {
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Vendor.FictionalName",
                    }
                }
            };

            Action a = () => new Query(queryJson, Schema);
            a.Should().Throw<InvalidOperationException>();            
        }

        [Fact]
        public void RejectsMalformedColumnName()
        {
            var queryJson = new QueryJson
            {
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Amount",
                    }
                }
            };

            Action a = () => new Query(queryJson, Schema);
            a.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MinimalSelectOneColumn()
        {
            var queryJson = new QueryJson
            {                
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Vendor.VendorName",
                    }
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                select top 10 main.[VendorName] Value0
                from [TestSchema].[Vendor] main
                order by main.[VendorName] desc
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void FilterByPrimaryKeyOfOtherTable()
        {
            var queryJson = new QueryJson
            {
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount"
                    }
                },
                Filters = new List<FilterJson>
                {
                    new FilterJson
                    {
                        Column = "Vendor.Id",
                        Operator = "=",
                        Value = JsonDocument.Parse("42").RootElement
                    }
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();

            // As filter is on PK of Vendor, can just use FK of Invoice, avoid join
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                select top 10 main.[Amount] Value0
                from [TestSchema].[Invoice] main
                where main.[VendorId] = @filter0
                order by main.[Amount] desc
            ");
            filterParams.Names.Should().HaveCount(1);
        }

        [Fact]
        public void SingleAggregation()
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
                    }
                }                
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                select top 10 join0.[VendorName] Select0, Sum ( main.[Amount] ) Value0
                from [TestSchema].[Invoice] main
                join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                group by join0.[VendorName]
                order by Sum ( main.[Amount] ) desc
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void SingleAggregationTotals()
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
                    }
                },
                Totals = true
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                select top 10 join0.[VendorName] Select0, Sum ( main.[Amount] ) Value0
                from [TestSchema].[Invoice] main
                join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                group by join0.[VendorName]
                order by Sum ( main.[Amount] ) desc ;
                select top 1 Sum ( main.[Amount] ) Value0
                from [TestSchema].[Invoice] main
                order by Sum ( main.[Amount] ) desc
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void DoubleAggregation()
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
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select join0.[VendorName] Select0, Sum ( main.[Amount] ) Value0
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                ) ,
                [Aggregation1] as (
                    select join0.[VendorName] Select0, Count ( main.[Id] ) Value0
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                )
                select top 10 a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Value0 desc
            ");

            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void DoubleAggregationTotals()
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
                Totals = true
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select join0.[VendorName] Select0, Sum ( main.[Amount] ) Value0
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                ) ,
                [Aggregation1] as (
                    select join0.[VendorName] Select0, Count ( main.[Id] ) Value0
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                )
                select top 10 a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Value0 desc ;

                with [Aggregation0] as (        
                    select Sum ( main.[Amount] ) Value0
                    from [TestSchema].[Invoice] main
                ) ,
                [Aggregation1] as (
                    select Count ( main.[Id] ) Value0
                    from [TestSchema].[Invoice] main
                )
                select top 1
                    a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                cross join Aggregation1 a1
                order by a0.Value0 desc
            ");

            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void DoubleAggregationDifferentFilters()
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
                        Function = AggregationType.Count,
                        Filters = new List<FilterJson>
                        {
                            new FilterJson
                            {
                                Column = "Invoice.Paid",
                                Operator = "=",
                                Value = JsonDocument.Parse("true").RootElement
                            }
                        }
                    }
                }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select join0.[VendorName] Select0, Sum ( main.[Amount] ) Value0
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                ) ,
                [Aggregation1] as (
                    select join0.[VendorName] Select0, Count ( main.[Id] ) Value0
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    where main.[Paid] = @filter0
                    group by join0.[VendorName]
                )
                select top 10 a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Value0 desc
            ");

            filterParams.Names.Should().HaveCount(1);
        }

        [Fact]
        public void ExtraFilters()
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
                    }
                }
            };

            var query = new Query(queryJson, Schema);

            var extra = new Filter(new FilterJson
                {
                    Column = "Invoice.Paid",
                    Operator = "=",
                    Value = JsonDocument.Parse("true").RootElement
                },
                Schema);

            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, new[] { extra }, 10), @"
                select top 10 join0.[VendorName] Select0, Sum ( main.[Amount] ) Value0
                from [TestSchema].[Invoice] main
                join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                where main.[Paid] = @filter0
                group by join0.[VendorName]
                order by Sum ( main.[Amount] ) desc
            ");
            filterParams.Names.Should().HaveCount(1);
        }

        [Fact]
        public void DoubleAggregationOrderBy()
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
                OrderBy = new List<OrderingJson> { new OrderingJson { Column = "Vendor.VendorName" } }
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new FilterParameters();
            AssertSameSql(query.ToSql(filterParams, Enumerable.Empty<Filter>(), 10), @"
                with [Aggregation0] as (
                    select join0.[VendorName] Select0, Sum ( main.[Amount] ) Value0
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                ) ,
                [Aggregation1] as (
                    select join0.[VendorName] Select0, Count ( main.[Id] ) Value0
                    from [TestSchema].[Invoice] main
                    join [TestSchema].[Vendor] join0 on join0.[Id] = main.[VendorId]
                    group by join0.[VendorName]
                )
                select top 10 a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Select0 asc
            ");

            filterParams.Names.Should().HaveCount(0);
        }

        private void AssertSameSql(string actual, string expected)
        {
            static string Flatten(string sql) => new Regex("\\s+").Replace(sql, " ").Trim();
            var flatActual = Flatten(actual);
            var flatExpected = Flatten(expected);

            var minLength = Math.Min(flatActual.Length, flatExpected.Length);
            var startOfDifference = -1;

            for (var n = 0; n < minLength; n++)
            {
                if (flatActual[n] != flatExpected[n])
                {
                    startOfDifference = n;
                    break;
                }
            }

            if (startOfDifference != -1)
            {
                var start = Math.Max(0, startOfDifference - 20);
                flatActual.Substring(start).Should().Be(flatExpected.Substring(start));
            }

            flatActual.Should().Be(flatExpected);
        }
    }
}
