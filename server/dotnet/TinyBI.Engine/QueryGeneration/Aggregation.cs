using System;
using System.Collections.Generic;
using System.Linq;
using TinyBI.Engine.JsonModels;
using HandlebarsDotNet;

namespace TinyBI
{
    public enum AggregationType
    {
        Count,
        Sum
    };

    public class Aggregation
    {
        public AggregationType? Function { get; }

        public IColumn Column { get; }

        public IEnumerable<Filter> Filters { get; }

        public Aggregation(AggregationJson json, Schema schema)            
        {
            Function = json.Function;
            Column = schema.GetColumn(json.Column);
            Filters = Filter.Load(json.Filters, schema);
        }

        private static readonly Func<object, string> _template = Handlebars.Compile(@"
select {{#if top}}top {{top}}{{/if}}
    
    {{#each selectColumns}}
        {{this}} Select{{@index}},
    {{/each}}

    {{#if Function}} {{Function}} ({{/if}}
        main.[{{Column.DbName}}] 
    {{#if Function}} ) {{/if}} Value0

from [{{Column.Table.Schema.DbName}}].[{{Column.Table.DbName}}] main

{{#each Joins}}
join [{{Table.Schema.DbName}}].[{{Table.DbName}}] {{Alias}}
  on {{Alias}}.[{{Table.Id.DbName}}] = main.[{{ForeignKey.DbName}}]
{{/each}}

{{#if Filters}}
where
    {{#each Filters}}
        {{Column}} {{{Operator}}} @{{Param}}
        {{#unless @last}} and {{/unless}}
    {{/each}}
{{/if}}

{{#if groupBy}}
    group by
    {{#each groupBy}}
        {{Alias}}.[{{Column}}]
        {{#unless @last}} , {{/unless}}
    {{/each}}
{{/if}}

{{#if top}}
    order by main.Value0 desc
{{/if}}
");

        private string GenerateSelect(
            IEnumerable<string> selectColumns,
            IEnumerable<Filter> outerFilters,
            FilterParameters filterParams,
            IEnumerable<IColumn> groupByColumns,
            Joins joins,
            int? top = null)
        {
            var filters = outerFilters.Concat(Filters).Select(f => new
            {
                Column = f.Column == f.Column.Table.Id
                    ? $"main.[{Column.Table.GetForeignKeyTo(f.Column.Table).DbName}]"
                    : $"{joins[f.Column.Table]}.[{f.Column.DbName}]",
                f.Operator,
                Param = filterParams[f]
            })
            .ToList();

            var groupBy = groupByColumns?.Select(x => new
                {
                    Alias = joins[x.Table],
                    Column = x.DbName
                })
                .ToList();

            return _template(new
            {
                selectColumns,
                Column,
                Function,
                Filters = filters,
                Joins = joins.Aliases.Select(x => new
                {
                    Table = x.Key,
                    Alias = x.Value,
                    ForeignKey = Column.Table.GetForeignKeyTo(x.Key)                    
                }),
                groupBy,
                top
            });
        }

        public string ToSql(
            IEnumerable<IColumn> selectColumns,
            IEnumerable<Filter> outerFilters,
            FilterParameters filterParams,
            bool totals,
            int? top = null)
        {
            var joins = new Joins("main", Column.Table);

            var selects = selectColumns.Select(c => $"{joins[c.Table]}.[{c.DbName}]").ToList();

            var unionOf = new List<string>
            {
                GenerateSelect(selects, outerFilters, filterParams, Function != null ? selectColumns : null, joins, top)
            };

            if (totals)
            {
                joins = new Joins("main", Column.Table);
                unionOf.Add(GenerateSelect(selectColumns.Select(_ => "'_grand_total_'"), outerFilters, filterParams, null, joins));
            }

            return string.Join(" union all ", unionOf);
        }
    }
}
