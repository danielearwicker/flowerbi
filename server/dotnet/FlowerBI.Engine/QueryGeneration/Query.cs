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
        public IList<IColumn> Select { get; }
        public IList<Aggregation> Aggregations { get; }
        public IList<Filter> Filters { get; }
        public IList<Ordering> OrderBy { get; }

        public bool Totals { get; }

        public long Skip { get; }

        public int Take { get; }

        public string Comment { get; }

        public bool AllowDuplicates { get; }

        public Query(QueryJson json, Schema schema)
        {
            Select = schema.Load(json.Select);
            Aggregations = Aggregation.Load(json.Aggregations, schema);
            Filters = Filter.Load(json.Filters, schema);
            OrderBy = Ordering.Load(json.OrderBy, schema);
            Totals = json.Totals;
            Skip = json.Skip;
            Take = json.Take;
            Comment = json.Comment;
            AllowDuplicates = json.AllowDuplicates;
        }

        private static readonly HandlebarsTemplate<object, string> _aggregatedTemplate = Handlebars.Compile(@"

with {{#each Aggregations}}

    Aggregation{{@index}} as (
        {{{this}}}
    )
    {{#unless @last}},{{/unless}}

{{/each}}

select

{{#each Select}}
    a0.Select{{@index}},
{{/each}}

{{#each Aggregations}}
    a{{@index}}.Value0 Value{{@index}}
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

{{#if orderBy}}
    order by {{orderBy}}
{{/if}}

{{skipAndTake}}
");

        private string ToSql(ISqlFormatter sql, IList<IColumn> select,
                             IFilterParameters filterParams, IEnumerable<Filter> outerFilters,
                             long skip, int take, bool totals)
        {
            if (Aggregations.Count == 1)
            {
                return Aggregations[0].ToSql(sql, select, outerFilters.Concat(Filters), filterParams, OrderBy, skip, take, AllowDuplicates, totals);
            }

            var ordering = "a0.Value0 desc";

            if (select != null && OrderBy.Count != 0)
            {
                ordering = string.Join(", ", OrderBy.Select(FindOrderingColumn));
            }

            return _aggregatedTemplate(new
            {
                skipAndTake = totals ? null : sql.SkipAndTake(skip, take),
                Aggregations = Aggregations.Select(x =>
                    x.ToSql(sql, select, outerFilters.Concat(Filters), filterParams)),
                Select = select,
                orderBy = totals ? null : ordering
            });
        }

        private string FindOrderingColumn(Ordering ordering)
        {
            var i = Select.IndexOf(ordering.Column);
            if (i == -1)
            {
                throw new InvalidOperationException(
                    $"Cannot order by {ordering.Column} as it has not been selected");
            }

            return $"a0.Select{i} {ordering.Direction}";
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

            var reader = db.QueryMultiple(querySql, filterParams.DapperParams);

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
            var aggColumns = Aggregations.Select(x => x.Column).ToList();
            var selColumns = Select.ToList();

            return list.Select(x => new QueryRecordJson
            {
                Selected = GetList(x, "Select", selColumns),
                Aggregated = GetList(x, "Value", aggColumns),
            })
                .ToList();
        }

        private static IList<object> GetList(IDictionary<string, object> raw, string prefix, IReadOnlyList<IColumn> columns)
        {
            IList<object> result = null;

            for (var n = 0; n < 100; n++)
            {
                if (!raw.TryGetValue($"{prefix}{n}", out var value)) break;
                if (result == null) result = new List<object>();

                result.Add(columns[n].ConvertValue(value));
            }

            return result;
        }
    }
}