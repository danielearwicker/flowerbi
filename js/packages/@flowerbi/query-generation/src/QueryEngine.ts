import { QueryJson, QueryResultJson, QueryRecordJson, ISqlFormatter, IFilterParameters, Schema } from './types';
import { Query } from './Query';
import { Filter } from './Filter';

/**
 * Represents the result of query preparation - SQL and parameters ready for execution
 */
export interface PreparedQuery {
  /** The SQL query string to execute */
  sql: string;
  /** Parameters to pass to the database driver */
  parameters: any[];
}

/**
 * Raw database result formats that different drivers might return
 */
export type DatabaseResult = 
  | ArrayOfArraysResult
  | ArrayOfObjectsResult;

/**
 * Result format where first array contains column names, subsequent arrays contain values
 * Common in drivers like sqlite3
 */
export interface ArrayOfArraysResult {
  type: 'array-of-arrays';
  columns: string[];
  rows: any[][];
}

/**
 * Result format where each record is an object with named properties
 * Common in drivers like mysql2, pg, mssql
 */
export interface ArrayOfObjectsResult {
  type: 'array-of-objects';
  rows: Record<string, any>[];
}

/**
 * Abstract base class for query engines that handle specific database drivers
 */
export abstract class QueryEngine {
  protected readonly schema: Schema;
  protected readonly formatter: ISqlFormatter;

  constructor(schema: Schema, formatter: ISqlFormatter) {
    this.schema = schema;
    this.formatter = formatter;
  }

  /**
   * Prepare a query for execution by generating SQL and extracting parameters
   */
  public prepareQuery(
    queryJson: QueryJson, 
    outerFilters: Filter[] = []
  ): PreparedQuery {
    const query = new Query(queryJson, this.schema);
    const filterParams: IFilterParameters = {};
    
    const sql = query.toSqlWithComment(this.formatter, filterParams, outerFilters);
    
    // Extract parameter values in sorted order for consistent parameter binding
    const parameters = Object.keys(filterParams)
      .sort()
      .map(key => filterParams[key]);
    
    return { sql, parameters };
  }

  /**
   * Map raw database results to FlowerBI's QueryResultJson format
   */
  public mapResults(
    queryJson: QueryJson,
    result: DatabaseResult
  ): QueryResultJson {
    const rows = this.extractRows(result);
    
    // Handle multiple statements (totals + records)
    if (queryJson.Totals && this.hasTotalsQuery(queryJson)) {
      return this.mapTotalsResults(queryJson, rows);
    } else {
      return this.mapRecordsOnly(queryJson, rows);
    }
  }

  /**
   * Extract raw row data from different result formats
   */
  private extractRows(result: DatabaseResult): any[][] {
    switch (result.type) {
      case 'array-of-arrays':
        return result.rows;
      case 'array-of-objects':
        return result.rows.map(row => Object.values(row));
      default:
        throw new Error(`Unsupported result type: ${(result as any).type}`);
    }
  }

  /**
   * Check if the query generates totals SQL (contains semicolon for multiple statements)
   */
  private hasTotalsQuery(queryJson: QueryJson): boolean {
    return queryJson.Totals === true;
  }

  /**
   * Map results when totals are requested (first row is totals, rest are records)
   */
  private mapTotalsResults(queryJson: QueryJson, rows: any[][]): QueryResultJson {
    if (rows.length === 0) {
      return { Records: [], Totals: undefined };
    }

    // First row is totals (only aggregations, no selected columns)
    const totalsRow = rows[0];
    const totals: QueryRecordJson = {
      Selected: [],
      Aggregated: totalsRow.map(this.convertValue),
    };

    // Remaining rows are records
    const recordRows = rows.slice(1);
    const records = recordRows.map(row => this.mapRecord(queryJson, row));

    return {
      Records: records,
      Totals: totals,
    };
  }

  /**
   * Map results when only records are requested (no totals)
   */
  private mapRecordsOnly(queryJson: QueryJson, rows: any[][]): QueryResultJson {
    const records = rows.map(row => this.mapRecord(queryJson, row));
    return {
      Records: records,
      Totals: undefined,
    };
  }

  /**
   * Map a single row to a QueryRecordJson
   */
  private mapRecord(queryJson: QueryJson, row: any[]): QueryRecordJson {
    const selectCount = queryJson.Select?.length || 0;
    
    return {
      Selected: row.slice(0, selectCount).map(this.convertValue),
      Aggregated: row.slice(selectCount).map(this.convertValue),
    };
  }

  /**
   * Convert database values to appropriate JavaScript types
   * Can be overridden by specific engine implementations
   */
  protected convertValue(value: any): any {
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

  /**
   * Round numeric values to 4 decimal places like the C# version
   */
  public static round(value: any): any {
    if (typeof value === 'number') {
      return Math.round(value * 10000) / 10000;
    }
    return value;
  }
}

/**
 * Factory function to create a QueryEngine from a YAML schema
 */
export function createQueryEngine(
  schema: Schema, 
  formatter: ISqlFormatter
): QueryEngine {
  return new StandardQueryEngine(schema, formatter);
}

/**
 * Standard implementation of QueryEngine
 */
class StandardQueryEngine extends QueryEngine {
  // Uses base implementation - can be extended for specific database behaviors
}