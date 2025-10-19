import * as fs from 'fs';
import * as path from 'path';
import {
  QueryJson,
  QueryResultJson,
  QueryRecordJson,
  ISqlFormatter,
  Schema,
  Filter,
  Query,
  FlowerBIException,
  AggregationType,
  OrderingType,
} from '@flowerbi/query-generation';
import { SchemaResolver, SchemaImplementation } from '@flowerbi/query-generation';

export abstract class ExecutionTestsBase {
  public log: string[] = [];

  public static findSchemaText(name: string): string {
    const schemasDir = path.join(__dirname, 'schemas');
    const schemaFile = path.join(schemasDir, `${name}.yaml`);
    
    if (fs.existsSync(schemaFile)) {
      return fs.readFileSync(schemaFile, 'utf8');
    }
    
    throw new Error(`Could not find schema file: ${schemaFile}`);
  }

  public static findSchema(name: string): Schema {
    const schemaText = ExecutionTestsBase.findSchemaText(name);
    const resolvedSchema = SchemaResolver.resolve(schemaText);
    return new SchemaImplementation(resolvedSchema);
  }

  public static readonly Schema = ExecutionTestsBase.findSchema('testSchema');
  public static readonly ComplicatedSchema = ExecutionTestsBase.findSchema('complicatedTestSchema');

  protected abstract getConnection(): Promise<any>;
  protected abstract getFormatter(): ISqlFormatter;
  protected abstract executeQuery(sql: string, params?: any[]): Promise<any[]>;

  public async executeFlowerBIQuery(
    json: QueryJson,
    schema: Schema = ExecutionTestsBase.Schema,
    outerFilters: Filter[] = []
  ): Promise<QueryResultJson> {
    this.log = [];
    
    const query = new Query(json, schema);
    const formatter = this.getFormatter();
    
    // Collect parameters during SQL generation
    const filterParams: { [key: string]: any } = {};
    
    const sql = query.toSqlWithComment(formatter, filterParams, outerFilters);
    this.log.push(sql);
    
    // Extract parameter values in the order they appear in the SQL
    const paramValues = Object.keys(filterParams).sort().map(key => filterParams[key]);
    
    try {
      // Handle multiple statements for totals functionality
      const statements = sql.split(';').map(s => s.trim()).filter(s => s.length > 0);
      
      const result: QueryResultJson = {
        records: [],
        totals: undefined,
      };

      if (statements.length === 1) {
        // Single statement - normal query
        const rows = await this.executeQuery(statements[0], paramValues);
        result.records = rows.map(row => ({
          selected: Object.values(row).filter((_, i) => i < (json.select?.length || 0)).map(this.convertValue),
          aggregated: Object.values(row).filter((_, i) => i >= (json.select?.length || 0)).map(this.convertValue),
        }));
      } else if (statements.length === 2) {
        // Two statements - totals + records query
        const [totalsStatement, recordsStatement] = statements;
        
        // Execute totals query first
        const totalsRows = await this.executeQuery(totalsStatement, paramValues);
        if (totalsRows.length > 0) {
          const totalsRow = totalsRows[0];
          // Totals query only has aggregations, no selected columns
          result.totals = {
            selected: [],
            aggregated: Object.values(totalsRow).map(this.convertValue),
          };
        }
        
        // Execute records query second
        const recordsRows = await this.executeQuery(recordsStatement, paramValues);
        result.records = recordsRows.map(row => ({
          selected: Object.values(row).filter((_, i) => i < (json.select?.length || 0)).map(this.convertValue),
          aggregated: Object.values(row).filter((_, i) => i >= (json.select?.length || 0)).map(this.convertValue),
        }));
      } else {
        throw new FlowerBIException(`Unexpected number of SQL statements: ${statements.length}`);
      }
      
      return result;
    } catch (error) {
      throw new FlowerBIException(`SQL execution failed: ${error}`);
    }
  }

  public static round(value: any): any {
    if (typeof value === 'number') {
      return Math.round(value * 10000) / 10000; // Round to 4 decimal places like the C# version
    }
    return value;
  }

  private convertValue(value: any): any {
    // Convert string numbers to actual numbers to match C# behavior
    if (typeof value === 'string' && !isNaN(Number(value)) && value.trim() !== '') {
      const num = Number(value);
      // Only convert if it's a valid integer or decimal
      if (Number.isInteger(num) || (num % 1 !== 0 && num.toString() === value)) {
        return num;
      }
    }
    return value;
  }

  // Test helper methods
  public expectRecordsToEqual(actual: QueryRecordJson[], expected: any[]): void {
    const actualMapped = actual.map(record => {
      const selected = record.selected;
      const aggregated = record.aggregated?.map(ExecutionTestsBase.round);
      return selected.length === 1 && aggregated?.length === 1
        ? [selected[0], aggregated[0]]
        : [...selected, ...(aggregated || [])];
    });

    expect(actualMapped).toEqual(expect.arrayContaining(expected));
  }

  public expectToBeInDescendingOrder(values: any[]): void {
    for (let i = 1; i < values.length; i++) {
      if (typeof values[i] === 'string' && typeof values[i - 1] === 'string') {
        expect(values[i].localeCompare(values[i - 1])).toBeLessThanOrEqual(0);
      } else {
        expect(values[i]).toBeLessThanOrEqual(values[i - 1]);
      }
    }
  }

  public expectToBeInAscendingOrder(values: any[]): void {
    for (let i = 1; i < values.length; i++) {
      if (typeof values[i] === 'string' && typeof values[i - 1] === 'string') {
        expect(values[i].localeCompare(values[i - 1])).toBeGreaterThanOrEqual(0);
      } else {
        expect(values[i]).toBeGreaterThanOrEqual(values[i - 1]);
      }
    }
  }
}