using System;
using System.Collections.Generic;
using System.Linq;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI
{
    public class Ordering
    {
        public bool Descending { get; }

        public LabelledColumn Column { get; }

        public int Index { get; }

        public Ordering(LabelledColumn column, bool descending, int index)
        {
            Column = column;
            Descending = descending;
            Index = index;
        }

        public Ordering(IColumn column, bool descending, int index)
            : this(new LabelledColumn(null, column), descending, index) {}

        public string Direction => Descending? "desc" : "asc";

        public Ordering(OrderingJson json, Schema schema, int selects = 0, int values = 0, int calcs = 0)
            : this(
                json.Column == null ? null : schema.GetColumn(json.Column),
                json.Descending,
                json.Index == null || json.Type == null ? 0 :
                    json.Type == OrderingType.Select && json.Index < selects ? json.Index.Value :
                    json.Type == OrderingType.Value && json.Index < values ? json.Index.Value + selects :
                    json.Type == OrderingType.Calculation && json.Index < calcs ? json.Index.Value + selects + values :                    
                    throw new ArgumentOutOfRangeException("index", $"Ordering index {json.Index} is out of range in {json.Type}")        
            ) { }

        public static IList<Ordering> Load(IEnumerable<OrderingJson> orderings, Schema schema, 
                                           int selects = 0, int values = 0, int calcs = 0)
            => orderings?.Select(x => new Ordering(x, schema, selects, values, calcs)).ToList()
                ?? new List<Ordering>();
    }
}
