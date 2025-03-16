using System;
using System.Data;
using Xunit;

namespace FlowerBI.Engine.Tests;

public class ExecutionTestsSqlServer(SqlServerFixture Fixture)
    : ExecutionTests,
        IClassFixture<SqlServerFixture>
{
    protected override Func<IDbConnection> Db => () => Fixture.Connect(false);

    protected override ISqlFormatter Formatter => new SqlServerFormatter();
}
