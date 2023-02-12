using System;

namespace FlowerBI
{
    public class PrimaryKey<T> : Column<T>
    {
        public PrimaryKey(string name, Func<T, T> converter = null, Column<T> extends = null)
            : base(name, converter, extends) { }
    }
}
