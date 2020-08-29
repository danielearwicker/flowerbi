using System;

namespace FlowerBI
{
    public interface IColumn : INamed
    {
        Table Table { get; }

        Type ClrType { get; }
    }

    internal interface IColumnInternal : IColumn
    {
        void SetTable(Table table, string reflectionName);
    }

    public class Column<T> : Named, IColumnInternal
    {
        public Table Table { get; private set; }

        public Column(string name)
        {
            DbName = name;
        }

        public Type ClrType => typeof(T);

        public void SetTable(Table table, string reflectionName)
        {
            if (Table != null)
            {
                throw new InvalidOperationException(
                    $"Field {reflectionName} belongs to table {Table}, " +
                    $"cannot also belong to table {table}");
            }

            Table = table;
            RefName = reflectionName;
        }

        public override string ToString()
            => $"{Table}.{RefName}";
    }
}
