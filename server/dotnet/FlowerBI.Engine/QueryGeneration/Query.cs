﻿using System;
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

        private static string FormatAggFunction(AggregationType func, string expr)
            => func == AggregationType.CountDistinct
                ? $"count(distinct {expr})"
                : $"{func}({expr})";

        private static string FormatFilter(string column, string op, string param, object constant)
        {
            if (op == "BITS IN")
            {
                // constant must be provided and is treated as an integer bit mask
                var mask = constant is int i ? i :
                           constant is long l ? l :
                           constant is double d ? (int)d :
                           throw new InvalidOperationException("BITS IN filter requires integer constant");

                return $"({column} & {mask}) in {param}";
            }

            return $"{column} {op} {param}";
        }

        public string ToSql(
            ISqlFormatter sql,
            IEnumerable<Filter> outerFilters,
            IFilterParameters filterParams,
            IEnumerable<Ordering> orderings = null,
            long? skip = null,
            int? take = null,
            bool allowDuplicates = false,
            bool totals = false)
        {
            var joins = new Joins();

            var selects = Select?.Select((c, i) =>
                $"{sql.IdentifierPair(joins.GetAlias(c.Value.Table, c.JoinLabel), c.Value.DbName)} Select{i}").ToList()
                ?? new List<string>();

            var aggs = Aggregations?.Select((a, i) =>
                $"{FormatAggFunction(Function, joins.Aliased(Column, sql))} Value{i}").ToList()
                ?? new List<string>();

            selects.AddRange(aggs);

            if (selects.Count == 0)
            {
                throw new InvalidOperationException("Must select something");
            }

            var filters = outerFilters.Concat(Filters).Select(f => new
            {
                FilterSql = FormatFilter(joins.Aliased(f.Column, sql), f.Operator, filterParams[f], f.Constant),
            })
            .ToList();

            var groupBy = (allowDuplicates && Column == null) ? null : Select?.Select(x => new
            {
                Part = joins.Aliased(x, sql)
            })
            .ToList();

            var skipAndTake = skip != null && take != null && !totals
                    ? sql.SkipAndTake(skip.Value, take.Value)
                    : null;

            var orderBy = skipAndTake == null ? null :
                orderings.Any() ? string.Join(", ", orderings.Select(x => Query.FindIndexedOrderingColumn(x) ?? FindNamedSelectOrderingColumn(x, Select)) :
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

            var aggColumns = Aggregations.Select(x => x?.Column == null ? nullConvert : new Func<object, object>(x.Column.Value.ConvertValue))
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