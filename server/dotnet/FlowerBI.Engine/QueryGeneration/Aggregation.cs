using System.Collections.Generic;
using System.Linq;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI;

public enum AggregationType
{
    Count,
    Sum,
    Avg,
    Min,
    Max,
    CountDistinct,
};

public class Aggregation
{
    public AggregationType Function { get; }

    public LabelledColumn Column { get; }

    public IEnumerable<Filter> Filters { get; }

    public Aggregation(AggregationJson json, Schema schema)
    {
        Function = json.Function;
        Column = schema.GetColumn(json.Column);
        Filters = Filter.Load(json.Filters, schema);
    }

    public Aggregation()
    {
        Filters = Enumerable.Empty<Filter>();
    }

    public object Convert(object objVal)
        => (objVal?.GetType()?.IsAssignableTo(Column.Value.ClrType.GetType()) ?? false) 
            ? Column.Value.ConvertValue(objVal) 
            : objVal;

    public static IList<Aggregation> Load(IEnumerable<AggregationJson> aggs, Schema schema)
        => aggs?.Select(x => new Aggregation(x, schema)).ToList() ?? [];
}