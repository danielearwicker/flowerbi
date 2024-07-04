// #define STOP_DOCKER_SQL

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Diagnostics;
using System.Threading;

namespace FlowerBI.Engine.Tests;

public sealed class SqlServerFixture : IDisposable
{
    public IDbConnection Db { get; }

    private const string _containerName = "FlowerBITestSqlServer";
    private const string _databaseName = "FlowerBITest";
    private const string _password = "Str0ngPa$$w0rd";
    private const int _port = 61316;

    public SqlServerFixture()
    {
        Docker(string.Join(" ", [
            "run",
            "--name", _containerName,
            "-e", "\"ACCEPT_EULA=Y\"",
            "-e", $"\"MSSQL_SA_PASSWORD={_password}\"",
            "-p", $"{_port}:1433",
            "-d", "mcr.microsoft.com/mssql/server:2022-latest"
        ]));

        CreateDb();

        _cs.InitialCatalog = _databaseName;

        Db = new SqlConnection(_cs.ConnectionString);

        Db.Execute("CREATE SCHEMA Testing;");
        Db.Execute(SqlScripts.SetupTestingDb);
    }

    public void Dispose()
    {
        Db?.Dispose();

#if STOP_DOCKER_SQL
        Docker($"kill {_container}");
        Docker($"rm {_container}");
#endif      
    }

    private static void Docker(string cmd)
    {
        using var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = cmd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });

        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();

        Debug.WriteLine(stdout);
        Debug.WriteLine(stderr);

        proc.WaitForExit();
    }

    private readonly SqlConnectionStringBuilder _cs = new()
    {
        DataSource = $"localhost,{_port}",
        InitialCatalog = "master",
        UserID = "sa",
        Password = _password,
        Encrypt = false,
        TrustServerCertificate = true,
    };

    private void CreateDb()
    {
        const int attempts = 100;

        for (var i = 1; i <= attempts; i++) 
        {
            using var db = new SqlConnection(_cs.ConnectionString);

            try
            {
                db.Execute($"CREATE DATABASE {_databaseName};");
                return;
            }
            catch (SqlException x) when (x.Number == 1801)
            {
                // Database already exists - delete and retry
                DeleteDb(db);
            }
            catch (Exception x) when (i < attempts)
            {
                Debug.WriteLine($"Retrying soon due to {x.GetBaseException().Message}");
                Thread.Sleep(1000);
            }
        }
    }

    private static void DeleteDb(IDbConnection db)
    {
        db.Execute(
            $"""
            ALTER DATABASE {_databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE {_databaseName};
            """
        );
    }
}
