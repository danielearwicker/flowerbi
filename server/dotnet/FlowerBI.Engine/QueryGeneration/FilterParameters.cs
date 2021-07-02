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
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        override public string ToString() =>
            string.Join(", ", _values.Select(x => $"{x.Key} = {x.Value}"));

        private string DefinePrimitive(object value, int index)
        {
            var plainName = $"filter{_names.Count}_{index}";
            DapperParams.Add(plainName, value);
            _values[plainName] = value;
            return $"@{plainName}";
        }

        public string this[Filter filter]
        {
            get
            {
                if (!_names.TryGetValue(filter, out var name))
                {
                    _names[filter] = name = filter.Operator == "IN"
                        ? "(" + string.Join(", ", ((IEnumerable<object>)filter.Value).Select(DefinePrimitive)) + ")"
                        : DefinePrimitive(filter.Value, 0);
                }

                return name;
            }
        }
    }
}