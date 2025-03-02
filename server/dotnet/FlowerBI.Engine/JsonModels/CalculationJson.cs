using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowerBI;

public class CalculationJson
{
    public decimal? Value { get; set; }

    public int? Aggregation { get; set; }

    public CalculationJson First { get; set; }

    public CalculationJson Second { get; set; }

    public string Operator { get; set; }

    private static readonly ISet<string> _allowedOperators = new[]
    {
        "+",
        "-",
        "*",
        "/",
        "??",
    }.ToHashSet();

    public string ToSql(ISqlFormatter sql, Func<int, string> fetchAggValue)
    {
        if (Value != null)
        {
            RequireNulls(Aggregation, First, Second, Operator);
            return $"{Value}";
        }

        if (Aggregation != null)
        {
            RequireNulls(Value, First, Second, Operator);
            return fetchAggValue(Aggregation.Value);
        }

        if (First != null && Second != null && Operator != null)
        {
            RequireNulls(Aggregation, Value);

            if (!_allowedOperators.Contains(Operator))
            {
                throw new FlowerBIException($"Operator '{Operator}' not supported");
            }

            var firstExpr = First.ToSql(sql, fetchAggValue);
            var secondExpr = Second.ToSql(sql, fetchAggValue);

            return Operator == "/"
                    ? sql.Conditional(
                        $"{secondExpr} = 0",
                        "0",
                        $"{firstExpr} / {sql.CastToFloat(secondExpr)}"
                    )
                : Operator == "??" ? sql.Conditional($"{firstExpr} is null", secondExpr, firstExpr)
                : $"({firstExpr} {Operator} {secondExpr})";
        }

        throw new FlowerBIException("Calculation does not specify enough properties");
    }

    public static void RequireNulls(params object[] nulls)
    {
        if (nulls.Any(x => x != null))
        {
            throw new FlowerBIException("Calculation has too many properties in same object");
        }
    }

    public Func<decimal?> Compile(Func<int, Func<decimal?>> aggregations)
    {
        if (Value != null)
        {
            RequireNulls(Aggregation, First, Second, Operator);
            var constant = Value.Value;
            return () => constant;
        }

        if (Aggregation != null)
        {
            RequireNulls(Value, First, Second, Operator);
            return aggregations(Aggregation.Value);
        }

        if (First != null && Second != null && Operator != null)
        {
            RequireNulls(Aggregation, Value);

            var firstExpr = First.Compile(aggregations);
            var secondExpr = Second.Compile(aggregations);

            return Operator switch
            {
                "+" => () => firstExpr() + secondExpr(),
                "-" => () => firstExpr() - secondExpr(),
                "*" => () => firstExpr() * secondExpr(),
                "/" => () => firstExpr() / secondExpr(),
                "??" => () => firstExpr() is null ? secondExpr() : firstExpr(),
                _ => throw new FlowerBIException($"Operator '{Operator}' not supported"),
            };
        }

        throw new FlowerBIException("Calculation does not specify enough properties");
    }
}
