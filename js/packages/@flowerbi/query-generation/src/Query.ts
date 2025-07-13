import * as Handlebars from 'handlebars';
import {
  QueryJson,
  QueryResultJson,
  QueryRecordJson,
  ISqlFormatter,
  IFilterParameters,
  LabelledColumn,
  Schema,
  CalculationJson,
  AggregationType,
  FlowerBIException,
  Table,
  LabelledTable,
} from './types';
import { Filter } from './Filter';
import { Aggregation } from './Aggregation';
import { Ordering } from './Ordering';
import { Joins } from './Joins';
import { Calculation } from './Calculation';
import { NullSqlFormatter } from './SqlFormatter';

export class Query {
  public readonly Select: LabelledColumn[];
  public readonly Aggregations: Aggregation[];
  public readonly Filters: Filter[];
  public readonly OrderBy: Ordering[];
  public readonly Calculations: CalculationJson[];
  public readonly Totals: boolean;
  public readonly Skip: number;
  public readonly Take: number;
  public readonly Comment: string;
  public readonly AllowDuplicates: boolean;
  public readonly CommandTimeoutSeconds: number;
  public readonly FullJoins: boolean;

  private static readonly templateMain = `
        select
            {{#each selects}}
            {{{this}}}{{#unless @last}}, {{/unless}}
            {{/each}}
        {{{joins}}}
        {{#if filters}}
        where
            {{#each filters}}
            {{{FilterSql}}}{{#unless @last}} and {{/unless}}
            {{/each}}
        {{/if}}
        {{#if groupBy}}
        group by
            {{#each groupBy}}
            {{{Part}}}{{#unless @last}}, {{/unless}}
            {{/each}}
        {{/if}}
        `;

  private static readonly templateFooter = `
        {{#if orderBy}}
        order by {{{orderBy}}}
        {{/if}}
        {{#if skipAndTake}}
        {{{skipAndTake}}}
        {{/if}}
        `;

  private static readonly templateWithoutCalculations = Handlebars.compile(
    `${Query.templateMain}${Query.templateFooter}`
  );

  private static readonly templateWithCalculations = Handlebars.compile(
    `with calculation_source as (
                ${Query.templateMain}
            )
            select calculation_source.*
                {{#each calculations}}
                    ,{{{this}}}
                {{/each}}
            from calculation_source
            ${Query.templateFooter}`
  );

  constructor(json: QueryJson, schema: Schema) {
    this.Select = schema.Load(json.Select || []);
    this.Aggregations = Aggregation.Load(json.Aggregations, schema);
    this.Filters = Filter.Load(json.Filters, schema);
    this.OrderBy = Ordering.Load(
      json.OrderBy,
      schema,
      json.Select?.length || 0,
      json.Aggregations?.length || 0,
      json.Calculations?.length || 0
    );
    this.Calculations = json.Calculations || [];
    this.Totals = json.Totals || false;
    this.Skip = json.Skip || 0;
    this.Take = json.Take || 100;
    this.Comment = json.Comment || '';
    this.AllowDuplicates = json.AllowDuplicates || false;
    this.CommandTimeoutSeconds = 30;
    this.FullJoins = json.FullJoins || false;
  }

  private static formatAggFunction(
    func: AggregationType,
    expr: string,
    filters: Filter[],
    joins: Joins,
    sql: ISqlFormatter,
    filterParams: IFilterParameters
  ): string {
    if (filters.length > 0) {
      const when = filters
        .map(f => Query.formatFilter(f, joins, sql, filterParams))
        .join(' and ');
      expr = `case when ${when} then ${expr} end`;
    }

    return func === AggregationType.CountDistinct
      ? `count(distinct ${expr})`
      : `${func}(${expr})`;
  }

  private static formatFilter(
    f: Filter,
    joins: Joins,
    sql: ISqlFormatter,
    filterParams: IFilterParameters
  ): string {
    const column = joins.aliased(f.Column, sql);
    
    if (f.Operator === 'BITS IN') {
      const mask = typeof f.Constant === 'number' ? f.Constant : 
                   typeof f.Constant === 'string' ? parseInt(f.Constant) : 0;
      
      // Handle the array of values like a normal IN operator
      if (Array.isArray(f.Value)) {
        if (f.Value.length === 0) {
          throw new FlowerBIException('Filter JSON contains empty array');
        }
        const paramPlaceholders = f.Value.map((_, i) => {
          const paramName = `param${Object.keys(filterParams).length}`;
          filterParams[paramName] = f.Value[i];
          return Query.formatParameterPlaceholder(sql, paramName);
        }).join(', ');
        return `(${column} & ${mask}) in (${paramPlaceholders})`;
      }
      
      // Single value case
      const paramName = `param${Object.keys(filterParams).length}`;
      filterParams[paramName] = f.Value;
      const paramPlaceholder = Query.formatParameterPlaceholder(sql, paramName);
      return `(${column} & ${mask}) = ${paramPlaceholder}`;
    }

    if (f.Operator === 'IN' || f.Operator === 'NOT IN') {
      if (Array.isArray(f.Value)) {
        if (f.Value.length === 0) {
          throw new FlowerBIException('Filter JSON contains empty array');
        }
        // For IN queries, generate the parameter list
        const paramPlaceholders = f.Value.map((_, i) => {
          const paramName = `param${Object.keys(filterParams).length}`;
          filterParams[paramName] = f.Value[i];
          return Query.formatParameterPlaceholder(sql, paramName);
        }).join(', ');
        return `${column} ${f.Operator} (${paramPlaceholders})`;
      }
    }

    // For single value parameters
    const paramName = `param${Object.keys(filterParams).length}`;
    filterParams[paramName] = f.Value;
    const paramPlaceholder = Query.formatParameterPlaceholder(sql, paramName);

    return `${column} ${f.Operator} ${paramPlaceholder}`;
  }

