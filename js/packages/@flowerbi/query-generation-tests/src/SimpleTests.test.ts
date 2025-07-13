import { SqliteFixture } from './SqliteFixture';
import { SqlServerFixture } from './SqlServerFixture';

describe('Infrastructure Tests', () => {
  describe('SQLite Infrastructure', () => {
    let fixture: SqliteFixture;

    beforeAll(async () => {
      fixture = new SqliteFixture();
      await fixture.setup();
    });

    afterAll(async () => {
      if (fixture) {
        await fixture.teardown();
      }
    });

    test('SQLite database setup and basic query', async () => {
      const results = await fixture.execute('SELECT COUNT(*) as count FROM Testing.Supplier');
      expect(results).toHaveLength(1);
      expect(results[0].count).toBe(14); // Number of suppliers in test data
    });

    test('SQLite aggregation query', async () => {
      const results = await fixture.execute(`
        SELECT s.VendorName, SUM(i.FancyAmount) as total 
        FROM Testing.Supplier s 
        INNER JOIN Testing.Invoice i ON s.Id = i.VendorId 
        GROUP BY s.VendorName 
        ORDER BY total DESC 
        LIMIT 3
      `);
      
      expect(results).toHaveLength(3);
      expect(results[0].VendorName).toBe('United Cheese');
      expect(parseFloat(results[0].total)).toBeCloseTo(406.84, 2);
    });
  });

  describe('SQL Server Infrastructure', () => {
    let fixture: SqlServerFixture;

    beforeAll(async () => {
      fixture = new SqlServerFixture();
      await fixture.setup();
    }, 120000); // 2 minutes timeout for Docker setup

    afterAll(async () => {
      if (fixture) {
        await fixture.teardown();
      }
    }, 60000); // 1 minute timeout for teardown

    test('SQL Server database setup and basic query', async () => {
      const connection = await fixture.getConnection();
      const request = connection.request();
      const result = await request.query('SELECT COUNT(*) as count FROM Testing.Supplier');
      
      expect(result.recordset).toHaveLength(1);
      expect(result.recordset[0].count).toBe(14);
    });

    test('SQL Server aggregation query', async () => {
      const connection = await fixture.getConnection();
      const request = connection.request();
      const result = await request.query(`
        SELECT s.VendorName, SUM(i.FancyAmount) as total 
        FROM Testing.Supplier s 
        INNER JOIN Testing.Invoice i ON s.Id = i.VendorId 
        GROUP BY s.VendorName 
        ORDER BY total DESC
        OFFSET 0 ROWS FETCH NEXT 3 ROWS ONLY
      `);
      
      expect(result.recordset).toHaveLength(3);
      expect(result.recordset[0].VendorName).toBe('United Cheese');
      expect(parseFloat(result.recordset[0].total)).toBeCloseTo(406.84, 2);
    });
  });
});