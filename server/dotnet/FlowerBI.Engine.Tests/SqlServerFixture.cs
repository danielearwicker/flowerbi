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

    // How long to wait for SQL Server inside a freshly-started container to accept
    // connections. First boot of the image genuinely takes ~20-40s.
    private static readonly TimeSpan _sqlStartupTimeout = TimeSpan.FromSeconds(120);

    public SqlServerFixture()
    {
        EnsureDockerReachable();
        EnsureContainerRunning();

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
        Docker($"kill {_containerName}");
        Docker($"rm {_containerName}");
#endif
    }

    /// <summary>
    /// Fails fast with an actionable message if the Docker daemon can't be reached, rather
    /// than letting a missing daemon turn into a silent multi-minute connection-retry spin.
    /// </summary>
    private static void EnsureDockerReachable()
    {
        var (exitCode, _, stderr) = Docker("version --format {{.Server.Version}}");
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                "Cannot reach the Docker daemon, so the SQL Server integration tests cannot "
                    + "start their container. Is Docker running? Under WSL, check that Docker "
                    + "Desktop is running and that WSL integration is enabled for this distro "
                    + "(Docker Desktop > Settings > Resources > WSL Integration).\n\n"
                    + $"`docker version` exited with code {exitCode}:\n{stderr.Trim()}"
            );
        }
    }

    /// <summary>
    /// Ensures a container named <see cref="_containerName"/> is running, reusing an existing
    /// one (started if stopped) so reruns are fast, or creating it otherwise. Idempotent: a
    /// leftover container from a previous run no longer causes `docker run` to fail silently.
    /// </summary>
    private static void EnsureContainerRunning()
    {
        var (_, running, _) = Docker(
            $"inspect -f {{{{.State.Running}}}} {_containerName}"
        );

        var state = running.Trim();
        if (state == "true")
        {
            return; // already up - reuse it
        }

        if (state == "false")
        {
            // Container exists but is stopped - start it rather than failing on the name clash.
            var (startExit, _, startErr) = Docker($"start {_containerName}");
            if (startExit != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to start existing container '{_containerName}':\n{startErr.Trim()}"
                );
            }
            return;
        }

        // No such container (inspect failed) - create it.
        var (runExit, _, runErr) = Docker(
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

        if (runExit != 0)
        {
            throw new InvalidOperationException(
                $"Failed to start SQL Server container '{_containerName}':\n{runErr.Trim()}"
            );
        }
    }

    private static (int ExitCode, string StdOut, string StdErr) Docker(string cmd)
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

        proc.WaitForExit();

        Debug.WriteLine(stdout);
        Debug.WriteLine(stderr);

        return (proc.ExitCode, stdout, stderr);
    }

    private readonly SqlConnectionStringBuilder _cs = new()
    {
        DataSource = $"localhost,{_port}",
        InitialCatalog = "master",
        UserID = "sa",
        Password = _password,
        Encrypt = false,
        TrustServerCertificate = true,
        // Keep each attempt short; the container is already confirmed running by this point,
        // so we're only waiting for SQL Server itself to finish starting up.
        ConnectTimeout = 5,
    };

    private void CreateDb()
    {
        var stopwatch = Stopwatch.StartNew();
        Exception lastError = null;

        while (stopwatch.Elapsed < _sqlStartupTimeout)
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
            catch (Exception x)
            {
                lastError = x;
                Debug.WriteLine($"Retrying soon due to {x.GetBaseException().Message}");
                Thread.Sleep(1000);
            }
        }

        throw new InvalidOperationException(
            $"SQL Server in container '{_containerName}' did not accept connections within "
                + $"{_sqlStartupTimeout.TotalSeconds:0}s on {_cs.DataSource}. Inspect the "
                + $"container logs with `docker logs {_containerName}`.",
            lastError
        );
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
