using System;
using System.Collections.Generic;
using System.Linq;
using FlowerBI.Engine.JsonModels;
using HandlebarsDotNet;

namespace FlowerBI
{
    public enum AggregationType
    {
        Count,
        Sum,
        Avg,
        Min,
        Max
    };

    public class Aggregation
    {
        public AggregationType Function { get; }

        public IColumn Column { get; }

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

        public static IList<Aggregation> Load(IEnumerable<AggregationJson> aggs, Schema schema)
        {
            var list = aggs?.Select(x => new Aggregation(x, schema)).ToList();
            return list?.Count > 0 ? list : new List<Aggregation> { new Aggregation() };
        }

        private static readonly HandlebarsTemplate<object, string> _template = Handlebars.Compile(@"
select

    {{#each selects}}
        {{this}}{{#unless @last}}, {{/unless}}
    {{/each}}

    {{#if aggColumn}}
        {{aggColumn}} Value0
    {{/if}}

{{joins}}

{{#if filters}}
where
    {{#each filters}}
        {{Column}} {{{Operator}}} {{Param}}
        {{#unless @last}} and {{/unless}}
    {{/each}}
{{/if}}

{{#if groupBy}}
    group by
    {{#each groupBy}}
        {{Part}}
        {{#unless @last}} , {{/unless}}
    {{/each}}
{{/if}}

{{#if orderBy}}
    order by {{orderBy}}
{{/if}}

{{#if skipAndTake}}
    {{skipAndTake}}
{{/if}}
");

        public string ToSql(
            ISqlFormatter sql,
            IEnumerable<IColumn> selectColumns,
            IEnumerable<Filter> outerFilters,
            IFilterParameters filterParams,
            IEnumerable<Ordering> orderings = null,
            long? skip = null,
            int? take = null)
        {
            var joins = new Joins();

            var selects = selectColumns?.Select((c, i) =>
                $"{sql.IdentifierPair(joins[c.Table], c.DbName)} Select{i}").ToList()
                ?? new List<string>();

            var aggColumn = Column != null ? $"{Function}({joins.Aliased(Column, sql)})" : null;

            if (aggColumn != null)
            {
                selects.Add($"{aggColumn} Value0");
            }

            if (selects.Count == 0)
            {
                throw new InvalidOperationException("Must select something");
            }

            var filters = outerFilters.Concat(Filters).Select(f => new
            {
                Column = joins.Aliased(f.Column, sql),
                f.Operator,
                Param = filterParams[f]
            })
            .ToList();

            var groupBy = selectColumns?.Select(x => new
            {
                Part = joins.Aliased(x, sql)
            })
            .ToList();

            var skipAndTake = skip != null && take != null
                    ? sql.SkipAndTake(skip.Value, take.Value)
                    : null;

            var orderBy = skipAndTake == null ? null :
                orderings.Any() ? string.Join(", ", orderings.Select(x => $"{joins.Aliased(x.Column, sql)} {x.Direction}")) :
                aggColumn != null ? $"{aggColumn} desc" :
                null;

            return _template(new
            {
                selects,
                filters,
                joins = joins.ToSql(sql),
                groupBy,
                orderBy,
                skipAndTake
            });
        }
    }
}