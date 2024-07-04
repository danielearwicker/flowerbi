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

        private static readonly HandlebarsTemplate<object, string> _template = Handlebars.Compile(@"
select

    {{#each selects}}
        {{this}}{{#unless @last}}, {{/unless}}
    {{/each}}

{{joins}}

{{#if filters}}
where
    {{#each filters}}
        {{{FilterSql}}}
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

        // This needs to accept filters and use them inside CASE WHEN {filters} THEN {expr} END
        private static string FormatAggFunction(AggregationType func, string expr, IEnumerable<Filter> filters, Joins joins, ISqlFormatter sql, IFilterParameters filterParams)
        {
            if (filters.Any())
            {
                var when = string.Join(" and ", filters.Select(f => FormatFilter(f, joins, sql, filterParams)));
                expr = $"case when {when} then {expr} end";
            }

            return func == AggregationType.CountDistinct
                ? $"count(distinct {expr})"
                : $"{func}({expr})";
        }

        private static string FormatFilter(Filter f, Joins joins, ISqlFormatter sql, IFilterParameters filterParams)
        {
            var column = joins.Aliased(f.Column, sql);
            var param = filterParams[f];

            if (f.Operator == "BITS IN")
            {
                // constant must be provided and is treated as an integer bit mask
                var mask = f.Constant is int i ? i :
                           f.Constant is long l ? l :
                           f.Constant is double d ? (int)d :
                           throw new InvalidOperationException("BITS IN filter requires integer constant");

                return $"({column} & {mask}) in {param}";
            }

            return $"{column} {f.Operator} {param}";
        }

        public string ToSql(
            ISqlFormatter sql,
            IFilterParameters filterParams,
            IEnumerable<Filter> outerFilters,
            bool totals)
        {
            var joins = new Joins();

            var selects = (totals ? null : Select)?.Select((c, i) =>
                $"{sql.IdentifierPair(joins.GetAlias(c.Value.Table, c.JoinLabel), c.Value.DbName)} Select{i}").ToList()
                ?? [];

            var aggs = Aggregations?.Select((a, i) =>
                $"{FormatAggFunction(a.Function, joins.Aliased(a.Column, sql), a.Filters, joins, sql, filterParams)} Value{i}").ToList()
                ?? [];

            selects.AddRange(aggs);

            if (selects.Count == 0)
            {
                throw new InvalidOperationException("Must select something");
            }

            var filters = outerFilters.Concat(Filters).Select(f => new
            {
                FilterSql = FormatFilter(f, joins, sql, filterParams),
            })
            .ToList();

            var groupBy = totals || (AllowDuplicates && (Aggregations?.Count ?? 0) == 0) ? null : Select?.Select(x => new
            {
                Part = joins.Aliased(x, sql)
            })
            .ToList();

            var skipAndTake = !totals
                    ? sql.SkipAndTake(Skip, Take)
                    : null;

            var orderBy = skipAndTake == null ? null :
                OrderBy.Any() ? string.Join(", ", OrderBy.Select(x => FindIndexedOrderingColumn(x) ?? FindNamedSelectOrderingColumn(x, Select))) :
                aggs.Count > 0 ? $"{(Select?.Count ?? 0) + 1} desc" :
                "1 asc";

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

            return $"{found.n + 1} {ordering.Direction}";
        }

        private static readonly Regex SanitiseCommentPattern = new("[^\\w\\d\\r\\n]+");

        public string ToSql(ISqlFormatter sql,
                            IFilterParameters filterParams,
                            IEnumerable<Filter> outerFilters)
        {
            var result = ToSql(sql, filterParams, outerFilters, false);

            if (Totals)
            {
                result += ";" + ToSql(sql, filterParams, outerFilters, true);
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

            var result = new QueryResultJson
            {
                Records = ConvertRecords(reader.Read<dynamic>()
                                .Cast<IDictionary<string, object>>())
            };
            
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