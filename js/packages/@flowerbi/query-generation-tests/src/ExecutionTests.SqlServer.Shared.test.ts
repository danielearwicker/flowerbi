import { ConnectionPool, Request } from 'mssql';
import { SqlServerFixture } from './SqlServerFixture';
import { ExecutionTestsBase } from './ExecutionTestsBase';
import { ISqlFormatter, SqlServerFormatter } from '@flowerbi/query-generation';
import { runSharedTestSuite } from './SharedTestSuite';

describe('SQL Server Execution Tests', () => {
  let fixture: SqlServerFixture;
  let connection: ConnectionPool;

  beforeAll(async () => {
    fixture = new SqlServerFixture();
    await fixture.setup();
    connection = await fixture.getConnection();
  }, 120000); // 2 minutes timeout for Docker setup

  afterAll(async () => {
    if (fixture) {
      await fixture.teardown();
    }
  }, 60000);

  class SqlServerExecutionTests extends ExecutionTestsBase {
    protected getConnection(): Promise<ConnectionPool> {
      return Promise.resolve(connection);
    }

    protected getFormatter(): ISqlFormatter {
      return new SqlServerFormatter();
    }

    protected async executeQuery(sql: string, params?: any[]): Promise<any[]> {
      const request = new Request(connection);
      
      // Add parameters if provided
      if (params) {
        params.forEach((param, index) => {
          request.input(`param${index}`, param);
        });
      }
      
      const result = await request.query(sql);
      return result.recordset || [];
    }
  }

  const tests = new SqlServerExecutionTests();

  // Run the shared test suite
  runSharedTestSuite(tests, 'SQL Server Database');
});