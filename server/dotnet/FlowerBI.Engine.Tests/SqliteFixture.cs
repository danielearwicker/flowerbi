using System;
using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace FlowerBI.Engine.Tests;

public sealed class SqliteFixture : IDisposable
{
    public Func<IDbConnection> Db { get; }

    private readonly string[] _filenames = [Path.GetTempFileName(), Path.GetTempFileName()];

    public SqliteFixture()
    {
        Db = () => new SqliteConnection($"Data Source={_filenames[0]}");

        using var db = Db();
        db.Execute(
            $"""
            ATTACH '{_filenames[1]}' AS Testing;

            {SqlScripts.SetupTestingDb}
            """
        );
    }

    public void Dispose()
    {
        foreach (var filename in _filenames)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }
    }
}
