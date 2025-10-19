#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlowerBI.Engine.JsonModels;
using FluentAssertions;
using Parquet.Serialization;
using Xunit;

namespace FlowerBI.Engine.Tests;

public class ParquetTests
{
    public static readonly Schema Schema = ExecutionTests.FindSchema("parquetTestSchema");

    public class TestRecord
    {
        public double Amount { get; set; }
        public string Product { get; set; }
        public DateTime Purchased { get; set; }
    }

    private static readonly TestRecord[] _testRec =
    [
        new TestRecord
        {
            Amount = 1,
            Product = "A",
            Purchased = new(2024, 10, 8),
        },
        new TestRecord
        {
            Amount = 2,
            Product = "B",
            Purchased = new(2025, 01, 8),
        },
        new TestRecord
        {
            Amount = 3,
            Product = "C",
            Purchased = new(2025, 04, 17),
        },
        new TestRecord
        {
            Amount = 4,
            Product = "B",
            Purchased = new(2025, 04, 2),
        },
        new TestRecord
        {
            Amount = 5,
            Product = "C",
            Purchased = new(2025, 10, 16),
        },
    ];

    private static readonly Dictionary<IColumn, Func<object, object>> _dimensions = new (
        IColumn Column,
        Func<DateTime, object> Get
    )[]
    {
        (Schema.GetColumn("Date.DayOfMonth").Value, x => x.Day),
        (Schema.GetColumn("Date.Month").Value, x => x.Month),
        (Schema.GetColumn("Date.Year").Value, x => x.Year),
        (Schema.GetColumn("Date.MonthName").Value, x => x.ToString("MMMM")),
        (Schema.GetColumn("Date.StartOfMonth").Value, x => new DateTime(x.Year, x.Month, 1)),
    }.ToDictionary(x => x.Column, x => new Func<object, object>(v => x.Get((DateTime)v)));

    private static async Task<QueryResultJson> TestQuery(QueryJson query)
    {
        var parquet = new MemoryStream();
        await ParquetSerializer.SerializeAsync(_testRec, parquet);
        parquet.Position = 0;

        return await new Query(query, Schema).QueryParquet(
            Schema.GetTable("Business"),
            parquet,
            _dimensions
        );
    }

