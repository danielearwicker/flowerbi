// #define STOP_DOCKER_SQL

using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Dapper;
using Microsoft.Data.SqlClient;

namespace FlowerBI.Engine.Tests;

public sealed class SqlServerFixture : IDisposable
{
    private static readonly Mutex _mutex = new(false, $"FlowerBITestSqlServer{VersionString}");

    private static string VersionString => Environment.Version.ToString().Replace('.', '_');

    private static readonly string _containerName = $"FlowerBITestSqlServer{VersionString}";
    private static readonly string _databaseName = $"FlowerBITest{VersionString}";
    private const string _password = "Str0ngPa$$w0rd";
    private static readonly int _port = 61316 + Environment.Version.Major;

    public SqlServerFixture()
    {
        _mutex.WaitOne();
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

        using var db = Connect(false);

        db.Execute("CREATE SCHEMA Testing;");
        db.Execute(SqlScripts.SetupTestingDb);
    }

    public void Dispose()
    {
        DeleteDb();

#if STOP_DOCKER_SQL
        Docker($"kill {_container}");
        Docker($"rm {_container}");
#endif
        _mutex.ReleaseMutex();
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

    public IDbConnection Connect(bool master) =>
        new SqlConnection(
            new SqlConnectionStringBuilder()
            {
                DataSource = $"localhost,{_port}",
                InitialCatalog = master ? "master" : _databaseName,
                UserID = "sa",
                Password = _password,
                Encrypt = false,
                TrustServerCertificate = true,
            }.ConnectionString
        );

    private void CreateDb()
    {
        const int attempts = 100;

        for (var i = 1; i <= attempts; i++)
        {
            try
            {
                using var db = Connect(true);
                db.Execute($"CREATE DATABASE {_databaseName};");
                return;
            }
            catch (SqlException x) when (x.Number == 1801)
            {
                // Database already exists - delete and retry
                DeleteDb();
            }
            catch (Exception) when (i < attempts)
            {
                Thread.Sleep(1000);
            }
        }
    }

    private void DeleteDb()
    {
        using var db = Connect(true);
        db.Execute(
            $"""
            ALTER DATABASE {_databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE {_databaseName};
            """
        );
    }
}
