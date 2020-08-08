using System.Collections.Generic;

namespace TinyBI.Engine.JsonModels
{
    public class QueryJson
    {
        public IList<string> Select { get; set; }

        public IList<AggregationJson> Aggregations { get; set; }

        public IList<FilterJson> Filters { get; set; }

        public IList<OrderingJson> OrderBy { get; set; }

        public bool Totals { get; set; }
    }

    public class QueryRecordJson
    {
        public IList<object> Selected { get; set; }

        public IList<object> Aggregated { get; set; }
    }

    public class QueryResultJson
    {
        public IList<QueryRecordJson> Records { get; set; }

        public QueryRecordJson Totals { get; set; }
    }
}
