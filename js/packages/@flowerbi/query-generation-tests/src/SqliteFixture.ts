import * as sqlite3 from 'sqlite3';
import { tmpdir } from 'os';
import { join } from 'path';
import { unlink } from 'fs/promises';
import { sqlScripts } from './SqlScripts';

export class SqliteFixture {
  private db?: sqlite3.Database;
  private readonly filenames: string[];

  constructor() {
    this.filenames = [
      join(tmpdir(), `flowerbi-test-${Date.now()}-${Math.random()}.db`),
      join(tmpdir(), `flowerbi-test-${Date.now()}-${Math.random()}-testing.db`)
    ];
  }

  async setup(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.db = new sqlite3.Database(this.filenames[0], (err) => {
        if (err) {
          reject(err);
          return;
        }

        // Attach the testing database
        this.db!.run(`ATTACH '${this.filenames[1]}' AS Testing;`, (err) => {
          if (err) {
            reject(err);
            return;
          }

          // Setup test data
          this.db!.exec(sqlScripts.SetupTestingDb, (err) => {
            if (err) {
              reject(err);
            } else {
              resolve();
            }
          });
        });
      });
    });
  }

  async teardown(): Promise<void> {
    return new Promise((resolve) => {
      if (this.db) {
        this.db.close((err) => {
          // Ignore close errors
          this.cleanupFiles().then(() => resolve());
        });
      } else {
        this.cleanupFiles().then(() => resolve());
      }
    });
  }

  private async cleanupFiles(): Promise<void> {
    // Clean up temp files
    for (const filename of this.filenames) {
      try {
        await unlink(filename);
      } catch (error) {
        // Ignore errors when deleting temp files
      }
    }
  }

  getConnection(): sqlite3.Database {
    if (!this.db) {
      throw new Error('SQLite fixture not initialized. Call setup() first.');
    }
    return this.db;
  }

  async execute(sql: string, params?: any[]): Promise<any[]> {
    const db = this.getConnection();
    
    return new Promise((resolve, reject) => {
      // CTE queries start with WITH, but should be treated as SELECT queries
      if (sql.trim().toUpperCase().startsWith('SELECT') || sql.trim().toUpperCase().startsWith('WITH')) {
        if (params) {
          db.all(sql, params, (err, rows) => {
            if (err) reject(err);
            else resolve(rows || []);
          });
        } else {
          db.all(sql, (err, rows) => {
            if (err) reject(err);
            else resolve(rows || []);
          });
        }
      } else {
        if (params) {
          db.run(sql, params, (err) => {
            if (err) reject(err);
            else resolve([]);
          });
        } else {
          db.run(sql, (err) => {
            if (err) reject(err);
            else resolve([]);
          });
        }
      }
    });
  }
}