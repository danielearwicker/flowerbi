namespace FlowerBI.Engine.JsonModels
{
    public enum OrderingType
    {
        Select,
        Value,
        Calculation,
    }

    public class OrderingJson
    {
        public bool Descending { get; set; }

        public string Column { get; set; }

        public OrderingType? Type { get; set; }

        public int? Index { get; set; }
    }
}
