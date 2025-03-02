using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using FlowerBI.Engine.JsonModels;
using HandlebarsDotNet;

namespace FlowerBI;

public class Query(QueryJson json, Schema schema)
{
    public IList<LabelledColumn> Select { get; } = schema.Load(json.Select);

    public IList<Aggregation> Aggregations { get; } = Aggregation.Load(json.Aggregations, schema);

    public IList<Filter> Filters { get; } = Filter.Load(json.Filters, schema);

    public IList<Ordering> OrderBy { get; } =
        Ordering.Load(
            json.OrderBy,
            schema,
            json.Select?.Count ?? 0,
            json.Aggregations?.Count ?? 0,
            json.Calculations?.Count ?? 0
        );

    public IList<CalculationJson> Calculations { get; } = json.Calculations ?? [];

    public bool Totals { get; } = json.Totals ?? false;

    public long Skip { get; } = json.Skip ?? 0;

    public int Take { get; } = json.Take ?? 100;

    public string Comment { get; } = json.Comment;

    public bool AllowDuplicates { get; } = json.AllowDuplicates ?? false;

    public int CommandTimeoutSeconds { get; } = 30;

    public bool FullJoins { get; } = json.FullJoins ?? false;

    private const string _templateMain = """
        select
            {{#each selects}}
            {{{this}}}{{#unless @last}}, {{/unless}}
            {{/each}}
        {{joins}}
        {{#if filters}}
        where
            {{#each filters}}
            {{{FilterSql}}}{{#unless @last}} and {{/unless}}
            {{/each}}
        {{/if}}
        {{#if groupBy}}
        group by
            {{#each groupBy}}
            {{Part}}{{#unless @last}}, {{/unless}}
            {{/each}}
        {{/if}}
        """;

    private const string _templateFooter = """
        {{#if orderBy}}
        order by {{orderBy}}
        {{/if}}
        {{#if skipAndTake}}
        {{skipAndTake}}
        {{/if}}
        """;

    private static readonly HandlebarsTemplate<object, string> _templateWithoutCalculations =
        Handlebars.Compile(
            $"""
            {_templateMain}
            {_templateFooter}
            """
        );

    private static readonly HandlebarsTemplate<object, string> _templateWithCalculations =
        Handlebars.Compile(
            $$$"""
            with calculation_source as (
                {{{_templateMain}}}
            )
            select calculation_source.*
                {{#each calculations}}
                    ,{{this}}
                {{/each}}
            from calculation_source
            {{{_templateFooter}}}
            """
        );

    // This needs to accept filters and use them inside CASE WHEN {filters} THEN {expr} END
    private static string FormatAggFunction(
        AggregationType func,
        string expr,
        IEnumerable<Filter> filters,
        Joins joins,
        ISqlFormatter sql,
        IFilterParameters filterParams
    )
    {
        if (filters.Any())
        {
            var when = string.Join(
                " and ",
                filters.Select(f => FormatFilter(f, joins, sql, filterParams))
            );
            expr = $"case when {when} then {expr} end";
        }

        return func == AggregationType.CountDistinct
            ? $"count(distinct {expr})"
            : $"{func}({expr})";
    }

    private static string FormatFilter(
        Filter f,
        Joins joins,
        ISqlFormatter sql,
        IFilterParameters filterParams
    )
    {
        var column = joins.Aliased(f.Column, sql);
        var param = filterParams[f];

        if (f.Operator == "BITS IN")
        {
            // constant must be provided and is treated as an integer bit mask
            var mask =
                f.Constant is int i ? i
                : f.Constant is long l ? l
                : f.Constant is double d ? (int)d
                : throw new FlowerBIException("BITS IN filter requires integer constant");

            return $"({column} & {mask}) in {param}";
        }

        return $"{column} {f.Operator} {param}";
    }

    public string ToSql(
        ISqlFormatter sql,
        IFilterParameters filterParams,
        IEnumerable<Filter> outerFilters,
        bool totals
    ) => ToSqlAndTables(sql, filterParams, outerFilters, totals).Sql;

    public IEnumerable<Table> GetRequiredTables(IEnumerable<Filter> outerFilters) =>
        ToSqlAndTables(
            NullSqlFormatter.Singleton,
            NullFilterParameters.Singleton,
            outerFilters,
            false
        )
            .Tables.Select(x => x.Value)
            .Distinct()
            .ToList();

