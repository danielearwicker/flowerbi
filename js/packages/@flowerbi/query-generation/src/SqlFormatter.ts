import { ISqlFormatter } from './types';

export class NullSqlFormatter implements ISqlFormatter {
  static readonly Singleton = new NullSqlFormatter();

  castToFloat(valueExpr: string): string {
    return '';
  }

  conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
    return '';
  }

  escapedIdentifierPair(id1: string, id2: string): string {
    return '';
  }

  identifier(name: string): string {
    return '';
  }

  skipAndTake(skip: number, take: number): string {
    return '';
  }

  getParamPrefix(): string {
    return '';
  }
}

export class SqlServerFormatter implements ISqlFormatter {
  identifier(name: string): string {
    return `[${name}]`;
  }

  escapedIdentifierPair(id1: string, id2: string): string {
    return `${id1}.${id2}`;
  }

  skipAndTake(skip: number, take: number): string {
    return `
            offset ${skip} rows
            fetch next ${take} rows only`;
  }

  conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
    return `iif(${predExpr}, ${thenExpr}, ${elseExpr})`;
  }

  castToFloat(valueExpr: string): string {
    return `cast(${valueExpr} as float)`;
  }

  getParamPrefix(): string {
    return '@';
  }
}

export class SqliteFormatter implements ISqlFormatter {
  identifier(name: string): string {
    return `"${name}"`; // SQLite uses double quotes for identifiers
  }

  escapedIdentifierPair(id1: string, id2: string): string {
    return `${id1}.${id2}`;
  }

  skipAndTake(skip: number, take: number): string {
    return `limit ${take} offset ${skip}`;
  }

  conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
    return `CASE WHEN ${predExpr} THEN ${thenExpr} ELSE ${elseExpr} END`;
  }

  castToFloat(valueExpr: string): string {
    return `cast(${valueExpr} as real)`;
  }

  getParamPrefix(): string {
    return '?';
  }
}

export class PostgresFormatter implements ISqlFormatter {
  identifier(name: string): string {
    return name;
  }

  escapedIdentifierPair(id1: string, id2: string): string {
    return `${id1}.${id2}`;
  }

  skipAndTake(skip: number, take: number): string {
    return `limit ${take} offset ${skip}`;
  }

  conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
    return `CASE WHEN ${predExpr} THEN ${thenExpr} ELSE ${elseExpr} END`;
  }

  castToFloat(valueExpr: string): string {
    return `cast(${valueExpr} as real)`;
  }

  getParamPrefix(): string {
    return '@';
  }
}

export function IdentifierPair(sql: ISqlFormatter, id1: string, id2: string): string {
  return sql.escapedIdentifierPair(sql.identifier(id1), sql.identifier(id2));
}