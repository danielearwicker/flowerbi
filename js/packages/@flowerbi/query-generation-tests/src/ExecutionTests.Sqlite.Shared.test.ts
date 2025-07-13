import * as sqlite3 from 'sqlite3';
import { SqliteFixture } from './SqliteFixture';
import { ExecutionTestsBase } from './ExecutionTestsBase';
import { ISqlFormatter, SqliteFormatter } from '@flowerbi/query-generation';
import { runSharedTestSuite } from './SharedTestSuite';

describe('SQLite Execution Tests', () => {
  let fixture: SqliteFixture;
  let connection: sqlite3.Database;

  beforeAll(async () => {
    fixture = new SqliteFixture();
    await fixture.setup();
    connection = fixture.getConnection();
  });

  afterAll(async () => {
    if (fixture) {
      await fixture.teardown();
    }
  });

  class SqliteExecutionTests extends ExecutionTestsBase {
    protected getConnection(): Promise<sqlite3.Database> {
      return Promise.resolve(connection);
    }

    protected getFormatter(): ISqlFormatter {
      return new SqliteFormatter();
    }

    protected async executeQuery(sql: string, params?: any[]): Promise<any[]> {
      return fixture.execute(sql, params);
    }
  }

  const tests = new SqliteExecutionTests();

  // Run the shared test suite
  runSharedTestSuite(tests, 'SQLite Database');
});