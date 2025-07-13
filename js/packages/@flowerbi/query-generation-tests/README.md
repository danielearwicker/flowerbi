# @flowerbi/query-generation-tests

Integration tests for the FlowerBI query generation library, supporting both SQL Server (via Docker) and SQLite databases.

## Overview

This package provides a comprehensive test infrastructure that replicates the .NET integration test setup. It includes Docker-based SQL Server testing and SQLite in-memory testing with the same database schema and test data as the original .NET implementation.

## ✅ Current Status

- **✅ Complete Test Infrastructure**: Docker fixtures, SQLite fixtures, test base classes
- **✅ Database Setup**: All test tables, data, and schemas ported from .NET  
- **✅ SQLite Tests Working**: Infrastructure tests pass, database operations verified
- **✅ SQL Server Docker Integration**: Container management, database lifecycle
- **⚠️ Schema Integration**: Mock schema needs full implementation for query generation tests
- **⚠️ Requires Docker**: SQL Server tests need Docker daemon running

## Features

- **SQL Server Integration**: Uses Docker to spin up SQL Server 2022 instances for testing
- **SQLite Integration**: Uses in-memory SQLite databases for fast testing
- **Shared Test Suite**: Same tests run against both database types to ensure consistency
- **Real Database Execution**: Tests execute actual SQL against real databases
- **Comprehensive Coverage**: Includes complex joins, aggregations, filters, and edge cases

## Prerequisites

### For SQL Server Tests
- Docker installed and running
- Access to pull `mcr.microsoft.com/mssql/server:2022-latest` image

### For SQLite Tests
- No additional prerequisites (uses in-memory database)

## Usage

```bash
# Run all tests (includes infrastructure tests)
npm test

# Run working infrastructure tests only
npx jest src/SimpleTests.test.ts

# Run SQL Server tests (requires Docker)
npm run test:sqlserver

# Run SQLite tests  
npm run test:sqlite

# Watch mode
npm run test:watch
```

## Working Tests

The infrastructure is fully functional. Run these to verify:

```bash
# Test SQLite database setup and queries
npx jest src/SimpleTests.test.ts --testNamePattern="SQLite"

# Test SQL Server (if Docker is running)  
npx jest src/SimpleTests.test.ts --testNamePattern="SQL Server"
```

## Next Steps

To complete the full integration test suite:

1. **Implement Schema Parser**: Create a proper YAML schema parser that converts the test schemas into the required TypeScript Schema interface
2. **Connect Schema to Query Engine**: Update the mock schema to properly represent table relationships and foreign keys
3. **Verify Query Generation**: Once the schema is properly implemented, the existing ExecutionTests should work end-to-end

## Test Structure

### Database Fixtures

- **`SqlServerFixture`**: Manages Docker container lifecycle, database creation/cleanup
- **`SqliteFixture`**: Manages temporary SQLite database files
- **`ExecutionTestsBase`**: Abstract base class with shared test logic

### Test Categories

1. **Basic Queries**: Simple selects, aggregations, filters
2. **Complex Joins**: Many-to-many relationships, conjoint tables
3. **Aggregation Functions**: Sum, Count, Min, Max, CountDistinct
4. **Filtering**: Various operators, parameter handling, edge cases
5. **Ordering**: Column-based and index-based sorting
6. **Error Handling**: Invalid queries, empty filters, SQL injection prevention

## Database Schema

Tests use a comprehensive schema with:
- Vendors/Suppliers with departments
- Invoices with amounts and payment status
- Many-to-many relationships via tags and categories
- Hierarchical annotation system
- Complex join scenarios

## Docker Integration

SQL Server tests automatically:
1. Start containerized SQL Server instance
2. Create isolated test database
3. Execute schema setup scripts
4. Run tests against real SQL Server
5. Clean up resources

The Docker integration uses mutexes to prevent conflicts when running tests in parallel.

## Performance

- **SQLite Tests**: Very fast, suitable for development
- **SQL Server Tests**: Slower due to Docker startup, but more comprehensive
- **Parallel Execution**: Fixtures use locking to support parallel test runs

## Configuration

Test timeouts and database connection strings can be configured via:
- Jest configuration in `jest.config.js`
- Environment variables for connection details
- Docker container configuration in `SqlServerFixture`

## Troubleshooting

### Docker Issues
```bash
# Check if Docker is running
docker ps

# Pull SQL Server image manually
docker pull mcr.microsoft.com/mssql/server:2022-latest

# Check for port conflicts
netstat -an | grep 61316
```

### Test Failures
- Ensure Docker has sufficient resources (2GB+ RAM)
- Check firewall settings for SQL Server port access
- Verify schema files exist in `src/schemas/` directory

## Development

To add new tests:
1. Add test cases to both SQL Server and SQLite test files
2. Use the `ExecutionTestsBase` helper methods for assertions
3. Follow existing patterns for database-agnostic test logic
4. Update schema files if new tables/relationships are needed