import { ISqlFormatter } from './types';

export class NullSqlFormatter implements ISqlFormatter {
  static readonly Singleton = new NullSqlFormatter();

  CastToFloat(valueExpr: string): string {
    return '';
  }

  Conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
    return '';
  }

  EscapedIdentifierPair(id1: string, id2: string): string {
    return '';
  }

  Identifier(name: string): string {
    return '';
  }

  SkipAndTake(skip: number, take: number): string {
    return '';
  }

  GetParamPrefix(): string {
    return '';
  }
}

export class SqlServerFormatter implements ISqlFormatter {
  Identifier(name: string): string {
    return `[${name}]`;
  }

  EscapedIdentifierPair(id1: string, id2: string): string {
    return `${id1}.${id2}`;
  }

  SkipAndTake(skip: number, take: number): string {
    return `
            offset ${skip} rows
            fetch next ${take} rows only`;
  }

  Conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
    return `iif(${predExpr}, ${thenExpr}, ${elseExpr})`;
  }

  CastToFloat(valueExpr: string): string {
    return `cast(${valueExpr} as float)`;
  }

  GetParamPrefix(): string {
    return '@';
  }
}

export class SqliteFormatter implements ISqlFormatter {
  Identifier(name: string): string {
    return `"${name}"`; // SQLite uses double quotes for identifiers
  }

  EscapedIdentifierPair(id1: string, id2: string): string {
    return `${id1}.${id2}`;
  }

  SkipAndTake(skip: number, take: number): string {
    return `limit ${take} offset ${skip}`;
  }

  Conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
    return `CASE WHEN ${predExpr} THEN ${thenExpr} ELSE ${elseExpr} END`;
  }

  CastToFloat(valueExpr: string): string {
    return `cast(${valueExpr} as real)`;
  }

  GetParamPrefix(): string {
    return '?';
  }
}

export class PostgresFormatter implements ISqlFormatter {
  Identifier(name: string): string {
    return name;
  }

  EscapedIdentifierPair(id1: string, id2: string): string {
    return `${id1}.${id2}`;
  }

  SkipAndTake(skip: number, take: number): string {
    return `limit ${take} offset ${skip}`;
  }

  Conditional(predExpr: string, thenExpr: string, elseExpr: string): string {
    return `CASE WHEN ${predExpr} THEN ${thenExpr} ELSE ${elseExpr} END`;
  }

  CastToFloat(valueExpr: string): string {
    return `cast(${valueExpr} as real)`;
  }

  GetParamPrefix(): string {
    return '@';
  }
}

export function IdentifierPair(sql: ISqlFormatter, id1: string, id2: string): string {
  return sql.EscapedIdentifierPair(sql.Identifier(id1), sql.Identifier(id2));
}