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
        private Func<T, T> _converter;
        private readonly Column<T> _extends;

        public Table Table { get; private set; }

        public Column(string name, Func<T, T> converter = null, Column<T> extends = null)
        {
            DbName = name;
            _converter = converter;
            _extends = extends;
        }

        public void SetConverter(Func<T, T> converter)
        {
            _converter = converter;
        }

        public object ConvertValue(object fromDb)
        {
            if (fromDb == null) return null;

            var converterSource = this;
            while (converterSource._converter == null && converterSource._extends != null)
            {
                converterSource = converterSource._extends;
            }
            
            var converter = converterSource._converter;            
            return converter == null ? fromDb : converter((T)fromDb);
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
