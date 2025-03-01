using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowerBI.Engine.JsonModels;
using FluentAssertions;
using Parquet.Serialization;
using Xunit;

#if NET8_0_OR_GREATER
namespace FlowerBI.Engine.Tests;

public class ParquetTests
{
    public static readonly Schema Schema = new(typeof(ParquetTestSchema));

    private readonly List<string> _log = [];

    public class TestRecord
    {
        public double Amount { get; set; }
        public string Product { get; set; }
    }

    private static readonly TestRecord[] _testRec =
    [
        new TestRecord { Amount = 1, Product = "A" },
        new TestRecord { Amount = 2, Product = "B" },
        new TestRecord { Amount = 3, Product = "C" },
        new TestRecord { Amount = 4, Product = "B" },
        new TestRecord { Amount = 5, Product = "C" },
    ];

    private async Task<QueryResultJson> TestQuery(QueryJson query)
    {
        var parquet = new MemoryStream();
        await ParquetSerializer.SerializeAsync(_testRec, parquet);
        parquet.Position = 0;

        return await new Query(query, Schema).QueryParquet(parquet);
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
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["B"], Aggregated = [2] },
                        new() { Selected = ["C"], Aggregated = [2] },
                    ],
                }
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
                        new() { Selected = ["A"], Aggregated = [1] },
                        new() { Selected = ["B"], Aggregated = [6] },
                        new() { Selected = ["C"], Aggregated = [8] },
                    ],
                }
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
}
#endif
