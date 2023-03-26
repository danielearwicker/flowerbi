using System.Collections.Generic;
using System.Linq;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI
{
    public class Ordering
    {
        public bool Descending { get; set; }

        public LabelledColumn Column { get; }

        public Ordering(LabelledColumn column, bool descending)
        {
            Column = column;
            Descending = descending;
        }

        public Ordering(IColumn column, bool descending)
            : this(new LabelledColumn(null, column), descending) {}

        public string Direction => Descending? "desc" : "asc";

        public Ordering(OrderingJson json, Schema schema)
            : this(schema.GetColumn(json.Column), json.Descending) { }

        public static IList<Ordering> Load(IEnumerable<OrderingJson> orderings, Schema schema)
            => orderings?.Select(x => new Ordering(x, schema)).ToList()
                ?? new List<Ordering>();
    }
}
