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

    private static readonly ISet<string> _allowedOperators 
        = new[] { "+", "-", "*", "/" }.ToHashSet();

    public string ToSql(ISqlFormatter sql)
    {
        if (Value != null)
        {
            RequireNulls(Aggregation, First, Second, Operator);
            return $"{Value}";
        }

        if (Aggregation != null)
        {
            RequireNulls(Value, First, Second, Operator);
            return $"a{Aggregation}.Value0";
        }

        if (First != null && Second != null && Operator != null)
        {
            RequireNulls(Aggregation, Value);

            if (!_allowedOperators.Contains(Operator))
            {
                throw new InvalidOperationException($"Operator '{Operator}' not supported");
            }

            return Operator == "/"
                ? sql.Conditional($"{Second.ToSql(sql)} = 0", "0", $"{First.ToSql(sql)} / {sql.CastToFloat(Second.ToSql(sql))}")
                : $"({First.ToSql(sql)} {Operator} {Second.ToSql(sql)})";
        }

        throw new InvalidOperationException("Calculation does not specify enough properties");
    }

    public void RequireNulls(params object[] nulls)
    {
        if (nulls.Any(x => x != null))
        {
            throw new InvalidOperationException("Calculation has too many properties in same object");
        }
    }
}
