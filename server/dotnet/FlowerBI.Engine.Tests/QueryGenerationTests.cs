using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowerBI.Engine.JsonModels;
using FluentAssertions;
using Xunit;

namespace FlowerBI.Engine.Tests
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MinimalSelectOneColumn(bool allowDuplicates)
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
                Take = 10,
                AllowDuplicates = allowDuplicates
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select Count(|tbl0|!|VendorName|) Value0
                from |TestSchema|!|Vendor| tbl0
                order by Count(|tbl0|!|VendorName|)
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
            /*
                select Count(|tbl0|!|Amount|) Value0
                from |TestSchema|!|Invoice| tbl0
                where |tbl0|!|VendorId| = @filter0
                order by Count(|tbl0|!|Amount|) desc
                skip:5 take:10
             */

            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select Count(|tbl0|!|Amount|) Value0
                from |TestSchema|!|Invoice| tbl0
                join |TestSchema|!|Vendor| tbl1 on |tbl1|!|Id| = |tbl0|!|VendorId|
                where |tbl1|!|Id| = @filter0
                order by Count(|tbl0|!|Amount|) desc
                skip:5 take:10
            ");

            filterParams.Names.Should().HaveCount(1);
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
                Take = 10,
                AllowDuplicates = allowDuplicates,
                Comment = comment,
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), $@"{expectedComment}
                select |tbl0|!|VendorName| Select0, Sum(|tbl1|!|Amount|) Value0
                from |TestSchema|!|Invoice| tbl1
                join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                group by |tbl0|!|VendorName|
                order by Sum(|tbl1|!|Amount|) desc
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
                select |tbl0|!|VendorName| Select0, Sum(|tbl1|!|Amount|) Value0
                from |TestSchema|!|Invoice| tbl1
                join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                group by |tbl0|!|VendorName|
                order by Sum(|tbl1|!|Amount|) desc
                skip:5 take:10 ;
                select Sum(|tbl0|!|Amount|) Value0
                from |TestSchema|!|Invoice| tbl0
                skip:0 take:1
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Theory]
        [InlineData(AggregationType.Min)]
        [InlineData(AggregationType.Max)]
        public void AggregationFunctions(AggregationType type)
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = type
                    }
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), $@"
                select |tbl0|!|VendorName| Select0, {type}(|tbl1|!|Amount|) Value0
                from |TestSchema|!|Invoice| tbl1
                join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                group by |tbl0|!|VendorName|
                order by {type}(|tbl1|!|Amount|) desc
                skip:5 take:10
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Theory]
        [InlineData("suspicious \r\ncomment */ drop tables;", "/* suspicious \r\ncomment drop tables */")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void DoubleAggregation(string comment, string expectedComment)
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
                Take = 10,
                Comment = comment
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @$"{expectedComment}
                with Aggregation0 as (
                    select |tbl0|!|VendorName| Select0, Sum(|tbl1|!|Amount|) Value0
                    from |TestSchema|!|Invoice| tbl1
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                    group by |tbl0|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |tbl0|!|VendorName| Select0, Count(|tbl1|!|Id|) Value0
                    from |TestSchema|!|Invoice| tbl1
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                    group by |tbl0|!|VendorName|
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
                    select |tbl0|!|VendorName| Select0, Sum(|tbl1|!|Amount|) Value0
                    from |TestSchema|!|Invoice| tbl1
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                    group by |tbl0|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |tbl0|!|VendorName| Select0, Count(|tbl1|!|Id|) Value0
                    from |TestSchema|!|Invoice| tbl1
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                    group by |tbl0|!|VendorName|
                )
                select a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Value0 desc
                skip:5 take:10 ;

                with Aggregation0 as (
                    select Sum(|tbl0|!|Amount|) Value0
                    from |TestSchema|!|Invoice| tbl0
                ) ,
                Aggregation1 as (
                    select Count(|tbl0|!|Id|) Value0
                    from |TestSchema|!|Invoice| tbl0
                )
                select a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                cross join Aggregation1 a1
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
                    select |tbl0|!|VendorName| Select0, Sum(|tbl1|!|Amount|) Value0
                    from |TestSchema|!|Invoice| tbl1
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                    group by |tbl0|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |tbl0|!|VendorName| Select0, Count(|tbl1|!|Id|) Value0
                    from |TestSchema|!|Invoice| tbl1
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                    where |tbl1|!|Paid| = @filter0
                    group by |tbl0|!|VendorName|
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
                select |tbl0|!|VendorName| Select0, Sum(|tbl1|!|Amount|) Value0
                from |TestSchema|!|Invoice| tbl1
                join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                where |tbl1|!|Paid| = @filter0
                group by |tbl0|!|VendorName|
                order by Sum(|tbl1|!|Amount|) desc
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
                    select |tbl0|!|VendorName| Select0, Sum(|tbl1|!|Amount|) Value0
                    from |TestSchema|!|Invoice| tbl1
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                    group by |tbl0|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |tbl0|!|VendorName| Select0, Count(|tbl1|!|Id|) Value0
                    from |TestSchema|!|Invoice| tbl1
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl1|!|VendorId|
                    group by |tbl0|!|VendorName|
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
                    select |tbl0|!|VendorName| Select0,
                           |tbl1|!|DepartmentName| Select1,
                           Sum(|tbl2|!|Amount|) Value0
                    from |TestSchema|!|Invoice| tbl2
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl2|!|VendorId|
                    join |TestSchema|!|Department| tbl1 on |tbl1|!|Id| = |tbl2|!|DepartmentId|
                    group by |tbl0|!|VendorName| , |tbl1|!|DepartmentName|
                ) ,
                Aggregation1 as (
                    select |tbl0|!|VendorName| Select0,
                           |tbl1|!|DepartmentName| Select1,
                           Count(|tbl2|!|Id|) Value0
                    from |TestSchema|!|Invoice| tbl2
                    join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl2|!|VendorId|
                    join |TestSchema|!|Department| tbl1 on |tbl1|!|Id| = |tbl2|!|DepartmentId|
                    group by |tbl0|!|VendorName| , |tbl1|!|DepartmentName|
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

        [Fact]
        public void ManyToMany()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName", "Tag.TagName" },
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
                select |tbl0|!|VendorName| Select0,
                       |tbl1|!|TagName| Select1,
                       Sum(|tbl2|!|Amount|) Value0
                from |TestSchema|!|InvoiceTag| tbl3
                join |TestSchema|!|Invoice| tbl2 on |tbl2|!|Id| = |tbl3|!|InvoiceId|
                join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl2|!|VendorId|
                join |TestSchema|!|Tag| tbl1 on |tbl1|!|Id| = |tbl3|!|TagId|
                group by |tbl0|!|VendorName| , |tbl1|!|TagName|
                order by Sum(|tbl2|!|Amount|) desc
                skip:5 take:10
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void NoAggregation()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName", "Tag.TagName" },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select |tbl0|!|VendorName| Select0,
                       |tbl1|!|TagName| Select1
                from |TestSchema|!|InvoiceTag| tbl2
                join |TestSchema|!|Invoice| tbl3 on |tbl3|!|Id| = |tbl2|!|InvoiceId|
                join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl3|!|VendorId|
                join |TestSchema|!|Tag| tbl1 on |tbl1|!|Id| = |tbl2|!|TagId|
                group by |tbl0|!|VendorName| , |tbl1|!|TagName|
                skip:5 take:10

            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void NoAggregationAllowingDuplicates()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName", "Tag.TagName" },
                Skip = 5,
                Take = 10,
                AllowDuplicates = true // only has effect if no aggregations
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select |tbl0|!|VendorName| Select0,
                       |tbl1|!|TagName| Select1
                from |TestSchema|!|InvoiceTag| tbl2
                join |TestSchema|!|Invoice| tbl3 on |tbl3|!|Id| = |tbl2|!|InvoiceId|
                join |TestSchema|!|Vendor| tbl0 on |tbl0|!|Id| = |tbl3|!|VendorId|
                join |TestSchema|!|Tag| tbl1 on |tbl1|!|Id| = |tbl2|!|TagId|
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