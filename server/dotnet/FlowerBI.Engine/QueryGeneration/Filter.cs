using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI;

public class Filter
{
    public Filter(LabelledColumn column, string op, object val, object constant)
    {
        Column = column;
        Operator = CheckOperator(op);
        (Value, IncludesNull) = SeparateNull(val);
        Constant = constant;

        if (IncludesNull && !_nullableOperators.Contains(Operator))
        {
            throw new FlowerBIException($"{Operator} filter does not support null values");
        }
    }

    public LabelledColumn Column { get; }

    public string Operator { get; }

    /// <summary>
    /// The non-null part of the filter value: nulls are removed from a list, and a filter
    /// whose only value is null has a null Value. See <see cref="IncludesNull"/>.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// True if the filter value was null, or a list containing null. Such a filter is
    /// rendered with IS NULL / IS NOT NULL, because in SQL a comparison with NULL is
    /// never true.
    /// </summary>
    public bool IncludesNull { get; }

    public object Constant { get; }

    public static IList<Filter> Load(IEnumerable<FilterJson> filters, Schema schema) =>
        filters?.Select(x => new Filter(x, schema)).ToList() ?? [];

    public Filter(IColumn column, string op, object val, object constant)
        : this(new LabelledColumn(null, column), op, val, constant) { }

    public Filter(FilterJson json, Schema schema)
        : this(
            schema.GetColumn(json.Column),
            json.Operator,
            UnpackAndValidateValue(json.Value),
            UnpackAndValidateValue(json.Constant)
        ) { }

    private static readonly HashSet<string> _nullableOperators = ["=", "<>", "!=", "IN", "NOT IN"];

    private static (object Value, bool IncludesNull) SeparateNull(object val)
    {
        if (val is null)
        {
            return (null, true);
        }

        if (val is string || val is not IEnumerable enumerable)
        {
            return (val, false);
        }

        var items = enumerable.Cast<object>().ToList();
        if (!items.Contains(null))
        {
            return (val, false);
        }

        var nonNull = items.Where(x => x is not null).ToList();
        return (nonNull.Count == 0 ? null : nonNull, true);
    }

    private static readonly HashSet<Type> _basicValueTypes =
    [
        typeof(bool),
        typeof(byte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(string),
        typeof(DateTime),
    ];

    private static void ValidateBasicType(object value)
    {
        var type = value.GetType();
        if (!_basicValueTypes.Contains(type))
        {
            throw new FlowerBIException("Unsupported filter value");
        }
    }

    private static object UnpackAndValidateValue(object json)
    {
        var unpacked = UnpackValue(json);
        if (unpacked is null)
            return null;
        if (_basicValueTypes.Contains(unpacked.GetType()))
            return unpacked;

        if (unpacked is IEnumerable enumerable)
        {
            var empty = true;

            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    ValidateBasicType(item);
                }

                empty = false;
            }

            if (empty)
            {
                // We don't silently strip out filters with empty lists of required values so
                // that filtering can be used for security controls (denying access to data).
                //
                // This has always been the case, but now it produces a descriptive error
                // instead of a SQL syntax error.
                throw new FlowerBIException("Filter JSON contains empty array");
            }

            return unpacked;
        }

        ValidateBasicType(unpacked);
        return unpacked;
    }

    private static object UnpackValue(object json)
    {
        if (json is JsonElement e)
        {
            return e.ValueKind == JsonValueKind.Null ? null
                : e.ValueKind == JsonValueKind.False ? false
                : e.ValueKind == JsonValueKind.True ? true
                : e.ValueKind == JsonValueKind.Number ? e.GetDouble()
                : e.ValueKind == JsonValueKind.String
                    ? (DateTime.TryParse(e.GetString(), out var dt) ? dt : e.GetString())
                : e.ValueKind == JsonValueKind.Array
                    ? e.EnumerateArray()
                        .Select(item =>
                            item.ValueKind == JsonValueKind.Null ? null
                            : item.ValueKind == JsonValueKind.False ? false
                            : item.ValueKind == JsonValueKind.True ? true
                            : item.ValueKind == JsonValueKind.Number ? (object)item.GetDouble()
                            : item.ValueKind == JsonValueKind.String ? item.GetString()
                            : new object()
                        )
                        .ToList()
                : new object();
        }

        if (json is IEnumerable<object> l)
        {
            return l.Select(UnpackNewtonsoft).ToList();
        }

        return json;
    }

    // Don't want to depend on a specific version, so...
    private static object UnpackNewtonsoft(object val)
    {
        if (val == null)
        {
            return null;
        }

        var type = val.GetType();
        if (
            type.IsPrimitive
            || type == typeof(string)
            || type == typeof(DateTime)
            || type == typeof(decimal)
        )
        {
            return val;
        }

        // Assume it's a Newtonsoft value wrapper (don't want to depend on a specific version)
        var d = (dynamic)val;
        return d.Value;
    }

    private static readonly HashSet<string> _allowedOperators = new HashSet<string>
    {
        "=",
        "<>",
        "!=",
        ">",
        "<",
        ">=",
        "<=",
        "IN",
        "NOT IN",
        "BITS IN",
        "LIKE",
    };

    private static string CheckOperator(string op)
    {
        if (!_allowedOperators.Contains(op))
        {
            throw new FlowerBIException($"{op} is not an allowed operator");
        }

        return op;
    }
}
