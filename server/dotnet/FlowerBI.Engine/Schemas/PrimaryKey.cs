using System;

namespace FlowerBI
{
    public class PrimaryKey<T> : Column<T>
    {
        public PrimaryKey(string name, Func<T, T> converter = null, Column<T> extends = null)
            : base(name, converter, extends) { }
    }

    public class PrimaryForeignKey<T> : PrimaryKey<T>, IForeignKey
    {
        public PrimaryKey<T> To { get; }

        public PrimaryForeignKey(string name, PrimaryKey<T> to, Func<T, T> converter = null, Column<T> extends = null)
            : base(name, converter, extends)
        {
            To = to;
        }

        IColumn IForeignKey.To => To;
    }
}
