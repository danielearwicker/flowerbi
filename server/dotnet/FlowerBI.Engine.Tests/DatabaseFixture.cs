//#define START_DOCKER_SQL
//#define STOP_DOCKER_SQL

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Diagnostics;
using System.Threading;

namespace FlowerBI.Engine.Tests
{
    public sealed class DatabaseFixture : IDisposable
    {
        public IDbConnection Db { get; }

        private const string _container = "FlowerBITestSql";
        private const string _password = "5tr0ng-P@55w0rd";
        private const int _port = 1433;

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
            DataSource = "localhost",
            InitialCatalog = "master",
            UserID = "sa",
            Password = "5tr0ng-P@55w0rd",
            Encrypt = false,
            TrustServerCertificate = true,
        };

        public DatabaseFixture()
        {
            
#if START_DOCKER_SQL
            var startCommand = string.Join(" ", [
                "run",
                "--name", _container,
                "-e", "\"ACCEPT_EULA=Y\"",
                "-e", $"\"MSSQL_SA_PASSWORD={_password}\"",
                "-p", $"{_port}:{_port}",
                "-d", "mcr.microsoft.com/mssql/server:2022-latest"
            ]);
            
            Docker(startCommand);
#endif
            for (var i = 0; i < 10; i++) 
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
                    Thread.Sleep(2000);
                }
            }

            _cs.InitialCatalog = "FlowerBITest";

            Db = new SqlConnection(_cs.ConnectionString);

            Db.Execute(
                """
                CREATE SCHEMA Testing;
                """);

            Db.Execute(
                """
                CREATE TABLE Testing.Department (
                    Id INT NOT NULL,
                    DepartmentName NVARCHAR(50) NOT NULL,
                    PRIMARY KEY (Id)
                );

                INSERT INTO Testing.Department (Id, DepartmentName) VALUES 
                    (1, 'Accounts'),
                    (2, 'Marketing'),
                    (3, 'Missiles'),
                    (4, 'Cheese'),
                    (5, 'Yoga'),
                    (6, 'Sales');
                
                CREATE TABLE Testing.Supplier (
                    Id INT NOT NULL,
                    VendorName NVARCHAR(50) NOT NULL,
                    DepartmentId INT NOT NULL,
                    PRIMARY KEY(Id)
                );

                INSERT INTO Testing.Supplier (Id, VendorName, DepartmentId) VALUES 
                    (1,  'Acme Ltd', 4),
                    (2,  'Manchesterford Supplies Inc', 4),
                    (3,  'Party Hats 4 U', 2),
                    (4,  'United Cheese', 4),
                    (5,  'Mats and More', 5),
                    (6,  'Uranium 4 Less', 3),
                    (7,  'Tiles Tiles Tiles', 2),
                    (8,  'Steve Makes Sandwiches', 4),
                    (9,  'Handbags-a-Plenty', 3),
                    (10, 'Awnings-R-Us', 4),
                    (11, 'Disgusting Ltd', 5),
                    (12, 'Statues While You Wait', 4),
                    (13, 'Stationary Stationery', 1),
                    (14, 'Pleasant Plc', 3);

                CREATE TABLE Testing.Invoice (
                    Id INT NOT NULL,
                    VendorId INT NOT NULL,
                    DepartmentId INT NOT NULL,
                    FancyAmount DECIMAL(10, 2) NOT NULL,
                    Paid BIT NULL,
                    PRIMARY KEY(Id)
                );

                INSERT INTO Testing.Invoice (Id, VendorId, DepartmentId, FancyAmount, Paid) VALUES 
                    (1,  14, 2, 88.12, NULL),
                    (2,  4,  6, 18.12, NULL),
                    (3,  7,  5, 68.12, 1),
                    (4,  4,  3, 58.12, 1),
                    (5,  9,  6, 38.12, NULL),
                    (6,  9,  3, 88.12, NULL),
                    (7,  8,  1, 88.12, 1),
                    (8,  4,  4, 98.12, NULL),
                    (9,  3,  2, 58.12, NULL),
                    (10, 9,  6, 28.12, NULL),
                    (11, 12, 4, 78.12, 0),
                    (12, 10, 2, 88.12, 1),
                    (13, 5,  6, 58.12, NULL),
                    (14, 4,  5, 38.12, 1),
                    (15, 11, 1, 68.12, NULL),
                    (16, 7,  4, 38.12, NULL),
                    (17, 5,  2, 18.12, NULL),
                    (18, 8,  5, 88.12, 1),
                    (19, 12, 3, 78.12, 1),
                    (20, 9,  3, 98.12, NULL),
                    (21, 4,  3, 88.12, NULL),
                    (22, 2,  5, 58.12, NULL),
                    (23, 2,  6, 88.12, 1),
                    (24, 4,  5, 38.12, 0),
                    (25, 11, 5, 88.12, NULL),
                    (26, 13, 2, 28.12, NULL),
                    (27, 2,  1, 18.12, 1),
                    (28, 6,  1, 88.12, NULL),
                    (29, 4,  4, 68.12, NULL);
                
                CREATE TABLE Testing.Tag (
                    Id INT NOT NULL,
                    TagName NVARCHAR(50) NOT NULL,
                    PRIMARY KEY (Id)
                );

                INSERT INTO Testing.Tag (Id, TagName) VALUES
                    (1, 'Interesting'),
                    (2, 'Boring'),
                    (3, 'Lethal');

                CREATE TABLE Testing.InvoiceTag (
                    InvoiceId INT NOT NULL,
                    TagId INT NOT NULL,
                    PRIMARY KEY(InvoiceId, TagId)
                );

                INSERT INTO Testing.InvoiceTag (InvoiceId, TagId) VALUES
                    (4,  1),
                    (4,  3),
                    (9,  2),
                    (10, 2),
                    (11, 2),
                    (18, 1),
                    (20, 1),
                    (20, 2),
                    (20, 3);

                CREATE TABLE Testing.Category (
                    Id INT NOT NULL,
                    CategoryName NVARCHAR(50) NOT NULL,
                    PRIMARY KEY (Id)
                );

                INSERT INTO Testing.Category (Id, CategoryName) VALUES
                    (1, 'Special'),
                    (2, 'Regular'),
                    (3, 'Illegal');

                CREATE TABLE Testing.InvoiceCategory (
                    InvoiceId INT NOT NULL,
                    CategoryId INT NOT NULL,
                    PRIMARY KEY(InvoiceId, CategoryId)
                );

                INSERT INTO Testing.InvoiceCategory (InvoiceId, CategoryId) VALUES
                    (3,  1),
                    (7,  3),
                    (8,  2),
                    (10, 2),
                    (11, 2),
                    (13, 1),
                    (15, 1),
                    (24, 2),
                    (24, 3);

                CREATE TABLE Testing.AnnotationName (
                    Id INT NOT NULL,
                    Name NVARCHAR(50) NOT NULL,
                    PRIMARY KEY (Id)
                );

                INSERT INTO Testing.AnnotationName (Id, Name) VALUES
                    (1, 'Approver'),
                    (2, 'Instructions');

                CREATE TABLE Testing.AnnotationValue (
                    Id INT NOT NULL,
                    AnnotationNameId INT NOT NULL,
                    Value NVARCHAR(50) NOT NULL,
                    PRIMARY KEY (Id)
                );

                INSERT INTO Testing.AnnotationValue (Id, AnnotationNameId, Value) VALUES
                    (1, 1, 'Jill'),
                    (2, 1, 'Gupta'),
                    (3, 1, 'Snarvu'),
                    (4, 2, 'Pay quickly'),
                    (5, 2, 'Brown envelope job'),
                    (6, 2, 'Cash only'),
                    (7, 2, 'Meet me behind the tree');

                CREATE TABLE Testing.InvoiceAnnotation (
                    InvoiceId INT NOT NULL,
                    AnnotationValueId INT NOT NULL,
                    PRIMARY KEY (InvoiceId, AnnotationValueId)
                )
                """                
            );
        }

        public void Dispose()
        {
            Db?.Dispose();

#if STOP_DOCKER_SQL
            Docker($"kill {_container}");
            Docker($"rm {_container}");
#else
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
}