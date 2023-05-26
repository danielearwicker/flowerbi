using System.Collections.Generic;

namespace FlowerBI.Engine.JsonModels
{
    public class QueryJson
    {
        public IList<string> Select { get; set; }

        public IList<AggregationJson> Aggregations { get; set; }

        public IList<FilterJson> Filters { get; set; }

        public IList<OrderingJson> OrderBy { get; set; }

        public IList<CalculationJson> Calculations { get; set; }

        public bool? Totals { get; set; }

        public long? Skip { get; set; }

        public int? Take { get; set; }

        public string Comment { get; set; }

        public bool? AllowDuplicates { get; set; }

        public bool? FullJoins { get; set; }
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