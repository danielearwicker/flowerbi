using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FlowerBI.Engine.JsonModels;
using Dapper;
using HandlebarsDotNet;
using System.Text.RegularExpressions;

namespace FlowerBI
{
    public class Query
    {
        public IList<LabelledColumn> Select { get; }
        public IList<Aggregation> Aggregations { get; }
        public IList<Filter> Filters { get; }
        public IList<Ordering> OrderBy { get; }
        public IList<CalculationJson> Calculations { get; }

        public bool Totals { get; }

        public long Skip { get; }

        public int Take { get; }

        public string Comment { get; }

        public bool AllowDuplicates { get; }

        public bool FullJoins { get; }

        public int CommandTimeoutSeconds { get; } = 30;

        public Query(QueryJson json, Schema schema)
        {
            Select = schema.Load(json.Select);
            Aggregations = Aggregation.Load(json.Aggregations, schema);
            Filters = Filter.Load(json.Filters, schema);
            OrderBy = Ordering.Load(json.OrderBy, schema,
                                    json.Select?.Count ?? 0,
                                    json.Aggregations?.Count ?? 0,
                                    json.Calculations?.Count ?? 0);
            Calculations = json.Calculations ?? new List<CalculationJson>();
            Totals = json.Totals ?? false;
            Skip = json.Skip ?? 0;
            Take = json.Take ?? 100;
            Comment = json.Comment;
            AllowDuplicates = json.AllowDuplicates ?? false;
            FullJoins = json.FullJoins ?? false;
        }

