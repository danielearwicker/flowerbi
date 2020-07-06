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

        public bool Totals { get; }

        public Query(QueryJson json, Schema schema)
        {
            Select = schema.Load(json.Select);            
            Aggregations = json.Aggregations.Select(x => new Aggregation(x, schema)).ToList();
            Filters = Filter.Load(json.Filters, schema);
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
        left join Aggregation{{@index}} a{{@index}} on
        {{#each ../Select}}
            a{{../../@index}}.Select{{@index}} = a0.Select{{@index}}
        {{/each}}
    {{/unless}}
{{/each}}

order by a0.Value0 desc
");


        public string ToSql(FilterParameters filterParams, IEnumerable<Filter> outerFilters, int top)
        {
            if (Aggregations.Count == 1)
            {
                return Aggregations[0].ToSql(Select, outerFilters.Concat(Filters), filterParams, Totals, top);
            }

            return _template(new
            {
                Top = top,
                Aggregations = Aggregations.Select(x =>
                    x.ToSql(Select, outerFilters.Concat(Filters), filterParams, Totals)),
                Select
            });
        }

        public IEnumerable<dynamic> Run(IDbConnection db, int top, params Filter[] outerFilters)
        {
            var filterParams = new FilterParameters();
            var sql = ToSql(filterParams, outerFilters, top);

            Console.WriteLine(sql);

            return db.Query<dynamic>(sql, filterParams.DapperParams)
                     .Cast<IDictionary<string, object>>()
                     .Select(x => new QueryRecord
                     {
                         Selected = GetList(x, "Select"),
                         Aggregated = GetList(x, "Value"),
                     });
        }

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
}
