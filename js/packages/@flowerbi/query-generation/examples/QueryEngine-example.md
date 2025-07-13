# QueryEngine Usage Examples

## Basic Usage

```typescript
import { createQueryEngineFromYaml, QueryEngineHelpers } from '@flowerbi/query-generation';

// Create a query engine from YAML schema
const yamlSchema = `
schema: TestSchema
tables:
  Vendor:
    id:
      Id: [int]
    columns:
      VendorName: [string]
  Invoice:
    id:
      Id: [int]
    columns:
      VendorId: [Vendor]
      Amount: [decimal]
`;

// Using a string database type
const engine = createQueryEngineFromYaml(yamlSchema, 'sqlite');

// Prepare a query
const queryJson = {
  Select: ['Vendor.VendorName'],
  Aggregations: [{ Function: 'Sum', Column: 'Invoice.Amount' }],
};

const prepared = engine.prepareQuery(queryJson);
console.log('SQL:', prepared.sql);
console.log('Parameters:', prepared.parameters);

// Execute with your database driver (example with sqlite3)
import sqlite3 from 'sqlite3';

const db = new sqlite3.Database(':memory:');
db.all(prepared.sql, prepared.parameters, (err, rows) => {
  if (err) throw err;
  
  // Convert driver result to standard format
  const result = QueryEngineHelpers.fromSqlite3(rows);
  
  // Map to FlowerBI result format
  const mapped = engine.mapResults(queryJson, result);
  console.log('Results:', mapped);
});
```

## Working with Different Database Drivers

### SQLite3
```typescript
import sqlite3 from 'sqlite3';

// After executing query...
db.all(sql, params, (err, rows) => {
  const result = QueryEngineHelpers.fromSqlite3(rows);
  const mapped = engine.mapResults(queryJson, result);
});
```

### MySQL2
```typescript
import mysql from 'mysql2/promise';

const connection = await mysql.createConnection(config);
const results = await connection.execute(sql, params);
const result = QueryEngineHelpers.fromMysql2(results);
const mapped = engine.mapResults(queryJson, result);
```

### PostgreSQL (pg)
```typescript
import { Client } from 'pg';

const client = new Client(config);
const result = await client.query(sql, params);
const dbResult = QueryEngineHelpers.fromPg(result);
const mapped = engine.mapResults(queryJson, dbResult);
```

### SQL Server (mssql)
```typescript
import sql from 'mssql';

const pool = new sql.ConnectionPool(config);
const request = pool.request();
params.forEach((param, i) => request.input(`param${i}`, param));
const result = await request.query(sql);
const dbResult = QueryEngineHelpers.fromMssql(result.recordset);
const mapped = engine.mapResults(queryJson, dbResult);
```

## Custom Result Format

```typescript
// If you have a custom result format
const customRows = [
  { name: 'John', age: 30 },
  { name: 'Jane', age: 25 }
];

const result = QueryEngineHelpers.fromArrayOfObjects(customRows);
const mapped = engine.mapResults(queryJson, result);
```

## Custom SQL Formatters

```typescript
import { 
  createQueryEngineFromYaml, 
  QueryEngineFactory,
  SqlServerFormatter,
  ISqlFormatter 
} from '@flowerbi/query-generation';

// Using a string database type
const engine1 = createQueryEngineFromYaml(yamlSchema, 'postgresql');

// Using a custom SQL formatter instance
const customFormatter = new SqlServerFormatter();
const engine2 = createQueryEngineFromYaml(yamlSchema, customFormatter);

// Creating your own custom formatter
class CustomSqlFormatter implements ISqlFormatter {
  GetParamPrefix(): string {
    return '$'; // PostgreSQL-style parameters
  }

  EscapedIdentifierPair(first: string, second: string): string {
    return `"${first}"."${second}"`;
  }

  Identifier(name: string): string {
    return `"${name}"`; // Double-quoted identifiers
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

const engine3 = createQueryEngineFromYaml(yamlSchema, new CustomSqlFormatter());
```

## Factory Methods

```typescript
import { QueryEngineFactory } from '@flowerbi/query-generation';

// From YAML with string database type
const engine1 = QueryEngineFactory.fromYaml({
  sqlFormatter: 'postgresql',
  yamlSchema: yamlText,
});

// From YAML with custom formatter
const engine2 = QueryEngineFactory.fromYaml({
  sqlFormatter: new SqlServerFormatter(),
  yamlSchema: yamlText,
});

// From existing schema
import { SchemaResolver, SchemaImplementation } from '@flowerbi/query-generation';
const resolvedSchema = SchemaResolver.resolve(yamlText);
const schema = new SchemaImplementation(resolvedSchema);
const engine3 = QueryEngineFactory.fromSchema(schema, 'sqlite');
```

## Handling Totals Queries

```typescript
const queryWithTotals = {
  Select: ['Vendor.VendorName'],
  Aggregations: [{ Function: 'Sum', Column: 'Invoice.Amount' }],
  Totals: true, // This generates two SQL statements
};

const prepared = engine.prepareQuery(queryWithTotals);
// prepared.sql will contain two statements separated by semicolon

// When mapping results, the engine automatically handles the format:
// - First row(s) are totals (only aggregated values)  
// - Remaining rows are records (selected + aggregated values)
const mapped = engine.mapResults(queryWithTotals, result);
console.log('Totals:', mapped.Totals);
console.log('Records:', mapped.Records);
```