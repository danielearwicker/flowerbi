using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace FlowerBI
{
    public interface IFilterParameters
    {
        string this[Filter filter] { get; }
    }

    public class DictionaryFilterParameters : IFilterParameters
    {
        public Dictionary<Filter, string> Names { get; } = new Dictionary<Filter, string>();

        public Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

        public string this[Filter filter]
        {
            get
            {
                if (!Names.TryGetValue(filter, out var name))
                {
                    Names[filter] = name = $"@filter{Names.Count}";
                    Values.Add(name, filter.Value);
                }

                return name;
            }
        }
    }

    public class EmbeddedFilterParameters : IFilterParameters
    {
        public string this[Filter filter] =>
            filter.Value is string str ? $"'{str}'" :
            filter.Value is DateTime dt ? $"'{dt:yyyy-MM-dd}'" :
            filter.Value is true ? "1" :
            filter.Value is false ? "0" :
            $"{filter.Value}";
    }

    public class DapperFilterParameters : IFilterParameters
    {
        public DynamicParameters DapperParams { get; } = new DynamicParameters();

        private readonly Dictionary<Filter, string> _names = new Dictionary<Filter, string>();

        private static string FormatValue(object val)
            => val is IEnumerable<object> list ? string.Join(", ", list) : val?.ToString();

        override public string ToString() =>
            string.Join(", ", _names.Select(x => $"{x.Value} = {FormatValue(x.Key.Value)}"));

        public string this[Filter filter]
        {
            get
            {
                if (!_names.TryGetValue(filter, out var name))
                {
                    var plainName = $"filter{_names.Count}";
                    _names[filter] = name = $"@{plainName}";
                    DapperParams.Add(plainName, filter.Value);
                }

                return filter.Operator == "IN" ? $"({name})" : name;
            }
        }
    }
}