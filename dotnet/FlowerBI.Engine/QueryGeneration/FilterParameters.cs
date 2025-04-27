using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace FlowerBI;

public interface IFilterParameters
{
    string this[Filter filter] { get; }
}

public class DictionaryFilterParameters(string prefix) : IFilterParameters
{
    public Dictionary<Filter, string> Names { get; } = [];

    public Dictionary<string, object> Values { get; } = [];

    public string this[Filter filter]
    {
        get
        {
            if (!Names.TryGetValue(filter, out var name))
            {
                Names[filter] = name = $"{prefix}filter{Names.Count}";
                Values.Add(name, filter.Value);
            }

            return name;
        }
    }
}

public class NullFilterParameters : IFilterParameters
{
    public string this[Filter filter] => string.Empty;

    public static readonly IFilterParameters Singleton = new NullFilterParameters();
}

public class EmbeddedFilterParameters : IFilterParameters
{
    public string this[Filter filter] =>
        filter.Value is string str ? $"'{str}'"
        : filter.Value is DateTime dt ? $"'{dt:yyyy-MM-dd}'"
        : filter.Value is true ? "1"
        : filter.Value is false ? "0"
        : $"{filter.Value}";
}

public static class SafeEmbeddedFilterParameters
{
    public static SafeEmbeddedFilterParameters<I> WithEmbedded<I>(this I inner)
        where I : IFilterParameters => new(inner);
}

public class SafeEmbeddedFilterParameters<I>(I inner) : IFilterParameters
    where I : IFilterParameters
{
    public I Inner => inner;

    public override string ToString() => inner.ToString();

    private static bool TryGetSafeLiteral(object val, out string literal)
    {
        if (val is int or long or short or decimal or float or double)
        {
            literal = $"{val}";
            return true;
        }

        if (val is bool b)
        {
            literal = b ? "1" : "0";
            return true;
        }

        if (val is IEnumerable<object> seq)
        {
            var parts = new List<string>();
            foreach (var item in seq)
            {
                if (TryGetSafeLiteral(item, out var part))
                {
                    parts.Add(part);
                }
                else
                {
                    literal = string.Empty;
                    return false;
                }
            }

            literal = $"({string.Join(", ", parts)})";
            return true;
        }

        literal = string.Empty;
        return false;
    }

    public string this[Filter filter]
    {
        get
        {
            // Numeric and boolean values (and lists of) are not a SQL injection risk,
            // while embedding them as constants can help the DB query planner match to
            // filtered or indexed views etc.
            if (TryGetSafeLiteral(filter.Value, out var literal))
            {
                return literal;
            }

            return inner[filter];
        }
    }
}

public class DapperFilterParameters(string prefix = "@") : IFilterParameters
{
    public DynamicParameters DapperParams { get; } = new DynamicParameters();

    private readonly Dictionary<Filter, string> _names = [];

    private static string FormatValue(object val) =>
        val is IEnumerable<object> list ? string.Join(", ", list) : val?.ToString();

    public override string ToString() =>
        string.Join(", ", _names.Select(x => $"{x.Value} = {FormatValue(x.Key.Value)}"));

    public string this[Filter filter]
    {
        get
        {
            if (!_names.TryGetValue(filter, out var name))
            {
                var plainName = $"filter{_names.Count}";
                _names[filter] = name = $"{prefix}{plainName}";
                DapperParams.Add(plainName, filter.Value);
            }

            return name;
        }
    }
}
