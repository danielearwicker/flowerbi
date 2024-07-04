// Run once with START_DOCKER_SQL and CREATE_TEST_DB defined to set everything up
// then commment them back out to repeat-run tests with no setup cost.

#define START_DOCKER_SQL
#define CREATE_TEST_DB
#define DELETE_TEST_DB
#define STOP_DOCKER_SQL

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

    private const string _container = "FlowerBITestSqlServer";
    private const string _password = "Str0ngPa$$w0rd";
    private const int _port = 61316;

    private const string _setup = """

    """;

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

    public SqlServerFixture()
    {
        
#if START_DOCKER_SQL
        var startCommand = string.Join(" ", [
            "run",
            "--name", _container,
            "-e", "\"ACCEPT_EULA=Y\"",
            "-e", $"\"MSSQL_SA_PASSWORD={_password}\"",
            "-p", $"{_port}:1433",
            "-d", "mcr.microsoft.com/mssql/server:2022-latest"
        ]);
        
        Docker(startCommand);
#endif

#if CREATE_TEST_DB
        for (var i = 0; i < 100; i++) 
        {
            using var db = new SqlConnection(_cs.ConnectionString);

            try
            {
                db.Execute(
                    """
                    CREATE DATABASE FlowerBITest;
                    """
                );

                break;
            }
            catch (Exception x)
            {
                Debug.WriteLine($"Retrying soon due to {x.GetBaseException().Message}");
                Thread.Sleep(1000);
            }
        }
#endif
        _cs.InitialCatalog = "FlowerBITest";

        Db = new SqlConnection(_cs.ConnectionString);

#if CREATE_TEST_DB
        Db.Execute(
            """
            CREATE SCHEMA Testing;
            """);

        Db.Execute(SqlScripts.SetupTestingDb);
#endif            
    }

    public void Dispose()
    {
        Db?.Dispose();

#if STOP_DOCKER_SQL
        Docker($"kill {_container}");
        Docker($"rm {_container}");
#elif DELETE_TEST_DB
        _cs.InitialCatalog = "master";

        using var db = new SqlConnection(_cs.ConnectionString);

        db.Execute(
            """
            ALTER DATABASE FlowerBITest SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE FlowerBITest;
            """
        );
#endif
    }
}