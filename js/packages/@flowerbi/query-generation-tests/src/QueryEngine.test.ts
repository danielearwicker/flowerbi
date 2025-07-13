import { 
  createQueryEngineFromYaml, 
  QueryEngineHelpers,
  DatabaseResult,
  QueryJson,
  QueryEngine,
  SqlServerFormatter,
  ISqlFormatter
} from '@flowerbi/query-generation';
import { ExecutionTestsBase } from './ExecutionTestsBase';

describe('QueryEngine', () => {
  const yamlSchema = ExecutionTestsBase.findSchemaText('testSchema');
  const engine = createQueryEngineFromYaml(yamlSchema, 'sqlite');

  test('prepareQuery generates SQL and parameters', () => {
    const queryJson: QueryJson = {
      Select: ['Vendor.VendorName'],
      Aggregations: [{ Function: 'Sum' as any, Column: 'Invoice.Amount' }],
      Filters: [
        {
          Column: 'Vendor.Id',
          Operator: '=',
          Value: 2,
        },
      ],
    };

    const prepared = engine.prepareQuery(queryJson);
    
    expect(prepared.sql.toLowerCase()).toContain('select');
    expect(prepared.sql).toContain('Supplier'); // The actual table name in schema
    expect(prepared.sql).toContain('Invoice');
    expect(prepared.parameters).toEqual([2]);
  });

  test('mapResults handles array-of-objects format', () => {
    const queryJson: QueryJson = {
      Select: ['Vendor.VendorName'],
      Aggregations: [{ Function: 'Sum' as any, Column: 'Invoice.Amount' }],
    };

    const mockRows = [
      { VendorName: 'Test Vendor', Amount: 100.50 },
      { VendorName: 'Another Vendor', Amount: 200.25 },
    ];

    const dbResult = QueryEngineHelpers.fromArrayOfObjects(mockRows);
    const mapped = engine.mapResults(queryJson, dbResult);

    expect(mapped.Records).toHaveLength(2);
    expect(mapped.Records[0].Selected).toEqual(['Test Vendor']);
    expect(mapped.Records[0].Aggregated).toEqual([100.50]);
    expect(mapped.Records[1].Selected).toEqual(['Another Vendor']);
    expect(mapped.Records[1].Aggregated).toEqual([200.25]);
    expect(mapped.Totals).toBeUndefined();
  });

  test('mapResults handles totals query', () => {
    const queryJson: QueryJson = {
      Select: ['Vendor.VendorName'],
      Aggregations: [{ Function: 'Sum' as any, Column: 'Invoice.Amount' }],
      Totals: true,
    };

    const mockRows = [
      { TotalAmount: 300.75 }, // Totals row (only aggregations)
      { VendorName: 'Test Vendor', Amount: 100.50 }, // Records
      { VendorName: 'Another Vendor', Amount: 200.25 },
    ];

    const dbResult = QueryEngineHelpers.fromArrayOfObjects(mockRows);
    const mapped = engine.mapResults(queryJson, dbResult);

    expect(mapped.Totals).toBeDefined();
    expect(mapped.Totals!.Selected).toEqual([]);
    expect(mapped.Totals!.Aggregated).toEqual([300.75]);
    
    expect(mapped.Records).toHaveLength(2);
    expect(mapped.Records[0].Selected).toEqual(['Test Vendor']);
    expect(mapped.Records[0].Aggregated).toEqual([100.50]);
  });

  test('mapResults handles array-of-arrays format', () => {
    const queryJson: QueryJson = {
      Select: ['Vendor.VendorName'],
      Aggregations: [{ Function: 'Count' as any, Column: 'Invoice.Id' }],
    };

    const dbResult: DatabaseResult = {
      type: 'array-of-arrays',
      columns: ['VendorName', 'InvoiceCount'],
      rows: [
        ['Test Vendor', 5],
        ['Another Vendor', 3],
      ],
    };

    const mapped = engine.mapResults(queryJson, dbResult);

    expect(mapped.Records).toHaveLength(2);
    expect(mapped.Records[0].Selected).toEqual(['Test Vendor']);
    expect(mapped.Records[0].Aggregated).toEqual([5]);
    expect(mapped.Records[1].Selected).toEqual(['Another Vendor']);
    expect(mapped.Records[1].Aggregated).toEqual([3]);
  });

  test('QueryEngineHelpers converts formats correctly', () => {
    const objectsData = [
      { name: 'John', age: 30 },
      { name: 'Jane', age: 25 },
    ];

    const objectsResult = QueryEngineHelpers.fromArrayOfObjects(objectsData);
    expect(objectsResult.type).toBe('array-of-objects');
    expect(objectsResult.rows).toEqual(objectsData);

    const arraysResult = QueryEngineHelpers.convertToArrayOfArrays(objectsResult);
    expect(arraysResult.type).toBe('array-of-arrays');
    expect(arraysResult.columns).toEqual(['name', 'age']);
    expect(arraysResult.rows).toEqual([['John', 30], ['Jane', 25]]);

    const backToObjects = QueryEngineHelpers.convertToArrayOfObjects(arraysResult);
    expect(backToObjects.rows).toEqual(objectsData);
  });

  test('round function works like C# version', () => {
    expect(QueryEngine.round(123.456789)).toBe(123.4568);
    expect(QueryEngine.round(123.12345)).toBe(123.1235);
    expect(QueryEngine.round(123)).toBe(123);
    expect(QueryEngine.round('not a number')).toBe('not a number');
  });

  test('accepts custom SQL formatter', () => {
    const customFormatter = new SqlServerFormatter();
    const customEngine = createQueryEngineFromYaml(yamlSchema, customFormatter);

    const queryJson: QueryJson = {
      Select: ['Vendor.VendorName'],
      Aggregations: [{ Function: 'Count' as any, Column: 'Invoice.Id' }],
    };

    const prepared = customEngine.prepareQuery(queryJson);
    
    // SQL Server formatter uses @ prefix for parameters and [] for identifiers
    expect(prepared.sql).toContain('[Testing].[Supplier]');
    expect(prepared.sql).toContain('[Testing].[Invoice]');
  });

  test('accepts custom ISqlFormatter implementation', () => {
    class TestFormatter implements ISqlFormatter {
      GetParamPrefix(): string {
        return '$';
      }

      EscapedIdentifierPair(first: string, second: string): string {
        return `"${first}"."${second}"`;
      }

      Identifier(name: string): string {
        return `"${name}"`;
      }

      SkipAndTake(skip: number, take: number): string {
        return `LIMIT ${take} OFFSET ${skip}`;
      }

      Conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
        return `CASE WHEN ${predExpr} THEN ${thenExpr} ELSE ${elseExpr} END`;
      }

      CastToFloat(valueExpr: string): string {
        return `CAST(${valueExpr} AS FLOAT)`;
      }
    }

    const testEngine = createQueryEngineFromYaml(yamlSchema, new TestFormatter());

    const queryJson: QueryJson = {
      Select: ['Vendor.VendorName'],
      Aggregations: [{ Function: 'Count' as any, Column: 'Invoice.Id' }],
    };

    const prepared = testEngine.prepareQuery(queryJson);
    
    // Custom formatter uses double quotes and LIMIT/OFFSET
    expect(prepared.sql).toContain('""Testing""');
    expect(prepared.sql).toContain('""Invoice""');
    expect(prepared.sql).toContain('LIMIT 100 OFFSET 0');
  });
});