using System;

namespace FlowerBI
{
    public interface IColumn : INamed
    {
        Table Table { get; }

        Type ClrType { get; }

        object ConvertValue(object fromDb);
    }

    internal interface IColumnInternal : IColumn
    {
        void SetTable(Table table, string reflectionName);
    }

    public class Column<T> : Named, IColumnInternal
    {
        private readonly Func<T, T> _converter;

        public Table Table { get; private set; }

        public Column(string name, Func<T, T> converter = null)
        {
            DbName = name;
            _converter = converter;  
        }

        public object ConvertValue(object fromDb)
            => fromDb == null ? null : _converter == null ? fromDb : _converter((T)fromDb);

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
