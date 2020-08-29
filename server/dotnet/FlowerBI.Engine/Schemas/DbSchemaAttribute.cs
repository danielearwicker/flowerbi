using System;

namespace FlowerBI
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
