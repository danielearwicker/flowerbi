using System.Collections.Generic;
using System.Linq;
using TinyBI.Engine.JsonModels;

namespace TinyBI
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

        public Ordering(OrderingJson json, Schema schema)
            : this(schema.GetColumn(json.Column), json.Descending) { }

        public static IList<Ordering> Load(IEnumerable<OrderingJson> orderings, Schema schema)
            => orderings?.Select(x => new Ordering(x, schema)).ToList()
                ?? new List<Ordering>();
    }
}
