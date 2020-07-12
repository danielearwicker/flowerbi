using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TinyBI.Engine.JsonModels;
using Dapper;
using HandlebarsDotNet;

namespace TinyBI
{
    public class Query
    {
        public IList<IColumn> Select { get; }
        public IList<Aggregation> Aggregations { get; }
        public IList<Filter> Filters { get; }
        public IList<Ordering> OrderBy { get; }

        public bool Totals { get; }

        public Query(QueryJson json, Schema schema)
        {
            Select = schema.Load(json.Select);            
            Aggregations = json.Aggregations.Select(x => new Aggregation(x, schema)).ToList();
            Filters = Filter.Load(json.Filters, schema);
            OrderBy = Ordering.Load(json.OrderBy, schema);
            Totals = json.Totals;
        }

        private static readonly Func<object, string> _template = Handlebars.Compile(@"

with {{#each Aggregations}}

    [Aggregation{{@index}}] as (
        {{{this}}}
    )
    {{#unless @last}},{{/unless}}

{{/each}}

select top {{Top}}

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
                a{{../../@index}}.Select{{@index}} = a0.Select{{@index}}
            {{/each}}
        {{/if}}
        {{#unless ../Select}}
            cross join Aggregation{{@index}} a{{@index}}
        {{/unless}}
    {{/unless}}
{{/each}}

order by {{ordering}}
");

        private string ToSql(IList<IColumn> select, FilterParameters filterParams, IEnumerable<Filter> outerFilters, int top)
        {
            if (Aggregations.Count == 1 && OrderBy.Count == 0)
            {
                return Aggregations[0].ToSql(select, outerFilters.Concat(Filters), filterParams, top);
            }

            var ordering = "a0.Value0 desc";

            if (top > 1 && OrderBy.Count != 0)
            {
                ordering = string.Join(", ", OrderBy.Select(FindOrderingColumn));
            }

            return _template(new
            {
                Top = top,
                Aggregations = Aggregations.Select(x =>
                    x.ToSql(select, outerFilters.Concat(Filters), filterParams)),
                Select = select,
                ordering
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

            var direction = ordering.Descending ? "desc" : "asc";

            return $"a0.Select{i} {direction}";
        }

        public string ToSql(FilterParameters filterParams, IEnumerable<Filter> outerFilters, int top)
        {
            var sql = ToSql(Select, filterParams, outerFilters, top);

            if (Totals)
            {
                sql += ";" + ToSql(null, filterParams, outerFilters, 1);
            }

            return sql;
        }
        
        public QueryResult Run(IDbConnection db, int top, params Filter[] outerFilters)
        {
            var filterParams = new FilterParameters();

            var sql = ToSql(filterParams, outerFilters, top);

            Console.WriteLine(@$"
----------------------------------------------------------------------------
{sql}
----------------------------------------------------------------------------
");

            var reader = db.QueryMultiple(sql, filterParams.DapperParams);

            var result = new QueryResult();

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

        private static IList<QueryRecord> ConvertRecords(IEnumerable<IDictionary<string, object>> list)
            => list.Select(x => new QueryRecord
                {
                     Selected = GetList(x, "Select"),
                     Aggregated = GetList(x, "Value"),
                })
                .ToList();
        

        private static IList<object> GetList(IDictionary<string, object> raw, string prefix)
        {
            IList<object> result = null;

            for (var n = 0; n < 100; n++)
            {
                if (!raw.TryGetValue($"{prefix}{n}", out var value)) break;
                if (result == null) result = new List<object>();
                result.Add(value);
            }

            return result;
        }
    }

    public class QueryRecord
    {
        public IList<object> Selected { get; set; }
        public IList<object> Aggregated { get; set; }
    }

    public class QueryResult
    {
        public IList<QueryRecord> Records { get;set; }
        public QueryRecord Totals { get; set; }
    }
}
