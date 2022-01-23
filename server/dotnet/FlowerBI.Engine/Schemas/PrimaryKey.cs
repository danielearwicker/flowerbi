using System;

namespace FlowerBI
{
    public class PrimaryKey<T> : Column<T>
    {
        public PrimaryKey(string name, Func<T, T> converter = null)
            : base(name, converter) { }
    }
}