    [Fact]
    public async Task SimpleGroupingCount()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Count },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [5] },
                    Records =
                    [
                        new() { Selected = ["B"], Aggregated = [2] },
                        new() { Selected = ["C"], Aggregated = [2] },
                        new() { Selected = ["A"], Aggregated = [1] },
                    ],
                },
                o => o.WithStrictOrdering()
            );
    }

    [Fact]
    public async Task SimpleGroupingSum()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [15] },
                    Records =
                    [
                        new() { Selected = ["C"], Aggregated = [8] },
                        new() { Selected = ["B"], Aggregated = [6] },
                        new() { Selected = ["A"], Aggregated = [1] },
                    ],
                },
                o => o.WithStrictOrdering()
            );
    }

    [Fact]
    public async Task SimpleGroupingMin()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Min },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [1] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["B"], Aggregated = [2] },
                        new() { Selected = ["C"], Aggregated = [3] },
                    ],
                }
            );
    }

    [Fact]
    public async Task SimpleGroupingMax()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Max },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [5] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["B"], Aggregated = [4] },
                        new() { Selected = ["C"], Aggregated = [5] },
                    ],
                }
            );
    }

    [Fact]
    public async Task SimpleGroupingAverage()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Avg },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [3] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["B"], Aggregated = [3] },
                        new() { Selected = ["C"], Aggregated = [4] },
                    ],
                }
            );
    }

    [Fact]
    public async Task MultipleAggregations()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Count },
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                    new() { Column = "Business.Amount", Function = AggregationType.Min },
                    new() { Column = "Business.Amount", Function = AggregationType.Max },
                    new() { Column = "Business.Amount", Function = AggregationType.Avg },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [5, 15, 1, 5, 3] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1, 1, 1, 1, 1] },
                        new() { Selected = ["B"], Aggregated = [2, 6, 2, 4, 3] },
                        new() { Selected = ["C"], Aggregated = [2, 8, 3, 5, 4] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilterEquals()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Count },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = "=",
                        Value = 3.0,
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [1] },
                    Records = [new() { Selected = ["C"], Aggregated = [1] }],
                }
            );
    }

    [Fact]
    public async Task FilterNotEqual()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Count },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = "<>",
                        Value = 3.0,
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [4] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["B"], Aggregated = [2] },
                        new() { Selected = ["C"], Aggregated = [1] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilterIn()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Count },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = "IN",
                        Value = new[] { 1.0, 5.0 },
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [2] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["C"], Aggregated = [1] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilterNotIn()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Count },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = "NOT IN",
                        Value = new[] { 2.0, 3.0, 4.0 },
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [2] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["C"], Aggregated = [1] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilterGreaterThan()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = ">",
                        Value = 2.0,
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [12] },
                    Records =
                    [
                        new() { Selected = ["B"], Aggregated = [4] },
                        new() { Selected = ["C"], Aggregated = [8] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilterGreaterThanOrEqual()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = ">=",
                        Value = 3.0,
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [12] },
                    Records =
                    [
                        new() { Selected = ["B"], Aggregated = [4] },
                        new() { Selected = ["C"], Aggregated = [8] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilterLessThan()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = "<",
                        Value = 3.5,
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [6] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["B"], Aggregated = [2] },
                        new() { Selected = ["C"], Aggregated = [3] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilterLessThanOrEqual()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = "<=",
                        Value = 3.0,
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [6] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["B"], Aggregated = [2] },
                        new() { Selected = ["C"], Aggregated = [3] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilterBitsIn()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                ],
                Filters =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Operator = "BITS IN",
                        Constant = 3,
                        Value = 1,
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [6] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["C"], Aggregated = [5] },
                    ],
                }
            );
    }

    [Fact]
    public async Task FilteredAggregations()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Function = AggregationType.Sum,
                        Filters =
                        [
                            new()
                            {
                                Column = "Business.Amount",
                                Operator = "IN",
                                Value = new[] { 1.0, 2.0 },
                            },
                        ],
                    },
                    new()
                    {
                        Column = "Business.Amount",
                        Function = AggregationType.Sum,
                        Filters =
                        [
                            new()
                            {
                                Column = "Business.Amount",
                                Operator = "IN",
                                Value = new[] { 2.0, 3.0 },
                            },
                        ],
                    },
                    new()
                    {
                        Column = "Business.Amount",
                        Function = AggregationType.Sum,
                        Filters =
                        [
                            new()
                            {
                                Column = "Business.Amount",
                                Operator = "IN",
                                Value = new[] { 3.0, 4.0 },
                            },
                        ],
                    },
                    new()
                    {
                        Column = "Business.Amount",
                        Function = AggregationType.Sum,
                        Filters =
                        [
                            new()
                            {
                                Column = "Business.Amount",
                                Operator = "IN",
                                Value = new[] { 4.0, 5.0 },
                            },
                        ],
                    },
                    new()
                    {
                        Column = "Business.Amount",
                        Function = AggregationType.Sum,
                        Filters =
                        [
                            new()
                            {
                                Column = "Business.Amount",
                                Operator = "IN",
                                Value = new[] { 1.0, 5.0 },
                            },
                        ],
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [3, 5, 7, 9, 6] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1, null, null, null, 1] },
                        new() { Selected = ["B"], Aggregated = [2, 2, 4, 4, null] },
                        new() { Selected = ["C"], Aggregated = [null, 3, 3, 5, 5] },
                    ],
                }
            );
    }

    [Fact]
    public async Task DimensionGroupingCount_Year()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Date.Year"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Count },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [5] },
                    Records =
                    [
                        new() { Selected = [2024], Aggregated = [1] },
                        new() { Selected = [2025], Aggregated = [4] },
                    ],
                }
            );
    }

    [Fact]
    public async Task DimensionGroupingCount_MonthName()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Date.MonthName"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Count },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [5] },
                    Records =
                    [
                        new() { Selected = ["January"], Aggregated = [1] },
                        new() { Selected = ["April"], Aggregated = [2] },
                        new() { Selected = ["October"], Aggregated = [2] },
                    ],
                }
            );
    }

    [Fact]
    public async Task DimensionAggregations()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Date.DayOfMonth", Function = AggregationType.Sum },
                    new() { Column = "Date.Month", Function = AggregationType.Sum },
                    new() { Column = "Date.Year", Function = AggregationType.Sum },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [51, 29, 10124] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [8, 10, 2024] },
                        new() { Selected = ["B"], Aggregated = [10, 5, 4050] },
                        new() { Selected = ["C"], Aggregated = [33, 14, 4050] },
                    ],
                }
            );
    }

    [Fact]
    public async Task DimensionFilters()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new()
                    {
                        Column = "Business.Amount",
                        Function = AggregationType.Sum,
                        Filters =
                        [
                            new()
                            {
                                Column = "Date.Year",
                                Operator = "=",
                                Value = 2025,
                            },
                        ],
                    },
                    new()
                    {
                        Column = "Business.Amount",
                        Function = AggregationType.Sum,
                        Filters =
                        [
                            new()
                            {
                                Column = "Date.Month",
                                Operator = "=",
                                Value = 10,
                            },
                        ],
                    },
                    new()
                    {
                        Column = "Business.Amount",
                        Function = AggregationType.Sum,
                        Filters =
                        [
                            new()
                            {
                                Column = "Date.DayOfMonth",
                                Operator = ">",
                                Value = 15,
                            },
                        ],
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [14.0, 6.0, 8.0] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [null, 1.0, null] },
                        new() { Selected = ["B"], Aggregated = [6.0, null, null] },
                        new() { Selected = ["C"], Aggregated = [8.0, 5.0, 8.0] },
                    ],
                }
            );
    }

    [Fact]
    public async Task Calculations()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product"],
                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                ],
                Calculations =
                [
                    new() { Value = 2 },
                    new() { Aggregation = 0 },
                    new()
                    {
                        First = new CalculationJson { Aggregation = 0 },
                        Operator = "+",
                        Second = new CalculationJson { Value = 2 },
                    },
                    new()
                    {
                        First = new CalculationJson
                        {
                            First = new CalculationJson { Aggregation = 0 },
                            Operator = "+",
                            Second = new CalculationJson { Value = 2 },
                        },
                        Operator = "*",
                        Second = new CalculationJson { Value = 3 },
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null], Aggregated = [15, 2, 15, 17, 51] },
                    Records =
                    [
                        new() { Selected = ["A"], Aggregated = [1, 2, 1, 3, 9] },
                        new() { Selected = ["B"], Aggregated = [6, 2, 6, 8, 24] },
                        new() { Selected = ["C"], Aggregated = [8, 2, 8, 10, 30] },
                    ],
                }
            );
    }

    [Fact]
    public async Task OrderByMixed()
    {
        var results = await TestQuery(
            new QueryJson
            {
                Select = ["Business.Product", "Date.MonthName"],

                Aggregations =
                [
                    new() { Column = "Business.Amount", Function = AggregationType.Sum },
                ],

                OrderBy =
                [
                    new()
                    {
                        Type = OrderingType.Select,
                        Descending = false,
                        Index = 1,
                    },
                    new()
                    {
                        Type = OrderingType.Value,
                        Descending = false,
                        Index = 0,
                    },
                ],
            }
        );

        results
            .Should()
            .BeEquivalentTo(
                new QueryResultJson
                {
                    Totals = new() { Selected = [null, null], Aggregated = [15] },
                    Records =
                    [
                        new() { Selected = ["C", "April"], Aggregated = [3.0] },
                        new() { Selected = ["B", "April"], Aggregated = [4.0] },
                        new() { Selected = ["B", "January"], Aggregated = [2.0] },
                        new() { Selected = ["A", "October"], Aggregated = [1.0] },
                        new() { Selected = ["C", "October"], Aggregated = [5.0] },
                    ],
                }
            );
    }
}
#endif
