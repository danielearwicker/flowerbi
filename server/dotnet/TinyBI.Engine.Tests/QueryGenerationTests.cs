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
        public static readonly Schema Schema = new Schema(typeof(TestSchema));

        public class TestFormatter : ISqlFormatter
        {
            public string Identifier(string name) => $"|{name}|";
            public string EscapedIdentifierPair(string id1, string id2) => $"{id1}!{id2}";
            public string SkipAndTake(long skip, int take) => $"skip:{skip} take:{take}";
        }

        private static readonly ISqlFormatter Formatter = new TestFormatter();

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
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select Count(|main|!|VendorName|) Value0
                from |TestSchema|!|Vendor| main
                order by Count(|main|!|VendorName|)
                desc skip:5 take:10
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
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();

            // As filter is on PK of Vendor, can just use FK of Invoice, avoid join
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select Count(|main|!|Amount|) Value0
                from |TestSchema|!|Invoice| main
                where |main|!|VendorId| = @filter0
                order by Count(|main|!|Amount|) desc
                skip:5 take:10
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
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select |join0|!|VendorName| Select0, Sum(|main|!|Amount|) Value0  
                from |TestSchema|!|Invoice| main  
                join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                group by |join0|!|VendorName|
                order by Sum(|main|!|Amount|) desc
                skip:5 take:10
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
                Totals = true,
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select |join0|!|VendorName| Select0, Sum(|main|!|Amount|) Value0
                from |TestSchema|!|Invoice| main
                join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                group by |join0|!|VendorName|
                order by Sum(|main|!|Amount|) desc
                skip:5 take:10 ;
                select Sum(|main|!|Amount|) Value0
                from |TestSchema|!|Invoice| main
                order by Sum(|main|!|Amount|) desc
                skip:0 take:1
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
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                with Aggregation0 as (
                    select |join0|!|VendorName| Select0, Sum(|main|!|Amount|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    group by |join0|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |join0|!|VendorName| Select0, Count(|main|!|Id|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    group by |join0|!|VendorName|
                )
                select a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Value0 desc
                skip:5 take:10
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
                Totals = true,
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                with Aggregation0 as (
                    select |join0|!|VendorName| Select0, Sum(|main|!|Amount|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    group by |join0|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |join0|!|VendorName| Select0, Count(|main|!|Id|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    group by |join0|!|VendorName|
                )
                select a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Value0 desc
                skip:5 take:10 ;

                with Aggregation0 as (
                    select Sum(|main|!|Amount|) Value0
                    from |TestSchema|!|Invoice| main
                ) ,
                Aggregation1 as (
                    select Count(|main|!|Id|) Value0
                    from |TestSchema|!|Invoice| main
                )
                select a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                cross join Aggregation1 a1
                order by a0.Value0 desc
                skip:0 take:1
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
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                with Aggregation0 as (
                    select |join0|!|VendorName| Select0, Sum(|main|!|Amount|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    group by |join0|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |join0|!|VendorName| Select0, Count(|main|!|Id|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    where |main|!|Paid| = @filter0
                    group by |join0|!|VendorName|
                )
                select a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Value0 desc
                skip:5 take:10
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
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);

            var extra = new Filter(new FilterJson
            {
                Column = "Invoice.Paid",
                Operator = "=",
                Value = JsonDocument.Parse("true").RootElement
            },
                Schema);

            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, new[] { extra }), @"
                select |join0|!|VendorName| Select0, Sum(|main|!|Amount|) Value0
                from |TestSchema|!|Invoice| main
                join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                where |main|!|Paid| = @filter0
                group by |join0|!|VendorName|
                order by Sum(|main|!|Amount|) desc
                skip:5 take:10
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
                OrderBy = new List<OrderingJson> { new OrderingJson { Column = "Vendor.VendorName" } },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                with Aggregation0 as (
                    select |join0|!|VendorName| Select0, Sum(|main|!|Amount|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    group by |join0|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |join0|!|VendorName| Select0, Count(|main|!|Id|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    group by |join0|!|VendorName|
                )
                select a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Select0 asc
                skip:5 take:10
            ");

            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void DoubleAggregationMultipleSelects()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName", "Department.DepartmentName" },
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
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                with Aggregation0 as (
                    select |join0|!|VendorName| Select0,
                           |join1|!|DepartmentName| Select1,
                           Sum(|main|!|Amount|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    join |TestSchema|!|Department| join1 on |join1|!|Id| = |main|!|DepartmentId|
                    group by |join0|!|VendorName| , |join1|!|DepartmentName|
                ) ,
                Aggregation1 as (
                    select |join0|!|VendorName| Select0,
                           |join1|!|DepartmentName| Select1,
                           Count(|main|!|Id|) Value0
                    from |TestSchema|!|Invoice| main
                    join |TestSchema|!|Vendor| join0 on |join0|!|Id| = |main|!|VendorId|
                    join |TestSchema|!|Department| join1 on |join1|!|Id| = |main|!|DepartmentId|
                    group by |join0|!|VendorName| , |join1|!|DepartmentName|
                )
                select a0.Select0, a0.Select1, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on
                    a1.Select0 = a0.Select0 and
                    a1.Select1 = a0.Select1
                order by a0.Value0 desc
                skip:5 take:10
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
