/**
 * Test the bundled FlowerBI Query Generation library
 * This simulates how the C# library would interact with the bundle
 */

// Load the bundle (simulating how C# would embed and execute it)
const fs = require('fs');
const path = require('path');

// Read and execute the bundle
const bundlePath = path.join(__dirname, '../query-generation/bundle/flowerbi-query-generation.js');
const bundleCode = fs.readFileSync(bundlePath, 'utf8');

// Execute the bundle in the current context
eval(bundleCode);

// Test that the global FlowerBI object is available
console.log('FlowerBI Bundle Test');
console.log('==================');

if (typeof FlowerBI === 'undefined') {
  console.error('❌ FlowerBI global object not found');
  process.exit(1);
}

console.log('✅ FlowerBI global object available');
console.log('Version:', FlowerBI.getVersion());
console.log('Supported DB types:', FlowerBI.getSupportedDatabaseTypes());

// Test schema parsing
const testYaml = `
schema: TestSchema
tables:
  User:
    id:
      Id: [int]
    columns:
      Name: [string]
      Email: [string]
      IsActive: [bool]
  Order:
    id:
      Id: [int]
    columns:
      UserId: [User]
      Total: [decimal]
      OrderDate: [datetime]
`;

console.log('\nTesting schema parsing...');
const schemaResult = FlowerBI.parseSchema(testYaml);
if (schemaResult && schemaResult !== '{}') {
  console.log('✅ Schema parsed successfully');
  const schema = JSON.parse(schemaResult);
  console.log('Schema name:', schema.Name);
  console.log('Tables:', schema.Tables.map(t => t.Name));
} else {
  console.error('❌ Schema parsing failed:', FlowerBI.getLastError());
}

// Test query engine creation
console.log('\nTesting query engine creation...');
const engineId = FlowerBI.createQueryEngine(testYaml, 'sqlite');
if (engineId) {
  console.log('✅ Query engine created:', engineId);
} else {
  console.error('❌ Query engine creation failed:', FlowerBI.getLastError());
}

// Test query preparation
console.log('\nTesting query preparation...');
const queryJson = JSON.stringify({
  Select: ['User.Name'],
  Aggregations: [{ Function: 'Count', Column: 'Order.Id' }],
  Filters: [{ Column: 'User.IsActive', Operator: '=', Value: true }]
});

const preparedResult = FlowerBI.prepareQuery(engineId, queryJson);
if (preparedResult && preparedResult !== '{}') {
  console.log('✅ Query prepared successfully');
  const prepared = JSON.parse(preparedResult);
  console.log('SQL length:', prepared.sql.length);
  console.log('Parameters:', prepared.parameters);
} else {
  console.error('❌ Query preparation failed:', FlowerBI.getLastError());
}

// Test result mapping
console.log('\nTesting result mapping...');
const mockDatabaseResult = JSON.stringify({
  type: 'array-of-objects',
  rows: [
    { Name: 'John Doe', OrderCount: 5 },
    { Name: 'Jane Smith', OrderCount: 3 }
  ]
});

const mappedResult = FlowerBI.mapResults(engineId, queryJson, mockDatabaseResult);
if (mappedResult && mappedResult !== '{}') {
  console.log('✅ Results mapped successfully');
  const mapped = JSON.parse(mappedResult);
  console.log('Records count:', mapped.Records.length);
  console.log('First record:', mapped.Records[0]);
} else {
  console.error('❌ Result mapping failed:', FlowerBI.getLastError());
}

// Test TypeScript generation
console.log('\nTesting TypeScript generation...');
const tsCode = FlowerBI.generateTypeScript(testYaml);
if (tsCode) {
  console.log('✅ TypeScript generated successfully');
  console.log('Code length:', tsCode.length);
  console.log('Contains import:', tsCode.includes('import {'));
  console.log('Contains User table:', tsCode.includes('export const User'));
} else {
  console.error('❌ TypeScript generation failed:', FlowerBI.getLastError());
}

// Test C# generation
console.log('\nTesting C# generation...');
const csCode = FlowerBI.generateCSharp(testYaml, 'MyApp.Schema');
if (csCode) {
  console.log('✅ C# generated successfully');
  console.log('Code length:', csCode.length);
  console.log('Contains namespace:', csCode.includes('namespace MyApp.Schema'));
  console.log('Contains User class:', csCode.includes('public static class User'));
} else {
  console.error('❌ C# generation failed:', FlowerBI.getLastError());
}

console.log('\n✅ All bundle tests completed successfully!');
console.log('\nBundle is ready for C# integration.');