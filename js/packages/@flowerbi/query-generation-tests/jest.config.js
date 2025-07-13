module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  roots: ['<rootDir>/src'],
  testMatch: ['**/*.test.ts'],
  collectCoverageFrom: [
    'src/**/*.ts',
    '!src/**/*.test.ts',
    '!src/**/*.d.ts',
  ],
  testTimeout: 60000, // 60 seconds for Docker tests
  setupFilesAfterEnv: ['<rootDir>/src/setup.ts'],
  // Force tests to run sequentially to avoid Docker container conflicts
  maxWorkers: 1,
};