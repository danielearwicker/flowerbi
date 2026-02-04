using System.Collections.Generic;
using System.Linq;
using FlowerBI.Yaml;

namespace FlowerBI;

public sealed class Table : Named
{
    public Schema Schema { get; }

    public IColumn Id { get; private set; }

    public bool Conjoint { get; }

    private readonly Dictionary<Table, IForeignKey> _keys = new();

    private readonly Dictionary<string, IColumn> _columns = new();

    private readonly List<IColumn> _associative = new();

    internal Table(Schema schema, ResolvedTable resolved, Dictionary<ResolvedColumn, IColumn> columnMap)
    {
        Schema = schema;
        RefName = resolved.Name;
        DbName = resolved.NameInDb;
        Conjoint = resolved.conjoint;

        // Create ID column if present
        if (resolved.IdColumn != null)
        {
            var idCol = CreateColumn(resolved.IdColumn, isPrimaryKey: true);
            idCol.Table = this;
            Id = idCol;
            _columns[idCol.RefName] = idCol;
            columnMap[resolved.IdColumn] = idCol;
        }

        // Create regular columns
        foreach (var rc in resolved.Columns)
        {
            var col = CreateColumn(rc, isPrimaryKey: false);
            col.Table = this;
            _columns[col.RefName] = col;
            columnMap[rc] = col;
        }

        // Mark associative columns
        foreach (var rc in resolved.Associative)
        {
            _associative.Add(_columns[rc.Name]);
        }
    }

    internal void ResolveTargets(ResolvedTable resolved, Dictionary<ResolvedColumn, IColumn> columnMap)
    {
        // Wire up FK target for ID column if it's a PrimaryForeignKey
        if (resolved.IdColumn?.Target != null && Id is PrimaryForeignKey pfk)
        {
            pfk.To = columnMap[resolved.IdColumn.Target];
            RegisterForeignKey(pfk);
        }

        // Wire up FK targets for regular columns
        foreach (var rc in resolved.Columns)
        {
            if (rc.Target != null && _columns[rc.Name] is ForeignKey fk)
            {
                fk.To = columnMap[rc.Target];
                RegisterForeignKey(fk);
            }
        }
    }

    private void RegisterForeignKey(IForeignKey key)
    {
        if (_keys.ContainsKey(key.To.Table))
        {
            throw new FlowerBIException(
                $"Table {this} already has foreign key to {key.To.Table}, can't set {key}"
            );
        }
        _keys.Add(key.To.Table, key);
    }

    private static Column CreateColumn(ResolvedColumn rc, bool isPrimaryKey)
    {
        var hasTarget = rc.Target != null;

        if (isPrimaryKey)
        {
            return hasTarget
                ? new PrimaryForeignKey(rc.NameInDb, rc.Name, rc.DataType, rc.Nullable)
                : new PrimaryKey(rc.NameInDb, rc.Name, rc.DataType, rc.Nullable);
        }

        return hasTarget
            ? new ForeignKey(rc.NameInDb, rc.Name, rc.DataType, rc.Nullable)
            : new Column(rc.NameInDb, rc.Name, rc.DataType, rc.Nullable);
    }

    public override string ToString() => $"{Schema.RefName}.{RefName}";

    public IEnumerable<IColumn> Columns => _columns.Values;

    public IEnumerable<IColumn> Associative => _associative;

    public IColumn GetColumn(string refName)
    {
        if (!_columns.TryGetValue(refName, out var column))
        {
            throw new FlowerBIException($"No such column {refName} in table {this}");
        }

        return column;
    }

    public IForeignKey GetForeignKeyTo(Table otherTable)
    {
        return _keys.TryGetValue(otherTable, out var foreignKey)
            ? foreignKey
            : throw new FlowerBIException($"Table {this} has no foreign key to {otherTable}");
    }

    public string ToSql(ISqlFormatter sql) => sql.IdentifierPair(Schema.DbName, DbName);
}
