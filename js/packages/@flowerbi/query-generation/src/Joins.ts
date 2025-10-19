import { ISqlFormatter, LabelledTable, Table, Schema, IForeignKey, IColumn, LabelledColumn, FlowerBIException } from './types';
import { IdentifierPair } from './SqlFormatter';

interface LabelledArrow {
  key: IForeignKey;
  table: LabelledTable;
  reverse: boolean;
}

class Referrers {
  private readonly byTable: Map<Table, IForeignKey[]> = new Map();

  constructor(tables: Table[]) {
    for (const table of tables) {
      const fks = table.columns.filter((col): col is IForeignKey => 'to' in col);
      
      for (const fk of fks) {
        const targetTable = fk.to.table;
        if (!this.byTable.has(targetTable)) {
          this.byTable.set(targetTable, []);
        }
        this.byTable.get(targetTable)!.push(fk);
      }
    }
  }

  getByTable(table: Table): IForeignKey[] {
    return this.byTable.get(table) || [];
  }
}

class TableSubset {
  private readonly tables: Set<LabelledTable>;
  private readonly referrers: Referrers;
  private readonly joinLabels: string[];

  constructor(tables: LabelledTable[], referrers: Referrers, joinLabels: string[]) {
    this.tables = new Set(tables);
    this.referrers = referrers;
    this.joinLabels = joinLabels;
  }

  private getArrows(table: Table): Array<{ key: IForeignKey; table: Table; reverse: boolean }> {
    const arrows: Array<{ key: IForeignKey; table: Table; reverse: boolean }> = [];

    // Forward arrows - FKs from this table to other tables
    const forwardKeys = table.columns.filter((col): col is IForeignKey => 'to' in col);
    for (const key of forwardKeys) {
      arrows.push({ key: key, table: key.to.table, reverse: false });
    }

    // Reverse arrows - FKs to this table from other tables
    const referrersToThisTable = this.referrers.getByTable(table);
    for (const referrer of referrersToThisTable) {
      arrows.push({ key: referrer, table: referrer.table, reverse: true });
    }
    return arrows;
  }

  public getLabelledArrows(table: LabelledTable): LabelledArrow[] {
    const arrows: LabelledArrow[] = [];
    
    for (const arrow of this.getArrows(table.value)) {
      if (arrow.table.conjoint && !table.value.conjoint) {
        // Target is conjoint, source is not conjoint - try all join labels
        for (const label of this.joinLabels) {
          const toTable: LabelledTable = { joinLabel: label, value: arrow.table };
          const matchingTable = this.findTableInSubset(toTable);
          if (matchingTable) {
            arrows.push({ key: arrow.key, table: matchingTable, reverse: arrow.reverse });
          }
        }
      } else if (!arrow.table.conjoint && table.value.conjoint) {
        // Source is conjoint, target is not conjoint - try null label (unlabeled)
        const toTable: LabelledTable = { joinLabel: null, value: arrow.table };
        const matchingTable = this.findTableInSubset(toTable);
        if (matchingTable) {
          arrows.push({ key: arrow.key, table: matchingTable, reverse: arrow.reverse });
        }
      } else {
        // Both same type (conjoint/non-conjoint) - use same join label
        const toTable: LabelledTable = { joinLabel: table.joinLabel, value: arrow.table };
        const matchingTable = this.findTableInSubset(toTable);
        if (matchingTable) {
          arrows.push({ key: arrow.key, table: matchingTable, reverse: arrow.reverse });
        }
      }
    }

    return arrows;
  }

  private findTableInSubset(targetTable: LabelledTable): LabelledTable | undefined {
    for (const table of this.tables) {
      if (table.value === targetTable.value && table.joinLabel === targetTable.joinLabel) {
        return table;
      }
    }
    return undefined;
  }

  public getReachableTablesInJoinOrder(from: LabelledTable): LabelledTable[] {
    const visited = new Map<string, LabelledTable>();

    const recurse = (table: LabelledTable) => {
      const key = `${table.value.name}|${table.joinLabel}`;
      if (!visited.has(key)) {
        visited.set(key, table);
        for (const arrow of this.getLabelledArrows(table)) {
          recurse(arrow.table);
        }
      }
    };

    recurse(from);
    return Array.from(visited.values());
  }

