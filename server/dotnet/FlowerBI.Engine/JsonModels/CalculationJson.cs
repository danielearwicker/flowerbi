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

            var second = Operator == "/" 
                ? sql.Conditional($"{Second.ToSql(sql)} = 0", sql.CastToFloat(Second.ToSql(sql)), "0")
                : Second.ToSql(sql);

            return $"{First.ToSql(sql)} {Operator} {second}";
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
