﻿namespace FlowerBI
{
    public class PrimaryKey<T> : Column<T>
    {
        public PrimaryKey(string name)
            : base(name) { }
    }
}
