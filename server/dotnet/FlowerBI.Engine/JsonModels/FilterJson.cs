using System.Text.Json;

namespace FlowerBI.Engine.JsonModels
{
    public class FilterJson
    {
        public string Column { get; set; }

        public string Operator { get; set; }

        public object Value { get; set; }

        public object Constant { get; set; }
    }
}
