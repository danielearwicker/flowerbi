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

            string ValueOrZero(CalculationJson Node)
            {
                var expr = Node.ToSql(sql);
                return sql.Conditional($"{expr} is null", "0", expr);
            }

            var firstExpr = ValueOrZero(First);
            var secondExpr = ValueOrZero(Second);

            return Operator == "/"
                ? sql.Conditional($"{secondExpr} = 0", "0", $"{firstExpr} / {sql.CastToFloat(secondExpr)}")
                : $"({firstExpr} {Operator} {secondExpr})";
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