    public (string Sql, IEnumerable<LabelledTable> Tables) ToSqlAndTables(
        ISqlFormatter sql,
        IFilterParameters filterParams,
        IEnumerable<Filter> outerFilters,
        bool totals
    )
    {
        var joins = new Joins();

        var selects =
            (totals ? null : Select)
                ?.Select(
                    (c, i) =>
                        $"{sql.IdentifierPair(joins.GetAlias(c.Value.Table, c.JoinLabel), c.Value.DbName)} Select{i}"
                )
                .ToList() ?? [];

        var aggs =
            Aggregations
                ?.Select(
                    (a, i) =>
                        $"{FormatAggFunction(a.Function, joins.Aliased(a.Column, sql), a.Filters, joins, sql, filterParams)} Value{i}"
                )
                .ToList() ?? [];

        selects.AddRange(aggs);

        if (selects.Count == 0)
        {
            throw new FlowerBIException("Must select something");
        }

        var filters = outerFilters
            .Concat(Filters)
            .Select(f => new { FilterSql = FormatFilter(f, joins, sql, filterParams) })
            .ToList();

        var groupBy =
            totals || (AllowDuplicates && (Aggregations?.Count ?? 0) == 0)
                ? null
                : Select?.Select(x => new { Part = joins.Aliased(x, sql) }).ToList();

        var skipAndTake = !totals ? sql.SkipAndTake(Skip, Take) : null;

        var orderBy =
            skipAndTake == null ? null
            : OrderBy.Any()
                ? string.Join(
                    ", ",
                    OrderBy.Select(x =>
                        FindIndexedOrderingColumn(x) ?? FindNamedSelectOrderingColumn(x, Select)
                    )
                )
            : aggs.Count > 0 ? $"{(Select?.Count ?? 0) + 1} desc"
            : "1 asc";

        var calculations =
            Calculations
                ?.Select(x => x.ToSql(sql, i => $"Value{i}"))
                .Select((c, i) => $"{c} Value{(Aggregations?.Count ?? 0) + i}")
                .ToList() ?? [];

        var template =
            calculations.Count == 0 ? _templateWithoutCalculations : _templateWithCalculations;

        var (joinSql, joinedTables) = joins.ToSqlAndTables(sql, FullJoins);

        var sqlFromTemplate = template(
            new
            {
                selects,
                filters,
                calculations,
                joins = joinSql,
                groupBy,
                orderBy,
                skipAndTake,
            }
        );

        return (sqlFromTemplate, joinedTables);
    }

    public static string FindIndexedOrderingColumn(Ordering ordering) =>
        ordering.Column == null ? $"{ordering.Index + 1} {ordering.Direction}" : null;

    public static string FindNamedSelectOrderingColumn(
        Ordering ordering,
        IEnumerable<LabelledColumn> selects
    )
    {
        var found = selects.Select((c, n) => (c, n)).FirstOrDefault(x => x.c == ordering.Column);
        if (found.c == null)
        {
            throw new FlowerBIException(
                $"Cannot order by {ordering.Column} as it has not been selected"
            );
        }

        return $"{found.n + 1} {ordering.Direction}";
    }

    private static readonly Regex SanitiseCommentPattern = new("[^\\w\\d\\r\\n]+");

    public string ToSql(
        ISqlFormatter sql,
        IFilterParameters filterParams,
        IEnumerable<Filter> outerFilters
    )
    {
        var result = string.Empty;

        if (!string.IsNullOrWhiteSpace(Comment))
        {
            var stripped = SanitiseCommentPattern.Replace(Comment, " ").Trim();
            result += $"/* {stripped} */ \r\n";
        }

        if (Totals)
        {
            result += ToSql(sql, filterParams, outerFilters, true) + ";";
        }

        result += ToSql(sql, filterParams, outerFilters, false);

        return result;
    }

    public QueryResultJson Run(
        ISqlFormatter sql,
        IDbConnection db,
        Action<string> log,
        params Filter[] outerFilters
    )
    {
        var result = new QueryResultJson { Records = [] };

        var stream = Stream(sql, db, log, outerFilters).GetEnumerator();

        if (Totals)
        {
            if (!stream.MoveNext())
            {
                throw new FlowerBIException("Expected at least one record");
            }

            result.Totals = stream.Current;
        }

        while (stream.MoveNext())
        {
            result.Records.Add(stream.Current);
        }

        return result;
    }

    public IEnumerable<QueryRecordJson> Stream(
        ISqlFormatter sql,
        Func<IDbConnection> connect,
        Action<string> log,
        params Filter[] outerFilters
    )
    {
        using var db = connect();

        foreach (var record in Stream(sql, db, log, outerFilters))
        {
            yield return record;
        }
    }

    private IEnumerable<QueryRecordJson> Stream(
        ISqlFormatter sql,
        IDbConnection db,
        Action<string> log,
        params Filter[] outerFilters
    )
    {
        var filterParams = new DapperFilterParameters();

        var querySql = ToSql(sql, filterParams, outerFilters);

        log?.Invoke($"{querySql} with parameters: {filterParams}");

        var reader = db.QueryMultiple(
            querySql,
            filterParams.DapperParams,
            commandTimeout: CommandTimeoutSeconds
        );

        if (Totals)
        {
            yield return ConvertRecords(
                    reader.Read<dynamic>(buffered: false).Cast<IDictionary<string, object>>()
                )
                .Single();
        }

        foreach (
            var record in ConvertRecords(
                reader.Read<dynamic>(buffered: false).Cast<IDictionary<string, object>>()
            )
        )
        {
            yield return record;
        }
    }

    private IEnumerable<QueryRecordJson> ConvertRecords(
        IEnumerable<IDictionary<string, object>> list
    )
    {
        var nullConvert = new Func<object, object>(x => x);

        var aggColumns = Aggregations
            .Select(x => new Func<object, object>(x.Convert))
            .Concat(Calculations.Select(x => nullConvert))
            .ToList();

        var selColumns = Select
            .Select(x => new Func<object, object>(x.Value.ConvertValue))
            .ToList();

        return list.Select(x => new QueryRecordJson
        {
            Selected = GetList(x, "Select", selColumns),
            Aggregated = GetList(x, "Value", aggColumns),
        });
    }

    private static IList<object> GetList(
        IDictionary<string, object> raw,
        string prefix,
        IReadOnlyList<Func<object, object>> converters
    )
    {
        IList<object> result = null;

        for (var n = 0; n < 100; n++)
        {
            if (!raw.TryGetValue($"{prefix}{n}", out var value))
                break;

            result ??= [];
            result.Add(converters[n](value));
        }

        return result;
    }
}
