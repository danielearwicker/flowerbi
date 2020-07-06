using System.Collections.Generic;
using Dapper;

namespace TinyBI
{
    public class FilterParameters
    {
        public DynamicParameters DapperParams { get; } = new DynamicParameters();

        public Dictionary<Filter, string> Names { get; } = new Dictionary<Filter, string>();

        public string this[Filter filter]
        {
            get
            {
                if (!Names.TryGetValue(filter, out var name))
                {
                    Names[filter] = name = $"filter{Names.Count}";
                    DapperParams.Add(name, filter.Value);
                }

                return name;
            }
        }
    }
}