  public getReachableTablesMostDistantFirst(from: LabelledTable): LabelledTable[] {
    const visited = new Map<string, { table: LabelledTable, depth: number }>();

    const recurse = (table: LabelledTable, depth: number) => {
      const key = `${table.value.name}|${table.joinLabel}`;
      const existing = visited.get(key);
      if (!existing || existing.depth > depth) {
        visited.set(key, { table, depth });
        for (const arrow of this.getLabelledArrows(table)) {
          recurse(arrow.table, depth + 1);
        }
      }
    };

    recurse(from, 0);
    return Array.from(visited.values())
      .sort((a, b) => b.depth - a.depth)
      .map(entry => entry.table);
  }
}

export class Joins {
  private readonly aliases: Map<LabelledTable, string> = new Map();
  private readonly aliasesByKey: Map<string, string> = new Map();

  private getTableKey(table: LabelledTable): string {
    return `${table.value.name}|${table.joinLabel}`;
  }

  private get schema(): Schema {
    return Array.from(this.aliases.keys())[0].value.schema;
  }

  private get joinLabels(): string[] {
    return Array.from(new Set(Array.from(this.aliases.keys()).map(x => x.joinLabel).filter(x => x !== null) as string[]));
  }

  private get tables(): LabelledTable[] {
    const allTables: LabelledTable[] = [];
    
    // Add unlabeled (null) versions of all tables
    for (const table of this.schema.tables) {
      allTables.push({ joinLabel: null, value: table });
    }
    
    // Add labeled versions for each join label
    for (const joinLabel of this.joinLabels) {
      for (const table of this.schema.tables) {
        allTables.push({ joinLabel: joinLabel, value: table });
      }
    }
    
    return allTables;
  }

  private chooseBestRoot(needed: LabelledTable[], referrers: Referrers): LabelledTable {
    // Try each needed table as a potential root and see which one can reach all others best
    let bestRoot = needed[0];
    let bestScore = -1;

    for (const candidate of needed) {
      const score = this.calculateRootScore(candidate, needed, referrers);
      if (score > bestScore) {
        bestScore = score;
        bestRoot = candidate;
      }
    }

    return bestRoot;
  }

  private calculateRootScore(candidate: LabelledTable, needed: LabelledTable[], referrers: Referrers): number {
    let score = 0;

    // Prefer non-conjoint tables as they're usually central entities
    if (!candidate.value.conjoint) {
      score += 10;
    }

    // Count foreign key relationships (both outgoing and incoming)
    const outgoingFKs = candidate.value.columns.filter(col => 'to' in col).length;
    const incomingFKs = referrers.getByTable(candidate.value).length;
    score += outgoingFKs + incomingFKs;

    // Check if this candidate can reach all needed tables
    const tables = new TableSubset(this.tables, referrers, this.joinLabels);
    const reachable = tables.getReachableTablesMostDistantFirst(candidate);
    const canReachAll = needed.every(n => 
      n === candidate || reachable.some(r => r.value === n.value && r.joinLabel === n.joinLabel)
    );

    if (canReachAll) {
      score += 100; // Big bonus for being able to reach everything
    } else {
      score = -1; // Can't be root if it can't reach everything
    }

    return score;
  }

  private isAssociativeTable(table: Table): boolean {
    return table.associative != null && table.associative.length >= 2;
  }

  private isAssociativeRelationship(table: Table, foreignKey: IForeignKey): boolean {
    return table.associative?.includes(foreignKey) ?? false;
  }

  public getAlias(table: Table, join: string): string;
  public getAlias(table: LabelledTable): string;
  public getAlias(tableOrTable: Table | LabelledTable, join?: string): string {
    let labelledTable: LabelledTable;
    
    if ('value' in tableOrTable) {
      labelledTable = tableOrTable;
    } else {
      labelledTable = { joinLabel: join!, value: tableOrTable };
    }

    const key = this.getTableKey(labelledTable);
    let alias = this.aliasesByKey.get(key);
    if (!alias) {
      alias = `t${this.aliasesByKey.size}`;
      this.aliasesByKey.set(key, alias);
      this.aliases.set(labelledTable, alias);
    }
    return alias;
  }

  public aliased(column: LabelledColumn, sql: ISqlFormatter): string {
    return IdentifierPair(
      sql,
      this.getAlias(column.value.table, column.joinLabel!),
      column.value.dbName
    );
  }

  public toSql(sql: ISqlFormatter, fullJoins: boolean): string {
    return this.toSqlAndTables(sql, fullJoins).Sql;
  }

