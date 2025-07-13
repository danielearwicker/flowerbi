import { DatabaseResult, ArrayOfArraysResult, ArrayOfObjectsResult } from './QueryEngine';

/**
 * Helper functions to convert common database driver result formats
 * to the standardized DatabaseResult format
 */
export class QueryEngineHelpers {
  /**
   * Convert sqlite3 callback results to DatabaseResult
   * sqlite3 returns rows as objects by default
   */
  static fromSqlite3(rows: Record<string, any>[]): ArrayOfObjectsResult {
    return {
      type: 'array-of-objects',
      rows,
    };
  }

  /**
   * Convert mysql2 results to DatabaseResult
   * mysql2 returns [rows, fields] tuple
   */
  static fromMysql2(result: [Record<string, any>[], any[]]): ArrayOfObjectsResult {
    const [rows] = result;
    return {
      type: 'array-of-objects',
      rows,
    };
  }

  /**
   * Convert pg (node-postgres) results to DatabaseResult
   * pg returns a result object with rows property
   */
  static fromPg(result: { rows: Record<string, any>[] }): ArrayOfObjectsResult {
    return {
      type: 'array-of-objects',
      rows: result.rows,
    };
  }

  /**
   * Convert mssql results to DatabaseResult
   * mssql returns a recordset array
   */
  static fromMssql(recordset: Record<string, any>[]): ArrayOfObjectsResult {
    return {
      type: 'array-of-objects',
      rows: recordset,
    };
  }

  /**
   * Convert array-of-arrays format to DatabaseResult
   * Useful for custom drivers or CSV-like data
   */
  static fromArrayOfArrays(columns: string[], rows: any[][]): ArrayOfArraysResult {
    return {
      type: 'array-of-arrays',
      columns,
      rows,
    };
  }

  /**
   * Convert array-of-objects format to DatabaseResult
   * Generic helper for object-based results
   */
  static fromArrayOfObjects(rows: Record<string, any>[]): ArrayOfObjectsResult {
    return {
      type: 'array-of-objects',
      rows,
    };
  }

  /**
   * Handle multiple result sets for totals queries
   * Some drivers return multiple result sets when executing multiple statements
   */
  static fromMultipleResultSets(
    resultSets: Record<string, any>[][]
  ): ArrayOfObjectsResult {
    // Flatten multiple result sets into a single array
    // First result set is totals, second is records
    const allRows = resultSets.flat();
    return {
      type: 'array-of-objects',
      rows: allRows,
    };
  }

  /**
   * Extract column names from array-of-objects result
   * Useful for debugging or when column metadata is needed
   */
  static extractColumnNames(result: ArrayOfObjectsResult): string[] {
    if (result.rows.length === 0) {
      return [];
    }
    return Object.keys(result.rows[0]);
  }

  /**
   * Convert array-of-objects to array-of-arrays format
   * Useful for testing or when specific format is required
   */
  static convertToArrayOfArrays(result: ArrayOfObjectsResult): ArrayOfArraysResult {
    const columns = this.extractColumnNames(result);
    const rows = result.rows.map(row => columns.map(col => row[col]));
    
    return {
      type: 'array-of-arrays',
      columns,
      rows,
    };
  }

  /**
   * Convert array-of-arrays to array-of-objects format
   */
  static convertToArrayOfObjects(result: ArrayOfArraysResult): ArrayOfObjectsResult {
    const rows = result.rows.map(row => {
      const obj: Record<string, any> = {};
      result.columns.forEach((col, index) => {
        obj[col] = row[index];
      });
      return obj;
    });

    return {
      type: 'array-of-objects',
      rows,
    };
  }
}