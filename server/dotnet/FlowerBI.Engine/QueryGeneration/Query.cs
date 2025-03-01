using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using FlowerBI.Engine.JsonModels;
using HandlebarsDotNet;
#if NET8_0_OR_GREATER
using Parquet;
using DataColumn = Parquet.Data.DataColumn;
#endif

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

        var (joinSql, joinedTables) = joins.ToSqlAndTables(sql);

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

#if NET8_0_OR_GREATER

    public record FilterWithColumn(Func<object, bool> Eval, IColumn Column)
    {
        public FilterWithColumn(Filter filter)
            : this(filter.Compile(), filter.Column.Value) { }
    }

    public record AggregatorWithFilters(IAggregator Aggregator, FilterWithColumn[] Filters);

    public async Task<QueryResultJson> QueryParquet(Stream parquetStream)
    {
        using var reader = await ParquetReader.CreateAsync(parquetStream);
        var parquetSchema = reader.Schema;

        // Get all referenced columns so we can try to map them to the schema
        var columns = Select
            .Select(s => s.Value)
            .Concat(Aggregations.Select(a => a.Column.Value))
            .Concat(Aggregations.SelectMany(a => a.Filters.Select(f => f.Column.Value)))
            .Concat(Filters.Select(f => f.Column.Value))
            .Concat(OrderBy.Select(x => x.Column?.Value).Where(t => t != null))
            .Distinct()
            .ToList();

        // Validate that all columns refer to the same table
        var tables = columns.Select(c => c.Table).Distinct().ToList();
        if (tables.Count > 1)
        {
            throw new FlowerBIException(
                "All columns must be from the same table - tables referenced: "
                    + string.Join(", ", tables.Select(t => t.RefName))
            );
        }

        // Map the columns to the parquet schema
        var columnFields = columns.ToDictionary(
            c => c,
            c => parquetSchema.DataFields.FirstOrDefault(f => f.Name == c.DbName)
        );

        // Validate that all columns are present in the parquet schema
        var missingColumns = columnFields
            .Where(kv => kv.Value == null)
            .Select(kv => kv.Key)
            .ToList();
        if (missingColumns.Count != 0)
        {
            throw new FlowerBIException(
                "Columns not found in parquet schema: "
                    + string.Join(", ", missingColumns.Select(c => c.DbName))
            );
        }

        var groupings = new Dictionary<object[], AggregatorWithFilters[]>(ArrayComparer.Instance);

        var totals = new AggregatorWithFilters[Aggregations.Count];

        var filters = Filters.Select(f => new FilterWithColumn(f)).ToArray();

        static AggregatorWithFilters MakeAggregator(Aggregation aggregation, Type clrType)
        {
            var filters = aggregation.Filters.Select(f => new FilterWithColumn(f)).ToArray();
            var aggImpl = aggregation.Function switch
            {
                AggregationType.Count => new Count(),
                AggregationType.Sum => MakeTypedAggregator(typeof(Sum<>), clrType),
                AggregationType.Avg => MakeTypedAggregator(typeof(Average<>), clrType),
                AggregationType.Min => MakeTypedAggregator(typeof(Min<>), clrType),
                AggregationType.Max => MakeTypedAggregator(typeof(Max<>), clrType),
                _ => throw new FlowerBIException("Unsupported aggregation type"),
            };
            return new(aggImpl, filters);
        }

        foreach (var rowGroup in reader.RowGroups)
        {
            var columnData = new Dictionary<IColumn, DataColumn>();

            foreach (var column in columns)
            {
                columnData[column] = await rowGroup.ReadColumnAsync(columnFields[column]);
            }

            var boundFilters = filters.Select(f => (f.Eval, columnData[f.Column].Data)).ToList();

            for (var r = 0; r < rowGroup.RowCount; r++)
            {
                if (!boundFilters.All(f => f.Eval(f.Data.GetValue(r))))
                {
                    continue;
                }

                // Build grouping key
                var groupingKey = new object[Select.Count];
                for (var i = 0; i < Select.Count; i++)
                {
                    var column = Select[i].Value;
                    var value = columnData[column].Data.GetValue(r);
                    groupingKey[i] = value;
                }

                if (!groupings.TryGetValue(groupingKey, out var grouping))
                {
                    grouping = groupings[groupingKey] = new AggregatorWithFilters[
                        Aggregations.Count
                    ];

                    for (var a = 0; a < Aggregations.Count; a++)
                    {
                        var clrType = columnData[Aggregations[a].Column.Value].Field.ClrType;
                        grouping[a] = MakeAggregator(Aggregations[a], clrType);
                        totals[a] ??= MakeAggregator(Aggregations[a], clrType);
                    }
                }

                for (var a = 0; a < Aggregations.Count; a++)
                {
                    var aggregation = Aggregations[a];
                    var value = columnData[aggregation.Column.Value].Data.GetValue(r);

                    var (aggImpl, aggFilters) = grouping[a];

                    if (
                        aggFilters.All(filter =>
                            filter.Eval(columnData[filter.Column].Data.GetValue(r))
                        )
                    )
                    {
                        grouping[a].Aggregator.Add(value);
                        totals[a].Aggregator.Add(value);
                    }
                }
            }
        }

        return new QueryResultJson
        {
            Records =
            [
                .. groupings.Select(grouping => new QueryRecordJson
                {
                    Selected = [.. grouping.Key.Select((v, i) => Select[i].Value.ConvertValue(v))],
                    Aggregated = [.. grouping.Value.Select((a, i) => a.Aggregator.Result)],
                }),
            ],

            Totals = new QueryRecordJson
            {
                Selected = new object[Select.Count],
                Aggregated = [.. totals.Select(a => a?.Aggregator.Result)],
            },
        };
    }

    private static IAggregator MakeTypedAggregator(Type genericType, Type valueType) =>
        (IAggregator)Activator.CreateInstance(genericType.MakeGenericType(valueType));

