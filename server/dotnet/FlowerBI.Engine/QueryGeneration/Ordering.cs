using System.Collections.Generic;
using System.Linq;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI
{
    public class Ordering
    {
        public bool Descending { get; set; }

        public IColumn Column { get; }

        public Ordering(IColumn column, bool descending)
        {
            Column = column;
            Descending = descending;
        }

        public string Direction => Descending? "desc" : "asc";

        public Ordering(OrderingJson json, Schema schema)
            : this(schema.GetColumn(json.Column), json.Descending) { }

        public static IList<Ordering> Load(IEnumerable<OrderingJson> orderings, Schema schema)
            => orderings?.Select(x => new Ordering(x, schema)).ToList()
                ?? new List<Ordering>();
    }
}
