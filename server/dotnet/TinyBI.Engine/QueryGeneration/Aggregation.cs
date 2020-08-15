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
        public AggregationType Function { get; }

        public IColumn Column { get; }

        public IEnumerable<Filter> Filters { get; }

        public Aggregation(AggregationJson json, Schema schema)            
        {
            Function = json.Function;
            Column = schema.GetColumn(json.Column);
            Filters = Filter.Load(json.Filters, schema);
        }

        private static readonly Func<object, string> _template = Handlebars.Compile(@"
select
    
    {{#each selects}}
        {{this}} Select{{@index}},
    {{/each}}

    {{Function}}({{mainColumn}}) Value0

from {{mainTable}} main

{{#each Joins}}
join {{Table}} {{Alias}}
  on {{Left}} = {{Right}}
{{/each}}

{{#if Filters}}
where
    {{#each Filters}}
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

{{#if skipAndTake}}
    order by {{Function}}({{mainColumn}}) desc
    {{skipAndTake}}
{{/if}}
");
        public string ToSql(
            ISqlFormatter sql,
            IEnumerable<IColumn> selectColumns,
            IEnumerable<Filter> outerFilters,
            IFilterParameters filterParams,
            long? skip = null,
            int? take = null)
        {
            var joins = new Joins("main", Column.Table);

            var selects = selectColumns?.Select(c =>
                sql.IdentifierPair(joins[c.Table], c.DbName)).ToList();

            var filters = outerFilters.Concat(Filters).Select(f => new
            {
                Column = f.Column == f.Column.Table.Id
                    ? sql.IdentifierPair("main", Column.Table.GetForeignKeyTo(f.Column.Table).DbName)
                    : sql.IdentifierPair(joins[f.Column.Table], f.Column.DbName),
                f.Operator,
                Param = filterParams[f]
            })
            .ToList();

            var groupBy = selectColumns?.Select(x => new
            {
                Part = sql.IdentifierPair(joins[x.Table], x.DbName)
            })
            .ToList();

            var joinParts = joins.Aliases.Select(x => new
            {
                Table = sql.IdentifierPair(x.Key.Schema.DbName, x.Key.DbName),
                Alias = x.Value,
                Left = sql.IdentifierPair(x.Value, x.Key.Id.DbName),
                Right = sql.IdentifierPair("main", Column.Table.GetForeignKeyTo(x.Key).DbName)
            })
            .ToList();

            return _template(new
            {
                selects,
                mainColumn = sql.IdentifierPair("main", Column.DbName),
                mainTable = sql.IdentifierPair(Column.Table.Schema.DbName, Column.Table.DbName),
                Function,
                Filters = filters,
                Joins = joinParts,
                groupBy,
                skipAndTake = skip != null && take != null
                    ? sql.SkipAndTake(skip.Value, take.Value)
                    : null
            });
        }
    }
}