#endif
}

#if NET8_0_OR_GREATER

public interface IAggregator
{
    void Add(object value);

    object Result { get; }
}

public abstract class Aggregator<T> : IAggregator
{
    protected abstract void AddTyped(T value);

    public void Add(object value) => AddTyped((T)value);

    public abstract object Result { get; }
}

public class Count : IAggregator
{
    private int _count;

    public void Add(object value) => _count++;

    public object Result => _count;
}

public class Sum<T> : Aggregator<T>
    where T : struct, INumber<T>
{
    private T? _sum;

    protected override void AddTyped(T value) => _sum = _sum == null ? value : _sum + value;

    public override object Result => _sum;
}

public class Min<T> : Aggregator<T>
    where T : struct, INumber<T>
{
    private T? _min;

    protected override void AddTyped(T value) =>
        _min = _min == null ? value : T.Min(_min.Value, value);

    public override object Result => _min;
}

public class Max<T> : Aggregator<T>
    where T : struct, INumber<T>
{
    private T? _max;

    protected override void AddTyped(T value) =>
        _max = _max == null ? value : T.Max(_max.Value, value);

    public override object Result => _max;
}

public class Average<T> : Aggregator<T>
    where T : INumber<T>
{
    private T _sum;
    private int _count;

    protected override void AddTyped(T value)
    {
        _sum += value;
        _count++;
    }

    public override object Result =>
        _count == 0 ? null : _sum / (T)Convert.ChangeType(_count, typeof(T));
}

public class ArrayComparer : IEqualityComparer<object[]>
{
    public static readonly ArrayComparer Instance = new();

    public bool Equals(object[] x, object[] y)
    {
        for (var i = 0; i < x.Length; i++)
        {
            var comparison = Comparer.Default.Compare(x[i], y[i]);
            if (comparison != 0)
                return false;
        }

        return true;
    }

    public int GetHashCode([DisallowNull] object[] obj)
    {
        var hash = new HashCode();
        foreach (var item in obj)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}

#endif
