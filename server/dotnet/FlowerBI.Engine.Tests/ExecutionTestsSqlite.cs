using System.Data;
using Xunit;

namespace FlowerBI.Engine.Tests;

public class ExecutionTestsSqlite(SqliteFixture Fixture) : ExecutionTests, IClassFixture<SqliteFixture>
{
    protected override IDbConnection Db => Fixture.Db;

    protected override ISqlFormatter Formatter => new SqlLiteFormatter();
}
