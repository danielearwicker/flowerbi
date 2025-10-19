/**
 * Bundle entry point for FlowerBI Query Generation
 * This creates a single JavaScript file suitable for embedding in C# applications
 * 
 * All functionality is exposed through a global FlowerBI object with a simple API
 * designed for easy interop with C# via JSON string communication
 */

// No need for external YAML library - using SimpleYamlParser

// Import all the functionality we need to expose
import { 
  QueryEngine, 
  createQueryEngine,
  PreparedQuery,
  DatabaseResult,
  ArrayOfArraysResult,
  ArrayOfObjectsResult
} from './QueryEngine';

import {
  QueryEngineFactory,
  DatabaseType
} from './QueryEngineFactory';

import {
  QueryEngineHelpers
} from './QueryEngineHelpers';

import { 
  SchemaResolver
} from './schema/SchemaResolver';

import {
  SchemaImplementation
} from './schema/SchemaImplementation';

import {
  ResolvedSchema,
  DataType
} from './schema/YamlSchemaTypes';

import { 
  TypeScriptGenerator
} from './codegen/TypeScriptGenerator';

import {
  CSharpGenerator
} from './codegen/CSharpGenerator';

import {
  QueryJson,
  QueryResultJson,
  FilterJson,
  AggregationJson,
  FlowerBIException
} from './types';

import {
  SqlServerFormatter,
  SqliteFormatter,
  PostgresFormatter,
  NullSqlFormatter
} from './SqlFormatter';

/**
 * Main API interface for C# interop
 * All methods use JSON strings for input/output to simplify marshaling
 */
export interface FlowerBIAPI {
  // Schema operations
  parseSchema(yamlText: string): string; // Returns JSON serialized ResolvedSchema
  
  // Query engine operations  
  createQueryEngine(yamlText: string, databaseType: string): QueryEngine; // Returns QueryEngine object
  prepareQuery(queryJson: string): string; // Returns JSON PreparedQuery  
  mapResults(queryJson: string, databaseResult: string): string; // Returns JSON QueryResultJson
  
  // Code generation
  generateTypeScript(yamlText: string): string; // Returns generated TS code
  generateCSharp(yamlText: string, namespace: string): string; // Returns generated C# code
  
  // Utility functions
  getVersion(): string;
  getSupportedDatabaseTypes(): string[]; // Returns array of supported DB types
  
  // Error handling
  getLastError(): string | null;
}

/**
 * Internal implementation with proper error handling
 */
class FlowerBIImplementation implements FlowerBIAPI {
  private lastError: string | null = null;

  private handleError<T>(operation: () => T): T | null {
    try {
      this.lastError = null;
      return operation();
    } catch (error) {
      this.lastError = error instanceof Error ? error.message : String(error);
      return null;
    }
  }

  parseSchema(yamlText: string): string {
    return this.handleError(() => {
      const schema = SchemaResolver.resolve(yamlText);
      
      // Create a simplified version without circular references for JSON serialization
      // Using camelCase to match JavaScript conventions
      const simplified = {
        name: schema.Name,
        nameInDb: schema.NameInDb,
        tables: schema.Tables.map(table => ({
          name: table.Name,
          nameInDb: table.NameInDb,
          conjoint: table.conjoint,
          idColumn: table.IdColumn ? {
            name: table.IdColumn.Name,
            nameInDb: table.IdColumn.NameInDb,
            dataType: table.IdColumn.DataType,
            nullable: table.IdColumn.Nullable,
            target: table.IdColumn.Target ? {
              name: table.IdColumn.Target.Name,
              tableName: table.IdColumn.Target.Table.Name
            } : undefined
          } : undefined,
          columns: table.Columns.map(col => ({
            name: col.Name,
            nameInDb: col.NameInDb,
            dataType: col.DataType,
            nullable: col.Nullable,
            target: col.Target ? {
              name: col.Target.Name,
              tableName: col.Target.Table.Name
            } : undefined
          })),
          associative: table.Associative.map(assoc => ({
            name: assoc.Name,
            nameInDb: assoc.NameInDb
          }))
        }))
      };
      
      return JSON.stringify(simplified);
    }) || '{}';
  }

  createQueryEngine(yamlText: string, databaseType: string): any {
    const result = this.handleError(() => {
      // Validate database type
      const validTypes: DatabaseType[] = ['sqlserver', 'sqlite', 'postgresql', 'mysql'];
      if (!validTypes.includes(databaseType as DatabaseType)) {
        throw new Error(`Unsupported database type: ${databaseType}`);
      }

      // Create the actual engine
      const engine = QueryEngineFactory.fromYaml({
        yamlSchema: yamlText,
        sqlFormatter: databaseType as DatabaseType
      });
      
      // Return a wrapper object that explicitly exposes the methods
      return {
        prepareQuery: (queryJson: QueryJson) => engine.prepareQuery(queryJson),
        mapResults: (queryJson: QueryJson, databaseResult: DatabaseResult) => engine.mapResults(queryJson, databaseResult)
      };
    });
    
    if (!result) {
      throw new Error(this.lastError || 'Failed to create query engine');
    }
    
    return result;
  }

  // These methods are now on the QueryEngine object directly
  prepareQuery(queryJson: string): string {
    throw new Error('prepareQuery should be called on QueryEngine instance');
  }

  mapResults(queryJson: string, databaseResult: string): string {
    throw new Error('mapResults should be called on QueryEngine instance');
  }

  generateTypeScript(yamlText: string): string {
    return this.handleError(() => {
      const result = TypeScriptGenerator.fromYaml(yamlText);
      if (!result.code) {
        throw new Error('Code generation failed');
      }
      return result.code;
    }) || '';
  }

  generateCSharp(yamlText: string, namespace: string): string {
    return this.handleError(() => {
      const result = CSharpGenerator.fromYaml(yamlText, namespace);
      if (!result.code) {
        throw new Error('Code generation failed');
      }
      return result.code;
    }) || '';
  }

  getVersion(): string {
    return '1.0.0'; // Could be loaded from package.json in real implementation
  }

  getSupportedDatabaseTypes(): string[] {
    return ['sqlserver', 'sqlite', 'postgresql', 'mysql'];
  }

  getLastError(): string | null {
    return this.lastError;
  }
}

// Create the global API instance
const flowerBIAPI = new FlowerBIImplementation();

// Export for module systems
export { flowerBIAPI as FlowerBI };

// Expose as global for direct script inclusion
// Use this instead of globalThis for better compatibility
declare var global: any;
declare var window: any;
declare var self: any;

const globalObj = (function() {
  if (typeof global !== 'undefined') return global;
  if (typeof window !== 'undefined') return window;
  if (typeof self !== 'undefined') return self;
  throw new Error('Unable to locate global object');
})();

globalObj.FlowerBI = flowerBIAPI;

// Export additional utilities that might be needed
export {
  QueryEngineHelpers,
  DataType,
  FlowerBIException
};