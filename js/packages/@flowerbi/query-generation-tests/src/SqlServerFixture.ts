import { spawn } from 'child_process';
import { ConnectionPool, config as SqlConfig, Request } from 'mssql';
import { Mutex } from 'async-mutex';
import { sqlScripts } from './SqlScripts';

export class SqlServerFixture {
  private static readonly mutex = new Mutex();
  private static readonly versionString = process.version.replace(/\./g, '_');
  private static readonly containerName = `FlowerBITestSqlServer${SqlServerFixture.versionString}`;
  private static readonly databaseName = `FlowerBITest${SqlServerFixture.versionString}`;
  private static readonly password = 'Str0ngPa$$w0rd';
  private static readonly port = 61316 + parseInt(process.version.split('.')[0].substring(1));
  
  private static containerSetup = false;
  private static setupPromise?: Promise<void>;
  private static instanceCount = 0;

  private pool?: ConnectionPool;

  constructor() {
    // Constructor will be called by Jest, actual setup happens in setup()
  }

  async setup(): Promise<void> {
    const release = await SqlServerFixture.mutex.acquire();
    
    try {
      SqlServerFixture.instanceCount++;
      
      if (!SqlServerFixture.containerSetup) {
        if (!SqlServerFixture.setupPromise) {
          SqlServerFixture.setupPromise = this.doContainerSetup();
        }
        await SqlServerFixture.setupPromise;
        SqlServerFixture.containerSetup = true;
      }
    } finally {
      release();
    }
  }
  
  private async doContainerSetup(): Promise<void> {
    // First check if container is already running or exists
    try {
      const running = await this.docker(`ps -q -f name=^${SqlServerFixture.containerName}$`);
      if (running.trim()) {
        // Container is running, just ensure database exists
        try {
          await this.createDatabase();
          await this.setupTestData();
        } catch (dbError) {
          // Database might already exist, that's fine
        }
        return;
      }
      
      const stopped = await this.docker(`ps -a -q -f name=^${SqlServerFixture.containerName}$`);
      if (stopped.trim()) {
        await this.docker(`start ${SqlServerFixture.containerName}`);
        await this.createDatabase();
        await this.setupTestData();
        return;
      }
    } catch (error) {
      // Error checking container status, proceed with full setup
    }
    // No container exists, do full setup
    await this.startDockerContainer();
    await this.createDatabase();
    await this.setupTestData();
  }

  async teardown(): Promise<void> {
    const release = await SqlServerFixture.mutex.acquire();
    
    try {
      if (this.pool) {
        await this.pool.close();
        this.pool = undefined;
      }
      
      SqlServerFixture.instanceCount--;
      
      // Only tear down container when last instance is finished
      if (SqlServerFixture.instanceCount <= 0 && SqlServerFixture.containerSetup) {
        try {
          await this.docker(`stop ${SqlServerFixture.containerName}`);
          await this.docker(`rm ${SqlServerFixture.containerName}`);
        } catch (error) {
          // If container stop/remove fails, try force cleanup
          try {
            await this.docker(`kill ${SqlServerFixture.containerName}`);
            await this.docker(`rm -f ${SqlServerFixture.containerName}`);
          } catch (forceError) {
            // Container probably doesn't exist, which is fine
          }
        }
        
        SqlServerFixture.containerSetup = false;
        SqlServerFixture.setupPromise = undefined;
        SqlServerFixture.instanceCount = 0;
      }
    } finally {
      release();
    }
  }

  async getConnection(): Promise<ConnectionPool> {
    if (!this.pool) {
      this.pool = await this.createPool(false);
    }
    return this.pool;
  }

  private async startDockerContainer(): Promise<void> {
    // More robust container cleanup - try multiple methods
    try {
      // First try to stop if running
      await this.docker(`stop ${SqlServerFixture.containerName}`);
    } catch (error) {
      // Ignore error if container isn't running
    }
    
    try {
      // Then force remove
      await this.docker(`rm -f ${SqlServerFixture.containerName}`);
    } catch (error) {
      // Ignore error if container doesn't exist
    }
    
    // Double-check no container exists with this name
    try {
      const existing = await this.docker(`ps -a -q -f name=${SqlServerFixture.containerName}`);
      if (existing.trim()) {
        // Container still exists, force kill and remove
        await this.docker(`kill ${SqlServerFixture.containerName}`);
        await this.docker(`rm ${SqlServerFixture.containerName}`);
      }
    } catch (error) {
      // If still failing, container should be gone
    }

    // Create new container
    const args = [
      'run',
      '--name', SqlServerFixture.containerName,
      '-e', 'ACCEPT_EULA=Y',
      '-e', `MSSQL_SA_PASSWORD=${SqlServerFixture.password}`,
      '-p', `${SqlServerFixture.port}:1433`,
      '-d',
      'mcr.microsoft.com/mssql/server:2022-latest'
    ];

    await this.docker(args.join(' '));
  }

  private async createDatabase(): Promise<void> {
    const maxAttempts = 100;
    
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
      try {
        const masterPool = await this.createPool(true);
        const request = new Request(masterPool);
        await request.query(`CREATE DATABASE ${SqlServerFixture.databaseName}`);
        await masterPool.close();
        return;
      } catch (error: any) {
        if (error.number === 1801) {
          // Database already exists - delete and retry
          await this.deleteDatabase();
        } else if (attempt < maxAttempts) {
          // Connection failed - wait and retry
          await this.sleep(1000);
        } else {
          throw error;
        }
      }
    }
  }

  private async deleteDatabase(): Promise<void> {
    try {
      const masterPool = await this.createPool(true);
      const request = new Request(masterPool);
      await request.query(`
        ALTER DATABASE ${SqlServerFixture.databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE ${SqlServerFixture.databaseName};
      `);
      await masterPool.close();
    } catch (error) {
      // Ignore errors when deleting database
    }
  }

  private async setupTestData(): Promise<void> {
    const pool = await this.createPool(false);
    const request = new Request(pool);
    
    await request.query('CREATE SCHEMA Testing;');
    await request.query(sqlScripts.SetupTestingDb);
    
    await pool.close();
  }

  private async createPool(master: boolean): Promise<ConnectionPool> {
    const config: SqlConfig = {
      server: 'localhost',
      port: SqlServerFixture.port,
      database: master ? 'master' : SqlServerFixture.databaseName,
      user: 'sa',
      password: SqlServerFixture.password,
      options: {
        encrypt: false,
        trustServerCertificate: true,
      },
      connectionTimeout: 30000,
      requestTimeout: 30000,
    };

    const pool = new ConnectionPool(config);
    await pool.connect();
    return pool;
  }

  private async docker(cmd: string): Promise<string> {
    return new Promise((resolve, reject) => {
      const dockerProcess = spawn('docker', cmd.split(' '), {
        stdio: ['ignore', 'pipe', 'pipe']
      });

      let stdout = '';
      let stderr = '';

      dockerProcess.stdout?.on('data', (data) => {
        stdout += data.toString();
      });

      dockerProcess.stderr?.on('data', (data) => {
        stderr += data.toString();
      });

      dockerProcess.on('close', (code) => {
        if (process.env.NODE_ENV !== 'test') {
          console.log('Docker stdout:', stdout);
          console.log('Docker stderr:', stderr);
        }
        
        if (code === 0) {
          resolve(stdout);
        } else {
          reject(new Error(`Docker command failed with code ${code}: ${stderr}`));
        }
      });

      dockerProcess.on('error', reject);
    });
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}