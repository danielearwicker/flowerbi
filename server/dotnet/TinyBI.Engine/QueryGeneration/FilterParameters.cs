using System;
using System.Collections.Generic;
using Dapper;

namespace TinyBI
{
    public interface IFilterParameters
    {
        string this[Filter filter] { get; }
    }

    public class DictionaryFilterParameters : IFilterParameters
    {
        private readonly Dictionary<Filter, string> _names = new Dictionary<Filter, string>();

        public Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

        public string this[Filter filter]
        {
            get
            {
                if (!_names.TryGetValue(filter, out var name))
                {
                    _names[filter] = name = $":filter{_names.Count}";
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

                return name;
            }
        }
    }
}