  private static formatParameterPlaceholder(sql: ISqlFormatter, paramName: string): string {
    const prefix = sql.GetParamPrefix();
    if (prefix === '?') {
      // SQLite uses positional parameters
      return '?';
    } else {
      // SQL Server and others use named parameters
      return prefix + paramName;
    }
  }

  public toSql(
    sql: ISqlFormatter,
    filterParams: IFilterParameters,
    outerFilters: Filter[],
    totals: boolean
  ): string {
    return this.toSqlAndTables(sql, filterParams, outerFilters, totals).Sql;
  }

  public getRequiredTables(outerFilters: Filter[]): Table[] {
    return this.toSqlAndTables(
      NullSqlFormatter.Singleton,
      {},
      outerFilters,
      false
    ).Tables.map(x => x.Value);
  }

  public toSqlAndTables(
    sql: ISqlFormatter,
    filterParams: IFilterParameters,
    outerFilters: Filter[],
    totals: boolean
  ): { Sql: string; Tables: LabelledTable[] } {
    const joins = new Joins();

    const selects = (totals ? [] : this.Select)
      .map(
        (c, i) =>
          `${sql.EscapedIdentifierPair(joins.getAlias(c.Value.Table, c.JoinLabel!), c.Value.DbName)} Select${i}`
      );

    const aggs = this.Aggregations.map(
      (a, i) =>
        `${Query.formatAggFunction(a.Function, joins.aliased(a.Column, sql), a.Filters, joins, sql, filterParams)} Value${i}`
    );

    selects.push(...aggs);

    if (selects.length === 0) {
      throw new FlowerBIException('Must select something');
    }

    const filters = [...outerFilters, ...this.Filters].map(f => ({
      FilterSql: Query.formatFilter(f, joins, sql, filterParams),
    }));

    const groupBy =
      totals || (this.AllowDuplicates && this.Aggregations.length === 0)
        ? null
        : this.Select.map(x => ({ Part: joins.aliased(x, sql) }));

    const skipAndTake = !totals ? sql.SkipAndTake(this.Skip, this.Take) : null;

    const orderBy =
      skipAndTake === null ? null
      : this.OrderBy.length > 0
        ? this.OrderBy.map(x =>
            Query.findIndexedOrderingColumn(x) ?? Query.findNamedSelectOrderingColumn(x, this.Select)
          ).join(', ')
      : aggs.length > 0 ? `${this.Select.length + 1} desc`
      : '1 asc';

    const calculations = this.Calculations.map((calc, i) => {
      const calculation = new Calculation(calc);
      return `${calculation.toSql(sql, (index) => `calculation_source.Value${index}`)} Value${this.Aggregations.length + i}`;
    });

    const template =
      calculations.length === 0 ? Query.templateWithoutCalculations : Query.templateWithCalculations;

    const { Sql: joinSql, Tables: joinedTables } = joins.toSqlAndTables(sql, this.FullJoins);

    const sqlFromTemplate = template({
      selects,
      filters,
      calculations,
      joins: joinSql,
      groupBy,
      orderBy,
      skipAndTake,
    });

    return { Sql: sqlFromTemplate, Tables: joinedTables };
  }

  public static findIndexedOrderingColumn(ordering: Ordering): string | null {
    return ordering.Column === null ? `${ordering.Index + 1} ${ordering.Direction}` : null;
  }

  public static findNamedSelectOrderingColumn(
    ordering: Ordering,
    selects: LabelledColumn[]
  ): string {
    const found = selects.findIndex(c => 
      c.Value.DbName === ordering.Column?.Value.DbName && 
      c.Value.Table === ordering.Column?.Value.Table &&
      c.JoinLabel === ordering.Column?.JoinLabel
    );
    if (found === -1) {
      throw new FlowerBIException(
        `Cannot order by ${ordering.Column?.Value.DbName} as it has not been selected`
      );
    }
    return `${found + 1} ${ordering.Direction}`;
  }

  private static readonly sanitiseCommentPattern = /[^\w\d\r\n]+/g;

  public toSqlWithComment(
    sql: ISqlFormatter,
    filterParams: IFilterParameters,
    outerFilters: Filter[]
  ): string {
    let result = '';

    if (this.Comment) {
      // First replace escape sequences, then sanitize
      let processed = this.Comment
        .replace(/\\r/g, '\r')
        .replace(/\\n/g, '\n')
        .replace(/\\t/g, '\t');
      const stripped = processed.replace(Query.sanitiseCommentPattern, ' ').trim();
      result += `/* ${stripped} */ \r\n`;
    }

    if (this.Totals) {
      result += this.toSql(sql, filterParams, outerFilters, true) + ';';
    }

    result += this.toSql(sql, filterParams, outerFilters, false);

    return result;
  }
}