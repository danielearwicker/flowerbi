using System;
using System.Collections.Generic;
using System.Linq;
using FlowerBI.Engine.JsonModels;
using FlowerBI.JintEngine;

namespace FlowerBI.Jint;

/// <summary>
/// Jint-based implementation of FlowerBI Query
/// Maintains compatibility with existing API while using JavaScript bundle
/// </summary>
public class JintQuery : IDisposable
{
    private readonly JintQueryEngine _queryEngine;
    private readonly QueryJson _queryJson;
    private PreparedQuery _preparedQuery;
    private bool _disposed;

    public JintQuery(QueryJson json, JintSchema schema, string databaseType = "sqlite")
    {
        _queryJson = json;
        _queryEngine = schema.CreateQueryEngine(databaseType);
    }

    /// <summary>
    /// Get the SQL and parameters for this query
    /// </summary>
    public PreparedQuery GetPreparedQuery()
    {
        return _preparedQuery ??= _queryEngine.PrepareQuery(_queryJson);
    }

    /// <summary>
    /// Get the SQL string for this query
    /// </summary>
    public string ToSql()
    {
        return GetPreparedQuery().Sql;
    }

    /// <summary>
    /// Get the parameters for this query
    /// </summary>
    public object[] GetParameters()
    {
        return GetPreparedQuery().Parameters;
    }

    /// <summary>
    /// Map database results to FlowerBI format
    /// </summary>
    public QueryResultJson MapResults(DatabaseResult databaseResult)
    {
        return _queryEngine.MapResults(_queryJson, databaseResult);
    }

    /// <summary>
    /// Map database results from array of arrays format
    /// </summary>
    public QueryResultJson MapResults(object[][] rows)
    {
        var databaseResult = new DatabaseResult("array-of-arrays", rows);
        return MapResults(databaseResult);
    }

    /// <summary>
    /// Map database results from objects (as might come from Dapper)
    /// </summary>
    public QueryResultJson MapResults(IEnumerable<object> rows)
    {
        // Convert objects to dictionary format for JSON serialization
        var objectRows = rows.Select(row => 
        {
            if (row is IDictionary<string, object> dict)
                return dict;
            
            // Use reflection to convert object to dictionary
            var type = row.GetType();
            var properties = type.GetProperties();
            return properties.ToDictionary(p => p.Name, p => p.GetValue(row));
        }).ToArray();

        var databaseResult = new DatabaseResult("array-of-objects", 
            objectRows.Select(row => new object[] { row }).ToArray());
        return MapResults(databaseResult);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // JintQueryEngine doesn't need disposal - it's owned by JintSchema
            _disposed = true;
        }
    }
}

/// <summary>
/// Static factory methods for creating queries (maintains API compatibility)
/// </summary>
public static class JintQueryFactory
{
    /// <summary>
    /// Create a query from JSON and schema
    /// </summary>
    public static JintQuery FromJson(QueryJson json, JintSchema schema, string databaseType = "sqlite")
    {
        return new JintQuery(json, schema, databaseType);
    }
}