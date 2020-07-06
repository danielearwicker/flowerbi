using System.Collections.Generic;

namespace TinyBI.Engine.JsonModels
{
    public class QueryJson
    {
        public IList<string> Select { get; set; }

        public IList<AggregationJson> Aggregations { get; set; }

        public IList<FilterJson> Filters { get; set; }

        public bool Totals { get; set; }
    }
}
