using System.Text.Json;

namespace TinyBI.Engine.JsonModels
{
    public class FilterJson
    {
        public string Column { get; set; }

        public string Operator { get; set; }

        public object Value { get; set; }
    }
}
