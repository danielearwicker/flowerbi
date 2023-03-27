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
                select Count(|tbl00|!|VendorName|) Value0
                from |Testing|!|Supplier| tbl00
                order by Count(|tbl00|!|VendorName|)
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

            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select Count(|tbl00|!|FancyAmount|) Value0
                from |Testing|!|Invoice| tbl00
                join |Testing|!|Supplier| tbl01 on |tbl01|!|Id| = |tbl00|!|VendorId|
                where |tbl01|!|Id| = @filter0
                order by Count(|tbl00|!|FancyAmount|) desc
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
                select |tbl00|!|VendorName| Select0, Sum(|tbl01|!|FancyAmount|) Value0 
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id| 
                group by |tbl00|!|VendorName| 
                order by Sum(|tbl01|!|FancyAmount|) desc 
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
                select |tbl00|!|VendorName| Select0, Sum(|tbl01|!|FancyAmount|) Value0
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                group by |tbl00|!|VendorName|
                order by Sum(|tbl01|!|FancyAmount|) desc
                skip:5 take:10 ;
                select Sum(|tbl00|!|FancyAmount|) Value0
                from |Testing|!|Invoice| tbl00
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
                select |tbl00|!|VendorName| Select0, {type}(|tbl01|!|FancyAmount|) Value0
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                group by |tbl00|!|VendorName|
                order by {type}(|tbl01|!|FancyAmount|) desc
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
                    select |tbl00|!|VendorName| Select0, Sum(|tbl01|!|FancyAmount|) Value0
                    from |Testing|!|Supplier| tbl00
                    join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                    group by |tbl00|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |tbl00|!|VendorName| Select0, Count(|tbl01|!|Id|) Value0
                    from |Testing|!|Supplier| tbl00
                    join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                    group by |tbl00|!|VendorName|
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
                    select |tbl00|!|VendorName| Select0, Sum(|tbl01|!|FancyAmount|) Value0
                    from |Testing|!|Supplier| tbl00
                    join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                    group by |tbl00|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |tbl00|!|VendorName| Select0, Count(|tbl01|!|Id|) Value0
                    from |Testing|!|Supplier| tbl00
                    join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                    group by |tbl00|!|VendorName|
                )
                select a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                left join Aggregation1 a1 on a1.Select0 = a0.Select0
                order by a0.Value0 desc
                skip:5 take:10 ;

                with Aggregation0 as (
                    select Sum(|tbl00|!|FancyAmount|) Value0
                    from |Testing|!|Invoice| tbl00
                ) ,
                Aggregation1 as (
                    select Count(|tbl00|!|Id|) Value0
                    from |Testing|!|Invoice| tbl00
                )
                select a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0
                cross join Aggregation1 a1
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
                    select |tbl00|!|VendorName| Select0, Sum(|tbl01|!|FancyAmount|) Value0
                    from |Testing|!|Supplier| tbl00
                    join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                    group by |tbl00|!|VendorName|
                ) ,
                Aggregation1 as (
                    select |tbl00|!|VendorName| Select0, Count(|tbl01|!|Id|) Value0
                    from |Testing|!|Supplier| tbl00
                    join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                    where |tbl01|!|Paid| = @filter0
                    group by |tbl00|!|VendorName|
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
                select |tbl00|!|VendorName| Select0, Sum(|tbl01|!|FancyAmount|) Value0
                from |Testing|!|Supplier| tbl00
                join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                where |tbl01|!|Paid| = @filter0
                group by |tbl00|!|VendorName|
                order by Sum(|tbl01|!|FancyAmount|) desc
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
                    select |tbl00|!|VendorName| Select0, Sum(|tbl01|!|FancyAmount|) Value0 
                    from |Testing|!|Supplier| tbl00 
                    join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id| 
                    group by |tbl00|!|VendorName| 
                ) , 
                Aggregation1 as ( 
                    select |tbl00|!|VendorName| Select0, Count(|tbl01|!|Id|) Value0 
                    from |Testing|!|Supplier| tbl00 
                    join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id| 
                    group by |tbl00|!|VendorName| 
                ) 
                select a0.Select0, a0.Value0 Value0 , a1.Value0 Value1
                from Aggregation0 a0 left join Aggregation1 a1
                on a1.Select0 = a0.Select0
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
                    select |tbl00|!|VendorName| Select0,
                           |tbl01|!|DepartmentName| Select1,
                           Sum(|tbl02|!|FancyAmount|) Value0
                    from |Testing|!|Supplier| tbl00
                    join |Testing|!|Department| tbl01 on |tbl01|!|Id| = |tbl00|!|DepartmentId|
                    join |Testing|!|Invoice| tbl02 on |tbl02|!|VendorId| = |tbl00|!|Id| and |tbl02|!|DepartmentId| = |tbl01|!|Id|
                    group by |tbl00|!|VendorName| , |tbl01|!|DepartmentName|
                ) ,
                Aggregation1 as (
                    select |tbl00|!|VendorName| Select0,
                           |tbl01|!|DepartmentName| Select1,
                           Count(|tbl02|!|Id|) Value0
                    from |Testing|!|Supplier| tbl00
                    join |Testing|!|Department| tbl01 on |tbl01|!|Id| = |tbl00|!|DepartmentId|
                    join |Testing|!|Invoice| tbl02 on |tbl02|!|VendorId| = |tbl00|!|Id| and |tbl02|!|DepartmentId| = |tbl01|!|Id|
                    group by |tbl00|!|VendorName| , |tbl01|!|DepartmentName|
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
                select |tbl00|!|VendorName| Select0, 
                       |tbl01|!|TagName| Select1,
                       Sum(|tbl02|!|FancyAmount|) Value0 
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Invoice| tbl02 on |tbl02|!|VendorId| = |tbl00|!|Id| 
                join |Testing|!|InvoiceTag| tbl03 on |tbl03|!|InvoiceId| = |tbl02|!|Id| 
                join |Testing|!|Tag| tbl01 on |tbl01|!|Id| = |tbl03|!|TagId| 
                group by |tbl00|!|VendorName| , |tbl01|!|TagName| 
                order by Sum(|tbl02|!|FancyAmount|) 
                desc skip:5 take:10
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void MultipleManyToMany()
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
                Filters = new List<FilterJson>
                {
                    new FilterJson { Column = "Category.CategoryName", Operator = "=", Value = "shopping" }
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select |tbl00|!|VendorName| Select0, 
                       |tbl01|!|TagName| Select1,
                       Sum(|tbl02|!|FancyAmount|) Value0 
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Invoice| tbl02 on |tbl02|!|VendorId| = |tbl00|!|Id| 
                join |Testing|!|InvoiceTag| tbl04 on |tbl04|!|InvoiceId| = |tbl02|!|Id| 
                join |Testing|!|Tag| tbl01 on |tbl01|!|Id| = |tbl04|!|TagId| 
                join |Testing|!|InvoiceCategory| tbl05 on |tbl05|!|InvoiceId| = |tbl02|!|Id| 
                join |Testing|!|Category| tbl03 on |tbl03|!|Id| = |tbl05|!|CategoryId| 
                where |tbl03|!|CategoryName| = @filter0 
                group by |tbl00|!|VendorName| , |tbl01|!|TagName| 
                order by Sum(|tbl02|!|FancyAmount|) desc 
                skip:5 take:10
            ");
            
            filterParams.Names.Should().HaveCount(1);
        }

        [Fact]
        public void MultipleManyToManyWithSpecifiedJoins()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName", "AnnotationValue.Value@x", "AnnotationValue.Value@y" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    }
                },
                Filters = new List<FilterJson>
                {
                    new FilterJson { Column = "AnnotationName.Name@x", Operator = "=", Value = "math" },
                    new FilterJson { Column = "AnnotationName.Name@y", Operator = "=", Value = "shopping" }
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select |tbl00|!|VendorName| Select0, 
                       |tbl01|!|Value| Select1, 
                       |tbl02|!|Value| Select2, 
                       Sum(|tbl03|!|FancyAmount|) Value0 
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Invoice| tbl03 on |tbl03|!|VendorId| = |tbl00|!|Id| 
                join |Testing|!|InvoiceAnnotation| tbl06 on |tbl06|!|InvoiceId| = |tbl03|!|Id| 
                join |Testing|!|AnnotationValue| tbl01 on |tbl01|!|Id| = |tbl06|!|AnnotationValueId| 
                join |Testing|!|AnnotationName| tbl04 on |tbl04|!|Id| = |tbl01|!|AnnotationNameId| 
                join |Testing|!|InvoiceAnnotation| tbl07 on |tbl07|!|InvoiceId| = |tbl03|!|Id| 
                join |Testing|!|AnnotationValue| tbl02 on |tbl02|!|Id| = |tbl07|!|AnnotationValueId| 
                join |Testing|!|AnnotationName| tbl05 on |tbl05|!|Id| = |tbl02|!|AnnotationNameId| 
                where |tbl04|!|Name| = @filter0 and |tbl05|!|Name| = @filter1
                group by |tbl00|!|VendorName| , |tbl01|!|Value| , |tbl02|!|Value| 
                order by Sum(|tbl03|!|FancyAmount|) 
                desc skip:5 take:10
            ");
            
            filterParams.Names.Should().HaveCount(2);
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
                select |tbl00|!|VendorName| Select0, 
                       |tbl01|!|TagName| Select1 
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Invoice| tbl02 on |tbl02|!|VendorId| = |tbl00|!|Id| 
                join |Testing|!|InvoiceTag| tbl03 on |tbl03|!|InvoiceId| = |tbl02|!|Id| 
                join |Testing|!|Tag| tbl01 on |tbl01|!|Id| = |tbl03|!|TagId| 
                group by |tbl00|!|VendorName| , |tbl01|!|TagName| 
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
                select |tbl00|!|VendorName| Select0, 
                       |tbl01|!|TagName| Select1 
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Invoice| tbl02 on |tbl02|!|VendorId| = |tbl00|!|Id| 
                join |Testing|!|InvoiceTag| tbl03 on |tbl03|!|InvoiceId| = |tbl02|!|Id| 
                join |Testing|!|Tag| tbl01 on |tbl01|!|Id| = |tbl03|!|TagId| skip:5 take:10
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void AggregationCountDistinct()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.CountDistinct
                    }
                },
                Skip = 5,
                Take = 10,                
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), $@"
                select |tbl00|!|VendorName| Select0, count(distinct |tbl01|!|FancyAmount|) Value0 
                from |Testing|!|Supplier| tbl00
                join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id|
                group by |tbl00|!|VendorName|
                order by count(distinct |tbl01|!|FancyAmount|)
                desc skip:5 take:10
            ");
            filterParams.Names.Should().HaveCount(0);
        }

        [Fact]
        public void MoreThanOneJoinDependency()
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
                Filters = new List<FilterJson>
                {
                    new FilterJson { Column = "Department.DepartmentName", Operator = "=", Value = "Accounts" }
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), $@"
                select |tbl00|!|VendorName| Select0, Sum(|tbl01|!|FancyAmount|) Value0 
                from |Testing|!|Supplier| tbl00 
                join |Testing|!|Department| tbl02 on |tbl02|!|Id| = |tbl00|!|DepartmentId| 
                join |Testing|!|Invoice| tbl01 on |tbl01|!|VendorId| = |tbl00|!|Id| 
                                              and |tbl01|!|DepartmentId| = |tbl02|!|Id| 
                where |tbl02|!|DepartmentName| = @filter0 
                group by |tbl00|!|VendorName| 
                order by Sum(|tbl01|!|FancyAmount|) desc 
                skip:5 take:10
            ");
            filterParams.Names.Should().HaveCount(1);
        }

        [Fact]
        public void MultipleManyToManyWithSpecifiedJoinsAndMultipleJoinDependencies()
        {
            var queryJson = new QueryJson
            {
                Select = new List<string> { "Vendor.VendorName", "AnnotationValue.Value@x", "AnnotationValue.Value@y" },
                Aggregations = new List<AggregationJson>
                {
                    new AggregationJson
                    {
                        Column = "Invoice.Amount",
                        Function = AggregationType.Sum
                    }
                },
                Filters = new List<FilterJson>
                {
                    new FilterJson { Column = "AnnotationName.Name@x", Operator = "=", Value = "math" },
                    new FilterJson { Column = "AnnotationName.Name@y", Operator = "=", Value = "shopping" },
                    new FilterJson { Column = "Department.DepartmentName", Operator = "=", Value = "Accounts" }
                },
                Skip = 5,
                Take = 10
            };

            var query = new Query(queryJson, Schema);
            var filterParams = new DictionaryFilterParameters();
            AssertSameSql(query.ToSql(Formatter, filterParams, Enumerable.Empty<Filter>()), @"
                select |tbl00|!|VendorName| Select0,
                       |tbl01|!|Value| Select1,
                       |tbl02|!|Value| Select2,
                       Sum(|tbl03|!|FancyAmount|) Value0
                from |Testing|!|Supplier| tbl00
                join |Testing|!|Department| tbl06 on |tbl06|!|Id| = |tbl00|!|DepartmentId|
                join |Testing|!|Invoice| tbl03 on |tbl03|!|VendorId| = |tbl00|!|Id| and |tbl03|!|DepartmentId| = |tbl06|!|Id|
                join |Testing|!|InvoiceAnnotation| tbl07 on |tbl07|!|InvoiceId| = |tbl03|!|Id| 
                join |Testing|!|AnnotationValue| tbl01 on |tbl01|!|Id| = |tbl07|!|AnnotationValueId| 
                join |Testing|!|AnnotationName| tbl04 on |tbl04|!|Id| = |tbl01|!|AnnotationNameId| 
                join |Testing|!|InvoiceAnnotation| tbl08 on |tbl08|!|InvoiceId| = |tbl03|!|Id| 
                join |Testing|!|AnnotationValue| tbl02 on |tbl02|!|Id| = |tbl08|!|AnnotationValueId| 
                join |Testing|!|AnnotationName| tbl05 on |tbl05|!|Id| = |tbl02|!|AnnotationNameId| 
                where |tbl04|!|Name| = @filter0 and 
                      |tbl05|!|Name| = @filter1 and 
                      |tbl06|!|DepartmentName| = @filter2 
                group by |tbl00|!|VendorName| , |tbl01|!|Value| , |tbl02|!|Value|
                order by Sum(|tbl03|!|FancyAmount|) desc skip:5 take:10
            ");
            
            filterParams.Names.Should().HaveCount(3);
        }

        private void AssertSameSql(string actual, string expected)
        {
            static string Flatten(string sql) => new Regex("\\s+").Replace(sql, " ").Trim();
            var flatActual = Flatten(actual);
            var flatExpected = Flatten(expected);

            flatActual.Should().Be(flatExpected);
        }
    }
}