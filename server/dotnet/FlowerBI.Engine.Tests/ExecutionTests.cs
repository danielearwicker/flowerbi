using System.Text.Json;
using Xunit;
using FluentAssertions;
using FlowerBI.Engine.JsonModels;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;

namespace FlowerBI.Engine.Tests;

public abstract class ExecutionTests
{        
    public static readonly Schema Schema = new(typeof(TestSchema));

    public static readonly Schema ComplicatedSchema = new(typeof(ComplicatedTestSchema));

    protected abstract IDbConnection Db { get; }

    protected abstract ISqlFormatter Formatter { get; }

    private readonly List<string> _log = [];

    private QueryResultJson ExecuteQuery(QueryJson json, Schema schema = null, params Filter[] outerFilters)
        => new Query(json, schema ?? Schema).Run(Formatter, Db, _log.Add, outerFilters);

    private static object Round(object o)
        => o switch
        {
             double d => (decimal)Math.Round(d, 4), // SQLite uses double as substitute for decimal
            _ => o
        };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MinimalSelectOneColumn(bool allowDuplicates)
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
    [InlineData(true)]
    [InlineData(false)]
    public void SingleAggregation(bool allowDuplicates)
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
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected.Single(), Round(x.Aggregated.Single())));

        records.Should().BeEquivalentTo(new[]
            {
                ("[Steve Makes Sandwiches]", 176.24m),
                ("[Manchesterford Supplies Inc]", 164.36m),
                ("[Disgusting Ltd]", 156.14m),
                ("[Statues While You Wait]", 156.24m),
                ("[Tiles Tiles Tiles]", 106.24m),
                ("[Uranium 4 Less]", 88.12m),
                ("[Awnings-R-Us]", 88.12m),
                ("[Pleasant Plc]", 88.12m),
                ("[Mats and More]", 76.24m),
                ("[Party Hats 4 U]", 58.12m),                    
            });

        records.Select(x => x.Item2).Should().BeInDescendingOrder();

        results.Totals.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingleAggregationOrderBySelect(bool descending)
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
            OrderBy = [ new() { Column = "Vendor.VendorName", Descending = descending } ],
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected.Single(), Round(x.Aggregated.Single())));

        records.Should().BeEquivalentTo(descending ?
            [
                ("[Tiles Tiles Tiles]", 106.24m),
                ("[Steve Makes Sandwiches]", 176.24m),
                ("[Statues While You Wait]", 156.24m),
                ("[Stationary Stationery]", 28.12m),
                ("[Pleasant Plc]", 88.12m),
                ("[Party Hats 4 U]", 58.12m),
                ("[Mats and More]", 76.24m),
                ("[Manchesterford Supplies Inc]", 164.36m),
                ("[Handbags-a-Plenty]", 252.48m),
                ("[Disgusting Ltd]", 156.14m),
            ] : new[]
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

    [Fact]
    public void SingleAggregationTotals()
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
            Totals = true,
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected.Single(), Round(x.Aggregated.Single())));

        records.Should().BeEquivalentTo(new[]
            {
                ("[Steve Makes Sandwiches]", 176.24m),
                ("[Manchesterford Supplies Inc]", 164.36m),
                ("[Disgusting Ltd]", 156.14m),
                ("[Statues While You Wait]", 156.24m),
                ("[Tiles Tiles Tiles]", 106.24m),
                ("[Uranium 4 Less]", 88.12m),
                ("[Awnings-R-Us]", 88.12m),
                ("[Pleasant Plc]", 88.12m),
                ("[Mats and More]", 76.24m),
                ("[Party Hats 4 U]", 58.12m),                    
            });

        records.Select(x => x.Item2).Should().BeInDescendingOrder();

        results.Totals.Aggregated.Select(Round).Single().Should().Be(1845.38m);
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
        var records = results.Records.Select(x => (x.Selected.Single(), x.Aggregated.Single()));

        var expected = new[]
        {
            ("[Awnings-R-Us]", 88.12, 88.12),
            ("[Disgusting Ltd]", 68.12, 88.02),
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

        records.Should().BeEquivalentTo(expectedForType);

        records.Select(x => x.Item1).Should().BeInAscendingOrder();

        results.Totals.Should().BeNull();
    }

    [Fact]
    public void SuspiciousComment()
    {
        ExecuteQuery(new()
        {
            Comment = "suspicious \r\ncomment */ drop tables;",
            Aggregations = [new() { Column = "Vendor.VendorName" }],
        });

        _log.Single().StartsWith(
            """
            /* suspicious
            comment drop tables */
            """);
    }
    
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoubleAggregation(bool totals)
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
                },

                new()
                {
                    Column = "Invoice.Id",
                    Function = AggregationType.Count
                }
            ],
            Totals = totals,
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], Round(x.Aggregated[0]), x.Aggregated[1]));

        records.Should().BeEquivalentTo(new[]
            {
                ("[United Cheese]", 406.84m, 7m),
                ("[Handbags-a-Plenty]", 252.48m, 4m),
                ("[Steve Makes Sandwiches]", 176.24m, 2m),
                ("[Manchesterford Supplies Inc]", 164.36m, 3m),
                ("[Disgusting Ltd]", 156.14m, 2m),
                ("[Statues While You Wait]", 156.24m, 2m),
                ("[Tiles Tiles Tiles]", 106.24m, 2m),
                ("[Uranium 4 Less]", 88.12m, 1m),
                ("[Awnings-R-Us]", 88.12m, 1m),
                ("[Pleasant Plc]", 88.12m, 1m),
                ("[Mats and More]", 76.24m, 2m),
                ("[Party Hats 4 U]", 58.12m, 1m),
                ("[Stationary Stationery]", 28.12m, 1m),
            });

        records.Select(x => x.Item2).Should().BeInDescendingOrder();

        if (totals)
        {
            results.Totals.Aggregated.Select(Round).Should().BeEquivalentTo([1845.38m, 29]);
        }
        else
        {
            results.Totals.Should().BeNull();
        }
    }

    [Fact]
    public void DoubleAggregationDifferentFilters()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName"],
            Aggregations =
            [
                new AggregationJson
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                },

                new AggregationJson
                {
                    Column = "Invoice.Id",
                    Function = AggregationType.Count,
                    Filters =
                    [
                        new FilterJson
                        {
                            Column = "Invoice.Paid",
                            Operator = "=",
                            Value = JsonDocument.Parse("true").RootElement
                        }
                    ]
                }
            ]
        };

        var results = ExecuteQuery(queryJson);

        var records = results.Records.Select(x => (x.Selected[0], Round(x.Aggregated[0]), x.Aggregated[1]));

        records.Should().BeEquivalentTo(new[]
            {
                ("[United Cheese]", 406.84m, 2),
                ("[Handbags-a-Plenty]", 252.48m, 0),
                ("[Steve Makes Sandwiches]", 176.24m, 2),
                ("[Manchesterford Supplies Inc]", 164.36m, 2),
                ("[Disgusting Ltd]", 156.14m, 0),
                ("[Statues While You Wait]", 156.24m, 1),
                ("[Tiles Tiles Tiles]", 106.24m, 1),
                ("[Uranium 4 Less]", 88.12m, 0),
                ("[Awnings-R-Us]", 88.12m, 1),
                ("[Pleasant Plc]", 88.12m, 0),
                ("[Mats and More]", 76.24m, 0),
                ("[Party Hats 4 U]", 58.12m, 0),
                ("[Stationary Stationery]", 28.12m, 0),
            });

        records.Select(x => x.Item2).Should().BeInDescendingOrder();
    }

    [Fact]
    public void DoubleAggregationMultipleSelects()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "Department.DepartmentName"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                },

                new()
                {
                    Column = "Invoice.Id",
                    Function = AggregationType.Count
                }
            ]
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], x.Aggregated[0], x.Aggregated[1]));

        // Both Invoice and Supplier are linked to a Department, so "maximum joinage" is required, i.e. 
        // we only see cases where Invoice and Supplier have the same Department
        records.Should().BeEquivalentTo(new[]
            {
                ("[Handbags-a-Plenty]", "Missiles", 186.24m, 2),
                ("[United Cheese]", "Cheese", 166.24m, 2),
                ("[Disgusting Ltd]", "Yoga", 88.02m, 1),
                ("[Statues While You Wait]", "Cheese", 78.12m, 1),
                ("[Party Hats 4 U]", "Marketing", 58.12m, 1),
            });

        records.Select(x => x.Item3).Should().BeInDescendingOrder();
    }

    [Fact]
    public void ManyToMany()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "Tag.TagName"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                }
            ]
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], Round(x.Aggregated[0])));

        records.Should().BeEquivalentTo(new[]
            {
                ("[Handbags-a-Plenty]", "Boring", 126.24m),
                ("[Handbags-a-Plenty]", "Interesting", 98.12m),
                ("[Handbags-a-Plenty]", "Lethal", 98.12m),
                ("[Steve Makes Sandwiches]", "Interesting", 88.12m),
                ("[Statues While You Wait]", "Boring", 78.12m),
                ("[United Cheese]", "Lethal", 58.12m),
                ("[United Cheese]", "Interesting", 58.12m),
                ("[Party Hats 4 U]", "Boring", 58.12m),
            });

        records.Select(x => x.Item3).Should().BeInDescendingOrder();
    }

    [Fact]
    public void MultipleManyToMany()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "Tag.TagName"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                }
            ],
            Filters =
            [
                new() { Column = "Category.CategoryName", Operator = "=", Value = "Regular" }
            ]
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], x.Aggregated[0]));

        records.Should().BeEquivalentTo(new[]
            {
                ("[Statues While You Wait]", "Boring", 78.12m),
                ("[Handbags-a-Plenty]", "Boring", 28.12m),
            });

        records.Select(x => x.Item3).Should().BeInDescendingOrder();
    }

    [Fact]
    public void MultipleManyToManyWithSpecifiedJoins()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "AnnotationValue.Value@x", "AnnotationValue.Value@y"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                }
            ],
            Filters =
            [
                new() { Column = "AnnotationName.Name@x", Operator = "=", Value = "Approver" },
                new() { Column = "AnnotationName.Name@y", Operator = "=", Value = "Instructions" }
            ]
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], x.Selected[2], x.Aggregated[0]));

        records.Should().BeEquivalentTo(new[]
            {
                ("[Pleasant Plc]", "Jill", "Pay quickly", 88.12m),
                ("[Pleasant Plc]", "Jill", "Brown envelope job", 88.12m),
                ("[Statues While You Wait]", "Snarvu", "Cash only", 78.12m),
                ("[United Cheese]", "Gupta", "Cash only", 18.12m),
            });

        records.Select(x => x.Item4).Should().BeInDescendingOrder();
    }

    [Fact]
    public void NoAggregation()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "Tag.TagName"],
        };

        var results = ExecuteQuery(queryJson);

        results.Records.Where(x => x.Aggregated != null).Should().BeEmpty();

        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1]));

        records.Should().BeEquivalentTo(new[]
            {
                ("[United Cheese]", "Interesting"),
                ("[United Cheese]", "Lethal"),
                ("[Party Hats 4 U]", "Boring"),
                ("[Statues While You Wait]", "Boring"),
                ("[Steve Makes Sandwiches]", "Interesting"),
                ("[Handbags-a-Plenty]", "Interesting"),
                ("[Handbags-a-Plenty]", "Lethal"),
                ("[Handbags-a-Plenty]", "Boring"),
            });
    }

    [Fact]
    public void NoAggregationAllowingDuplicates()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "Tag.TagName"],
            AllowDuplicates = true,
        };

        var results = ExecuteQuery(queryJson);

        results.Records.Where(x => x.Aggregated != null).Should().BeEmpty();

        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1]));

        records.Should().BeEquivalentTo(new[]
            {
                ("[United Cheese]", "Interesting"),
                ("[United Cheese]", "Lethal"),
                ("[Party Hats 4 U]", "Boring"),
                ("[Statues While You Wait]", "Boring"),
                ("[Steve Makes Sandwiches]", "Interesting"),
                ("[Handbags-a-Plenty]", "Interesting"),
                ("[Handbags-a-Plenty]", "Lethal"),
                ("[Handbags-a-Plenty]", "Boring"),
                ("[Handbags-a-Plenty]", "Boring"),
            });
    }

    [Fact]
    public void AggregationCountDistinct()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.CountDistinct
                },
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.CountDistinct,
                    Filters =
                    [
                        new FilterJson
                        {
                            Column = "Invoice.Paid",
                            Operator = "=",
                            Value = true
                        }
                    ]
                }
            ],
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], x.Aggregated[0], x.Aggregated[1] ?? 0));

        records.Should().BeEquivalentTo(new[]
            {
                ("[Awnings-R-Us]", 1, 1),
                ("[Disgusting Ltd]", 2, 0),
                ("[Handbags-a-Plenty]", 4, 0),
                ("[Manchesterford Supplies Inc]", 3, 2),
                ("[Mats and More]", 2, 0),
                ("[Party Hats 4 U]", 1, 0),
                ("[Pleasant Plc]", 1, 0),
                ("[Stationary Stationery]", 1, 0),
                ("[Statues While You Wait]", 1, 1),
                ("[Steve Makes Sandwiches]", 1, 1),
                ("[Tiles Tiles Tiles]", 2, 1),
                ("[United Cheese]", 6, 2),
                ("[Uranium 4 Less]", 1, 0),
            });

        records.Select(x => x.Item2).Should().BeInDescendingOrder();
    }

    [Fact]
    public void MultipleManyToManyWithSpecifiedJoinsAndMultipleJoinDependencies()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "AnnotationValue.Value@x", "AnnotationValue.Value@y"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                }
            ],
            Filters =
            [
                new() { Column = "AnnotationName.Name@x", Operator = "=", Value = "Approver" },
                new() { Column = "AnnotationName.Name@y", Operator = "=", Value = "Instructions" },
                new() { Column = "Department.DepartmentName", Operator = "=", Value = "Cheese" }
            ],
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], x.Selected[2], Round(x.Aggregated[0])));
        
        records.Should().BeEquivalentTo(new[]
        {
            ("[Statues While You Wait]", "Snarvu", "Cash only", 78.12m)
        });
    }

    [Fact]
    public void ManyToManyWithComplicatedSchema()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "Category.CategoryName"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                }
            ],
            Filters =
            [
                new() { Column = "Department.DepartmentName", Operator = "=", Value = "Cheese" }
            ],
        };

        var results = ExecuteQuery(queryJson, ComplicatedSchema);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], Round(x.Aggregated[0])));

        records.Should().BeEquivalentTo(new[]
        {
            ("United Cheese", "Regular", 98.12m),
            ("Statues While You Wait", "Regular", 78.12m),
        });
    }

    [Fact]
    public void ManyToManyConjointWithComplicatedSchema()
    {
        var queryJson = new QueryJson
        {
            Select = ["AnnotationValue.Value@x", "AnnotationValue.Value@y"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                }
            ],
            Filters =
            [
                new() { Column = "AnnotationName.Name@x", Operator = "=", Value = "Approver" },
                new() { Column = "AnnotationName.Name@y", Operator = "=", Value = "Movie" },
                new() { Column = "Department.DepartmentName", Operator = "=", Value = "Cheese" }
            ]
        };

        var results = ExecuteQuery(queryJson, ComplicatedSchema);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], Round(x.Aggregated[0])));

        records.Should().BeEquivalentTo(new[]
        {
            ("Snarvu", "Robocop", 78.12m),
        });
    }

    [Theory]
    [InlineData(OrderingType.Select, 0, -1)]
    [InlineData(OrderingType.Select, 1, null)]
    [InlineData(OrderingType.Value, 0, 0)]
    [InlineData(OrderingType.Value, 1, 1)]
    [InlineData(OrderingType.Value, 3, null)]
    [InlineData(OrderingType.Calculation, 0, 2)]
    [InlineData(OrderingType.Calculation, 1, 3)]
    [InlineData(OrderingType.Calculation, 2, 4)]
    [InlineData(OrderingType.Calculation, 3, 5)]
    [InlineData(OrderingType.Calculation, 4, null)]
    public void CalculationsAndIndexedOrderBy(OrderingType orderingType, int orderingIndex, int? expectedOrderBy)
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName"],
            Aggregations =
            [
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
            ],
            Calculations =
            [
                new() { Aggregation = 1 },
                new()
                {                        
                    First = new() 
                    {                             
                        First = new() { Aggregation = 0 }, 
                        Operator = "??", 
                        Second = new() { Value = 42 } 
                    },
                    Operator = "+",
                    Second = new() { Value = 3 },
                },
                new()
                {                        
                    First = new() { Aggregation = 0 },
                    Operator = "/",
                    Second = new() { Value = 2 },
                },
                new()
                {                        
                    First = new() { Value = 50 },
                    Operator = "-",
                    Second = new() { Aggregation = 0 },
                }
            ],
            OrderBy =
            [
                new() { Type = orderingType, Index = orderingIndex } 
            ]
        };

        var func = new Func<QueryResultJson>(() => ExecuteQuery(queryJson));
        if (expectedOrderBy == null)
        {
            func.Should().Throw<ArgumentOutOfRangeException>();
            return;
        }
        
        var results = func();
        var records = results.Records.Select(x => (x.Selected[0], Round(x.Aggregated[0]), x.Aggregated[1], x.Aggregated[2], 
                                                    Round(x.Aggregated[3]), Round(x.Aggregated[4]), Round(x.Aggregated[5])));
        records.Should().BeEquivalentTo(new[]
        {
            ("[Uranium 4 Less]", 88.12m, 1, 1, 91.12m, 44.06m, -38.12),
            ("[Stationary Stationery]", 28.12m, 1, 1, 31.12m, 14.06m, 21.88),
            ("[Pleasant Plc]", 88.12m, 1, 1, 91.12m, 44.06m, -38.12),
            ("[Party Hats 4 U]", 58.12m, 1, 1, 61.12m, 29.06m, -8.12),
            ("[Awnings-R-Us]", 88.12m, 1, 1, 91.12m, 44.06m, -38.12),
            ("[Tiles Tiles Tiles]", 106.24m, 2, 2, 109.24m, 53.12m, -56.24),
            ("[Steve Makes Sandwiches]", 176.24m, 2, 2, 179.24m, 88.12m, -126.24),
            ("[Statues While You Wait]", 156.24m, 2, 2, 159.24m, 78.12m, -106.24),
            ("[Disgusting Ltd]", 156.14m, 2, 2, 159.14m, 78.07m, -106.14),
            ("[Mats and More]", 76.24m, 2, 2, 79.24m, 38.12m, -26.24),
            ("[Manchesterford Supplies Inc]", 164.36m, 3, 3, 167.36m, 82.18m, -114.36),
            ("[Handbags-a-Plenty]", 252.48m, 4, 4, 255.48m, 126.24m, -202.48),
            ("[United Cheese]", 406.84m, 7, 7, 409.84m, 203.42m, -356.84),
        });

        var orderedBy = expectedOrderBy == -1 
            ? results.Records.Select(x => x.Selected[0])
            : results.Records.Select(x => x.Aggregated[expectedOrderBy.Value]);
        
        orderedBy.Should().BeInAscendingOrder();
    }

    [Fact]
    public void CalculationsAndMultiSelect()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName", "Vendor.DepartmentId"],
            Aggregations =
            [
                new()
                {
                    Column = "Invoice.Amount",
                    Function = AggregationType.Sum
                },

                new()
                {
                    Column = "Invoice.Id",
                    Function = AggregationType.Count
                }
            ],
            Calculations =
            [
                new() { Aggregation = 1 },
                new()
                {                        
                    First = new() 
                    {                             
                        First = new() { Aggregation = 0 }, 
                        Operator = "??", 
                        Second = new() { Value = 42 } 
                    },
                    Operator = "+",
                    Second = new() { Value = 3 },
                },
                new()
                {                        
                    First = new() { Aggregation = 0 },
                    Operator = "/",
                    Second = new() { Aggregation = 1 },
                }                    
            ],
            OrderBy =
            [
                new OrderingJson { Type = OrderingType.Select, Index = 1 } 
            ],
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], Round(x.Aggregated[0]), x.Aggregated[1], 
                                                    x.Aggregated[2], Round(x.Aggregated[3]), Round(x.Aggregated[4])));

        records.Should().BeEquivalentTo(
            [
                ("[Stationary Stationery]", 1, 28.12m, 1, 1, 31.12m, 28.12m),
                ("[Party Hats 4 U]", 2, 58.12m, 1, 1, 61.12m, 58.12m),
                ("[Tiles Tiles Tiles]", 2, 106.24m, 2, 2, 109.24m, 53.12m),
                ("[Handbags-a-Plenty]", 3, 252.48m, 4, 4, 255.48m, 63.12m),
                ("[Pleasant Plc]", 3, 88.12m, 1, 1, 91.12m, 88.12m),
                ("[Uranium 4 Less]", 3, 88.12m, 1, 1, 91.12m, 88.12m),
                ("[Awnings-R-Us]", 4, 88.12m, 1, 1, 91.12m, 88.12m),
                ("[Manchesterford Supplies Inc]", 4, 164.36m, 3, 3, 167.36m, 54.7867m),
                ("[Statues While You Wait]", 4, 156.24m, 2, 2, 159.24m, 78.12m),
                ("[Steve Makes Sandwiches]", 4, 176.24m, 2, 2, 179.24m, 88.12m),
                ("[United Cheese]", 4, 406.84m, 7, 7, 409.84m, 58.12m),
                ("[Disgusting Ltd]", 5, 156.14m, 2, 2, 159.14m, 78.07m),
                ("[Mats and More]", 5, 76.24m, 2, 2, 79.24m, 38.12m),
            ]);
    }

    [Fact]
    public void BitFilters()
    {
        var queryJson = new QueryJson
        {
            Select = ["Vendor.VendorName"],
            Filters =
            [
                new()
                {
                    Column = "Invoice.VendorId",
                    Operator = "BITS IN",
                    Constant = 1 | 2,
                    Value = new[] { 0, 2 }
                },
                new()
                {
                    Column = "Invoice.VendorId",
                    Operator = "BITS IN",
                    Constant = 4 | 8,
                    Value = new[] { 0, 4 }
                }
            ]
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => x.Selected[0]);

        records.Should().BeEquivalentTo(
            [
                "[Manchesterford Supplies Inc]", 
                "[United Cheese]",
                "[Uranium 4 Less]",
            ]);
    }

    [Fact]
    public void SqlAndDapperWithListFilter()
    {
        var queryJson = new QueryJson
        {
            Select =
            [
                "Invoice.VendorId",
                "Invoice.DepartmentId",
            ],
            Aggregations =
            [
                new() { Column = "Vendor.VendorName" }
            ],
            Filters =
            [
                new()
                {
                    Column = "Invoice.Id",
                    Operator = "IN",
                    Value = JsonSerializer.Deserialize<object>("[2,4,6,8]"),
                }
            ]
        };

        var results = ExecuteQuery(queryJson);
        var records = results.Records.Select(x => (x.Selected[0], x.Selected[1], x.Aggregated[0]));

        records.Should().BeEquivalentTo(
            [
                (8, 12, 1),
                (8, 8, 1),
                (18, 6, 1),
                (8, 6, 1),
            ]);
    }
}
