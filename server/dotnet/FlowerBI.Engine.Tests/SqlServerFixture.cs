// #define STOP_DOCKER_SQL

using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using Dapper;
using Microsoft.Data.SqlClient;

namespace FlowerBI.Engine.Tests;

public sealed class SqlServerFixture : IDisposable
{
    public Func<IDbConnection> Db { get; }

    private static string VersionString => Environment.Version.ToString().Replace('.', '_');

    private static readonly string _containerName = $"FlowerBITestSqlServer{VersionString}";
    private static readonly string _databaseName = $"FlowerBITest{VersionString}";
    private const string _password = "Str0ngPa$$w0rd";
    private static readonly int _port = 61316 + Environment.Version.Major;

    public SqlServerFixture()
    {
        Docker(
            string.Join(
                " ",
                [
                    "run",
                    "--name",
                    _containerName,
                    "-e",
                    "\"ACCEPT_EULA=Y\"",
                    "-e",
                    $"\"MSSQL_SA_PASSWORD={_password}\"",
                    "-p",
                    $"{_port}:1433",
                    "-d",
                    "mcr.microsoft.com/mssql/server:2022-latest",
                ]
            )
        );

        CreateDb();

        _cs.InitialCatalog = _databaseName;

        Db = () => new SqlConnection(_cs.ConnectionString);

        using var db = Db();
        db.Execute("CREATE SCHEMA Testing;");
        db.Execute(SqlScripts.SetupTestingDb);
    }

    public void Dispose()
    {
#if STOP_DOCKER_SQL
        Docker($"kill {_container}");
        Docker($"rm {_container}");
#endif
    }

    private static void Docker(string cmd)
    {
        using var proc = Process.Start(
            new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = cmd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        );

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

            Console.WriteLine($"Creating database {_databaseName}...");

            try
            {
                db.Execute($"CREATE DATABASE {_databaseName};");

                Console.WriteLine($"Created database {_databaseName}.");

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
        Console.WriteLine($"Deleting database {_databaseName}...");

        db.Execute(
            $"""
            ALTER DATABASE {_databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE {_databaseName};
            """
        );

        Console.WriteLine($"Deleted database {_databaseName}.");
    }
}
