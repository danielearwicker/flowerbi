using System;

namespace TinyBI
{
    public class DbSchemaAttribute : Attribute
    {
        public DbSchemaAttribute(string dbSchemaName)
        {
            Name = dbSchemaName;
        }

        public string Name { get; }
    }
}