        private static readonly HandlebarsTemplate<object, string> _aggregatedTemplate = Handlebars.Compile(@"

with {{#each Aggregations}}

    Aggregation{{@index}} as (
        {{{this}}}
    )
    {{#unless @last}},{{/unless}}

{{/each}}

{{#if fullJoins}}
    
    , UnionedValues as (

    {{#each unionedValues}}
        select 
        {{#each Columns}}
            {{#unless @first}}, {{/unless}}
            {{this}}
        {{/each}}
        from {{Source}}
        {{#unless @last}}union all{{/unless}}
    {{/each}}
    ),
    CombinedValues as (
        select
        {{#each Select}}
            Select{{@index}},
        {{/each}}
        {{#each Aggregations}}
            {{#unless @first}}, {{/unless}}
            max(Value{{@index}}) as Value{{@index}}
        {{/each}}
        from UnionedValues
        group by 
        {{#each Select}}
            {{#unless @first}}, {{/unless}}Select{{@index}}
        {{/each}}
    )
    select 
    {{#each Select}}
        Select{{@index}},
    {{/each}}
    {{#each Calculations}}
        {{{this}}} as Value{{@index}}
        {{#unless @last}},{{/unless}}
    {{/each}}
    from CombinedValues
{{/if}}

{{#unless fullJoins}}
select 
    {{#each Select}}
        a0.Select{{@index}},
    {{/each}}

    {{#each Calculations}}
        {{{this}}} Value{{@index}}
        {{#unless @last}},{{/unless}}
    {{/each}}

    from Aggregation0 a0

    {{#each Aggregations}}
        {{#unless @first}}
            {{#if ../Select}}
                left join Aggregation{{@index}} a{{@index}} on
                {{#each ../Select}}
                    a{{@../index}}.Select{{@index}} = a0.Select{{@index}}
                    {{#unless @last}}and{{/unless}}
                {{/each}}
            {{/if}}
            {{#unless ../Select}}
                cross join Aggregation{{@index}} a{{@index}}
            {{/unless}}
        {{/unless}}
    {{/each}}
{{/unless}}

{{#if orderBy}}
    order by {{orderBy}}
{{/if}}

{{skipAndTake}}
");

        private string ToSql(ISqlFormatter sql, IList<LabelledColumn> select,
                             IFilterParameters filterParams, IEnumerable<Filter> outerFilters,
                             long skip, int take, bool totals)
        {
            if (Aggregations.Count == 1)
            {
                return Aggregations[0].ToSql(sql, select, outerFilters.Concat(Filters), filterParams, OrderBy, skip, take, AllowDuplicates, totals);
            }

            var fullJoins = FullJoins && select != null && select.Count > 0 && Aggregations.Count > 1 ? "full" : null;

            string FetchAggValue(int i) => fullJoins == null ? $"a{i}.Value0" : $"Value{i}";

            var ordering = (Aggregations?.Count ?? 0) > 0 ? $"{(select?.Count ?? 0) + 1} desc" : "1 asc";

            if (select != null && OrderBy.Count != 0)
            {
                ordering = string.Join(", ", OrderBy.Select(x => FindIndexedOrderingColumn(x) ?? FindNamedSelectOrderingColumn(x, Select)));
            }

            return _aggregatedTemplate(new
            {
                skipAndTake = totals ? null : sql.SkipAndTake(skip, take),
                Aggregations = Aggregations.Select(x =>
                    x.ToSql(sql, select, outerFilters.Concat(Filters), filterParams)).ToList(),
                Calculations = Aggregations.Select((_, i) => FetchAggValue(i))
                    .Concat(Calculations.Select(x => x.ToSql(sql, FetchAggValue))),
                Select = select,
                orderBy = totals ? null : ordering,
                fullJoins,
                unionedValues = Aggregations.Select((_, aRow) => new
                {
                    Source = $"Aggregation{aRow}",
                    Columns = select.Select((_, s) => $"Select{s}")
                                    .Concat(Aggregations
                                            .Select((_, aCol) => (Source: aRow == aCol ? "Value0" : "null", Target: $"Value{aCol}"))
                                            .Select(x => $"{x.Source} as {x.Target}"))
                })
            });
        }

        public static string FindIndexedOrderingColumn(Ordering ordering)
            => ordering.Column == null ? $"{ordering.Index + 1} {ordering.Direction}" : null;

        public static string FindNamedSelectOrderingColumn(Ordering ordering, IEnumerable<LabelledColumn> selects)
        {
            var found = selects.Select((c, n) => (c, n)).FirstOrDefault(x => x.c == ordering.Column);
            if (found.c == null)
            {
                throw new InvalidOperationException(
                    $"Cannot order by {ordering.Column} as it has not been selected");
            }

            return $"a0.Select{found.n} {ordering.Direction}";
        }

        private static readonly Regex SanitiseCommentPattern = new("[^\\w\\d\\r\\n]+");

        public string ToSql(ISqlFormatter sql,
                            IFilterParameters filterParams,
                            IEnumerable<Filter> outerFilters)
        {
            var result = ToSql(sql, Select, filterParams, outerFilters, Skip, Take, false);

            if (Totals)
            {
                result += ";" + ToSql(sql, null, filterParams, outerFilters, 0, 1, true);
            }

            if (!string.IsNullOrWhiteSpace(Comment))
            {
                var stripped = SanitiseCommentPattern.Replace(Comment, " ").Trim();
                result = $"/* {stripped} */ \r\n{result}";
            }

            return result;
        }

        public QueryResultJson Run(ISqlFormatter sql, IDbConnection db, Action<string> log, params Filter[] outerFilters)
        {
            var filterParams = new DapperFilterParameters();

            var querySql = ToSql(sql, filterParams, outerFilters);

            log?.Invoke($"{querySql} with parameters: {filterParams}");

            var reader = db.QueryMultiple(querySql, filterParams.DapperParams, commandTimeout: CommandTimeoutSeconds);

            var result = new QueryResultJson();

            result.Records = ConvertRecords(reader.Read<dynamic>()
                                .Cast<IDictionary<string, object>>());
            if (Totals)
            {
                result.Totals = ConvertRecords(reader.Read<dynamic>()
                                .Cast<IDictionary<string, object>>())
                                .Single();
            }

            return result;
        }

        private IList<QueryRecordJson> ConvertRecords(IEnumerable<IDictionary<string, object>> list)
        {
            var nullConvert = new Func<object, object>(x => x);

            var aggColumns = Aggregations.Select(x => new Func<object, object>(x.Convert))
                        .Concat(Calculations.Select(x => nullConvert)).ToList();

            var selColumns = Select.Select(x => new Func<object, object>(x.Value.ConvertValue)).ToList();

            return list.Select(x => new QueryRecordJson
            {
                Selected = GetList(x, "Select", selColumns),
                Aggregated = GetList(x, "Value", aggColumns),
            })
                .ToList();
        }

        private static IList<object> GetList(IDictionary<string, object> raw, string prefix, IReadOnlyList<Func<object, object>> converters)
        {
            IList<object> result = null;

            for (var n = 0; n < 100; n++)
            {
                if (!raw.TryGetValue($"{prefix}{n}", out var value)) break;
                if (result == null) result = new List<object>();
                result.Add(converters[n](value));
            }

            return result;
        }
    }
}