using System;

namespace FlowerBI
{
    public class DbTableAttribute : Attribute
    {
        public DbTableAttribute(string dbTableOrViewName, bool conjoint = false)
        {
            Name = dbTableOrViewName;
            Conjoint = conjoint;
        }

        public string Name { get; }

        public bool Conjoint { get; }
    }
}
