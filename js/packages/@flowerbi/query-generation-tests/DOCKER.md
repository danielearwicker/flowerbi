# Docker Setup and Troubleshooting

## Fixed: Container Conflict Issue ✅

The Docker container conflict issue has been resolved with these improvements:

1. **Sequential Test Execution**: Jest now runs tests with `maxWorkers: 1` to prevent parallel container creation
2. **Shared Container State**: Multiple test suites now properly share the same SQL Server container
3. **Robust Container Detection**: The system detects existing containers and reuses them instead of creating conflicts

## Container Cleanup (if needed)

If you still encounter issues, use these cleanup commands:

### Quick Cleanup (Recommended)
```bash
npm run cleanup
```

### Manual Cleanup
```bash
./cleanup-docker.sh
```

### Clean Test Run
```bash
npm run test:clean
```

## How It Works

- **Container Sharing**: Both `ExecutionTests.SqlServer.test.ts` and `ComprehensiveExecutionTests.SqlServer.test.ts` share the same container
- **Sequential Execution**: Jest runs test files one at a time to prevent race conditions
- **Smart Detection**: The system checks for existing containers before creating new ones
- **Automatic Cleanup**: Containers are properly removed when all tests complete

## Container Names
Test containers are named with Node.js version to avoid conflicts:
- `FlowerBITestSqlServerv18_x_x` (for Node 18)
- `FlowerBITestSqlServerv20_x_x` (for Node 20)

## Port Assignment
Each Node.js version uses a different port:
- Node 18: Port 61334
- Node 20: Port 61336
- etc.

## Troubleshooting Steps (if issues persist)
1. Run `npm run cleanup`
2. Verify no FlowerBI containers exist: `docker ps -a | grep FlowerBI`
3. Run tests: `npm test`
4. If still failing, restart Docker Desktop and repeat

## Status
✅ **Docker container conflicts are now resolved**
✅ **Tests run sequentially without interference**
✅ **Container sharing works properly between test suites**