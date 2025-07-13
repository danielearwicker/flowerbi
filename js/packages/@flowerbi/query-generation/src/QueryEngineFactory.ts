import { QueryEngine, createQueryEngine } from './QueryEngine';
import { SchemaResolver } from './schema/SchemaResolver';
import { SchemaImplementation } from './schema/SchemaImplementation';
import { ISqlFormatter } from './types';
import { SqlServerFormatter, SqliteFormatter, PostgresFormatter } from './SqlFormatter';

/**
 * Database types supported by the factory
 */
export type DatabaseType = 'sqlserver' | 'sqlite' | 'postgresql' | 'mysql';

/**
 * SQL formatter specification - either a string name or custom implementation
 */
export type SqlFormatterSpec = DatabaseType | ISqlFormatter;

/**
 * Configuration for creating a QueryEngine
 */
export interface QueryEngineConfig {
  /** The SQL formatter to use - either a database type string or custom ISqlFormatter */
  sqlFormatter: SqlFormatterSpec;
  /** YAML schema text defining the database structure */
  yamlSchema: string;
}

/**
 * Factory class for creating QueryEngine instances
 */
export class QueryEngineFactory {
  /**
   * Create a QueryEngine from YAML schema and SQL formatter specification
   */
  static fromYaml(config: QueryEngineConfig): QueryEngine {
    // Parse the YAML schema
    const resolvedSchema = SchemaResolver.resolve(config.yamlSchema);
    const schema = new SchemaImplementation(resolvedSchema);

    // Resolve the SQL formatter
    const formatter = this.resolveFormatter(config.sqlFormatter);

    // Create and return the query engine
    return createQueryEngine(schema, formatter);
  }

  /**
   * Create a QueryEngine from an already resolved schema
   */
  static fromSchema(schema: SchemaImplementation, sqlFormatter: SqlFormatterSpec): QueryEngine {
    const formatter = this.resolveFormatter(sqlFormatter);
    return createQueryEngine(schema, formatter);
  }

  /**
   * Resolve a SqlFormatterSpec to an actual ISqlFormatter instance
   */
  private static resolveFormatter(spec: SqlFormatterSpec): ISqlFormatter {
    // If it's already an ISqlFormatter object, return it directly
    if (typeof spec === 'object' && 'GetParamPrefix' in spec) {
      return spec;
    }
    
    // Otherwise, treat it as a database type string
    return this.getDefaultFormatter(spec as DatabaseType);
  }

  /**
   * Get the default SQL formatter for a database type
   */
  private static getDefaultFormatter(databaseType: DatabaseType): ISqlFormatter {
    switch (databaseType) {
      case 'sqlserver':
        return new SqlServerFormatter();
      case 'sqlite':
        return new SqliteFormatter();
      case 'postgresql':
        return new PostgresFormatter();
      case 'mysql':
        // MySQL uses similar syntax to PostgreSQL for most operations
        return new PostgresFormatter();
      default:
        throw new Error(`Unsupported database type: ${databaseType}`);
    }
  }

  /**
   * Create a QueryEngine with custom configuration
   * @deprecated Use fromYaml() with QueryEngineConfig instead
   */
  static create(
    yamlSchema: string,
    formatter: ISqlFormatter
  ): QueryEngine {
    return this.fromYaml({
      yamlSchema,
      sqlFormatter: formatter,
    });
  }
}

/**
 * Convenience function to create a QueryEngine from YAML
 * Supports both string database types and custom ISqlFormatter implementations
 */
export function createQueryEngineFromYaml(
  yamlSchema: string,
  sqlFormatter: SqlFormatterSpec
): QueryEngine {
  return QueryEngineFactory.fromYaml({
    yamlSchema,
    sqlFormatter,
  });
}