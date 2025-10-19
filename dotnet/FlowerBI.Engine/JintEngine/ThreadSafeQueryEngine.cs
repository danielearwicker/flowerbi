using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Jint.Native;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI.JintEngine;

/// <summary>
/// Thread-safe query engine that manages its own pool of Jint engines.
/// Designed to be stored in static readonly fields and shared across multiple threads.
/// Each instance manages engines for a specific schema+database combination.
/// </summary>
public class ThreadSafeQueryEngine : IDisposable
{
    private readonly string _bundlePath;
    private readonly string _yamlText;
    private readonly string _databaseType;
    private readonly ConcurrentBag<PooledJintEngine> _availableEngines = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly object _disposeLock = new();
    private volatile bool _disposed;
    
    // Configuration - scale pool with CPU cores
    private static readonly int MaxEngines = Math.Max(Environment.ProcessorCount, 4);
    private static readonly int InitialEngines = Math.Min(2, MaxEngines);

    /// <summary>
    /// Create a thread-safe query engine for the specified schema and database type.
    /// This instance can be safely stored in static fields and accessed from multiple threads.
    /// </summary>
    /// <param name="yamlText">YAML schema definition</param>
    /// <param name="databaseType">Database type (sqlite, sqlserver, postgresql, mysql)</param>
    /// <param name="bundlePath">Path to FlowerBI JavaScript bundle, or null for auto-detection</param>
    public ThreadSafeQueryEngine(string yamlText, string databaseType, string bundlePath = null)
    {
        _yamlText = yamlText ?? throw new ArgumentNullException(nameof(yamlText));
        _databaseType = databaseType ?? throw new ArgumentNullException(nameof(databaseType));
        _bundlePath = bundlePath; // null means use embedded resource
        _semaphore = new SemaphoreSlim(MaxEngines, MaxEngines);
        
        // Pre-create initial engines to avoid cold start delays
        for (int i = 0; i < InitialEngines; i++)
        {
            var engine = CreatePooledEngine();
            _availableEngines.Add(engine);
        }
    }

    /// <summary>
    /// Prepare a query for execution (thread-safe)
    /// </summary>
    public async Task<PreparedQuery> PrepareQueryAsync(QueryJson query)
    {
        return await ExecuteAsync(engine => engine.PrepareQuery(query));
    }

    /// <summary>
    /// Map database results to FlowerBI format (thread-safe)
    /// </summary>
    public async Task<QueryResultJson> MapResultsAsync(QueryJson query, DatabaseResult databaseResult)
    {
        return await ExecuteAsync(engine => engine.MapResults(query, databaseResult));
    }

    /// <summary>
    /// Synchronous version of PrepareQuery for compatibility
    /// </summary>
    public PreparedQuery PrepareQuery(QueryJson query)
    {
        return PrepareQueryAsync(query).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronous version of MapResults for compatibility
    /// </summary>
    public QueryResultJson MapResults(QueryJson query, DatabaseResult databaseResult)
    {
        return MapResultsAsync(query, databaseResult).GetAwaiter().GetResult();
    }

    private async Task<T> ExecuteAsync<T>(Func<PooledJintEngine, T> operation)
    {
        ThrowIfDisposed();
        
        await _semaphore.WaitAsync();
        try
        {
            var engine = GetOrCreateEngine();
            try
            {
                return operation(engine);
            }
            finally
            {
                // Return engine to pool if still valid
                if (!_disposed && engine.IsValid)
                {
                    _availableEngines.Add(engine);
                }
                else
                {
                    engine.Dispose();
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private PooledJintEngine GetOrCreateEngine()
    {
        // Try to get an existing engine from pool
        if (_availableEngines.TryTake(out var engine) && engine.IsValid)
        {
            return engine;
        }
        
        // Create new engine if needed
        return CreatePooledEngine();
    }

    private PooledJintEngine CreatePooledEngine()
    {
        var jintEngine = new FlowerBIJintEngine(_bundlePath);
        var queryEngineRef = jintEngine.CreateQueryEngine(_yamlText, _databaseType);
        return new PooledJintEngine(jintEngine, queryEngineRef);
    }


    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ThreadSafeQueryEngine));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_disposeLock)
        {
            if (_disposed) return;
            
            _disposed = true;
            
            // Dispose all engines in pool
            while (_availableEngines.TryTake(out var engine))
            {
                engine.Dispose();
            }
            
            _semaphore.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Wrapper for a FlowerBI Jint engine instance with its associated query engine reference
/// </summary>
internal class PooledJintEngine : IDisposable
{
    private readonly FlowerBIJintEngine _jintEngine;
    private readonly JsValue _queryEngineRef;
    private volatile bool _disposed;

    public bool IsValid => !_disposed && _jintEngine != null;

    public PooledJintEngine(FlowerBIJintEngine jintEngine, JsValue queryEngineRef)
    {
        _jintEngine = jintEngine ?? throw new ArgumentNullException(nameof(jintEngine));
        _queryEngineRef = queryEngineRef ?? throw new ArgumentNullException(nameof(queryEngineRef));
    }

    public PreparedQuery PrepareQuery(QueryJson query)
    {
        ThrowIfDisposed();
        return _jintEngine.PrepareQuery(_queryEngineRef, query);
    }

    public QueryResultJson MapResults(QueryJson query, DatabaseResult databaseResult)
    {
        ThrowIfDisposed();
        return _jintEngine.MapResults(_queryEngineRef, query, databaseResult);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PooledJintEngine));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _jintEngine?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Convenient static factory methods for creating thread-safe query engines
/// </summary>
public static class FlowerBIThreadSafe
{
    /// <summary>
    /// Create a thread-safe query engine suitable for storing in static fields
    /// </summary>
    public static ThreadSafeQueryEngine CreateQueryEngine(string yamlText, string databaseType, string bundlePath = null)
    {
        return new ThreadSafeQueryEngine(yamlText, databaseType, bundlePath);
    }

    /// <summary>
    /// Parse YAML schema (creates temporary engine, not suitable for repeated use)
    /// </summary>
    public static string ParseSchema(string yamlText, string bundlePath = null)
    {
        using var engine = new FlowerBIJintEngine(bundlePath);
        return engine.ParseSchema(yamlText);
    }

    /// <summary>
    /// Generate TypeScript code (creates temporary engine, not suitable for repeated use)
    /// </summary>
    public static string GenerateTypeScript(string yamlText, string bundlePath = null)
    {
        using var engine = new FlowerBIJintEngine(bundlePath);
        return engine.GenerateTypeScript(yamlText);
    }

    /// <summary>
    /// Generate C# code (creates temporary engine, not suitable for repeated use)
    /// </summary>
    public static string GenerateCSharp(string yamlText, string namespaceName, string bundlePath = null)
    {
        using var engine = new FlowerBIJintEngine(bundlePath);
        return engine.GenerateCSharp(yamlText, namespaceName);
    }

    /// <summary>
    /// Get supported database types
    /// </summary>
    public static string[] GetSupportedDatabaseTypes(string bundlePath = null)
    {
        using var engine = new FlowerBIJintEngine(bundlePath);
        return engine.GetSupportedDatabaseTypes();
    }

    /// <summary>
    /// Get FlowerBI version
    /// </summary>
    public static string GetVersion(string bundlePath = null)
    {
        using var engine = new FlowerBIJintEngine(bundlePath);
        return engine.GetVersion();
    }

}