  public toSqlAndTables(
    sql: ISqlFormatter,
    fullJoins: boolean
  ): { Sql: string; Tables: LabelledTable[] } {
    const needed = Array.from(this.aliases.entries())
      .sort((a, b) => a[1].localeCompare(b[1]))
      .map(([table]) => table);

    const referrers = new Referrers(this.schema.tables);
    const root = this.chooseBestRoot(needed, referrers);
    const output: string[] = [`from ${root.value.toSql(sql)} ${this.getAlias(root)}`];

    const canReachAllNeeded = (reached: LabelledTable[]): boolean =>
      needed.every(n => n === root || reached.some(r => r.value === n.value && r.joinLabel === n.joinLabel));

    let tables = new TableSubset(this.tables, referrers, this.joinLabels);
    let reachable = tables.getReachableTablesMostDistantFirst(root);


    if (!canReachAllNeeded(reachable)) {
      throw new FlowerBIException(`Could not connect tables: ${needed.map(t => t.value.name).join(',')}`);
    }

    // Minimize the join set
    let repeat = true;
    while (repeat) {
      repeat = false;
      
      for (const candidateForRemoval of reachable) {
        if (needed.some(n => n.value === candidateForRemoval.value && n.joinLabel === candidateForRemoval.joinLabel)) continue;

        const without = reachable.filter(x => x !== candidateForRemoval);
        const reducedTables = new TableSubset(without, referrers, this.joinLabels);
        const reducedReachable = reducedTables.getReachableTablesMostDistantFirst(root);
        
        if (canReachAllNeeded(reducedReachable)) {
          reachable = reducedReachable;
          repeat = true;
          break;
        }
      }
    }

    // Now add back any associative tables that associate with two or more of our surviving tables
    repeat = true;
    while (repeat) {
      repeat = false;

      for (const candidateForAddition of this.tables) {
        // Skip if already included
        if (reachable.some(r => r.value === candidateForAddition.value && r.joinLabel === candidateForAddition.joinLabel)) {
          continue;
        }

        // Check if this table is associative (has associative relationships)
        if (!this.isAssociativeTable(candidateForAddition.value)) {
          continue;
        }

        // Check if this associative table can bridge between existing tables
        const expandedReachable = [...reachable, candidateForAddition];
        const expandedTables = new TableSubset(expandedReachable, referrers, this.joinLabels);
        const associativeConnections = expandedTables.getLabelledArrows(candidateForAddition)
          .filter(arrow => 
            !arrow.reverse && 
            this.isAssociativeRelationship(candidateForAddition.value, arrow.key) &&
            reachable.some(r => r.value === arrow.table.value && r.joinLabel === arrow.table.joinLabel)
          );

        // If the associative table connects to at least 2 existing tables, include it
        if (associativeConnections.length >= 2) {
          reachable = expandedReachable;
          repeat = true;
          break;
        }
      }
    }

    // Update tables to use the final set with associative tables
    tables = new TableSubset(reachable, referrers, this.joinLabels);
    reachable = tables.getReachableTablesInJoinOrder(root);

    const joinedSoFar = new Set<string>();
    joinedSoFar.add(`${root.value.name}|${root.joinLabel}`);
    const joinType = fullJoins ? 'full join' : 'join';

    const nonRootTables = reachable.filter(x => !(x.value === root.value && x.joinLabel === root.joinLabel));
    
    // Remove duplicates from nonRootTables
    const uniqueNonRootTables: LabelledTable[] = [];
    const seenTables = new Set<string>();
    for (const table of nonRootTables) {
      const key = `${table.value.name}|${table.joinLabel}`;
      if (!seenTables.has(key)) {
        seenTables.add(key);
        uniqueNonRootTables.push(table);
      }
    }
    
    for (const table of uniqueNonRootTables) {
      const availableArrows = tables.getLabelledArrows(table);
      const arrowsToAlreadyJoined = availableArrows.filter(x => 
        joinedSoFar.has(`${x.table.value.name}|${x.table.joinLabel}`)
      );
      const criteria: string[] = [];


      for (const arrow of arrowsToAlreadyJoined) {
        let leftColumn: IColumn = arrow.key;
        let rightColumn: IColumn = arrow.key.to.table.id!;
        
        if (arrow.reverse) {
          [leftColumn, rightColumn] = [rightColumn, leftColumn];
        }

        const leftPair = IdentifierPair(sql, this.getAlias(table), leftColumn.dbName);
        const rightPair = IdentifierPair(sql, this.getAlias(arrow.table), rightColumn.dbName);
        criteria.push(`${leftPair} = ${rightPair}`);
      }

      output.push(
        `${joinType} ${table.value.toSql(sql)} ${this.getAlias(table)} on ${criteria.join(' and ')}`
      );
      joinedSoFar.add(`${table.value.name}|${table.joinLabel}`);
    }

    return { Sql: output.join('\n'), Tables: [root, ...uniqueNonRootTables] };
  }
}