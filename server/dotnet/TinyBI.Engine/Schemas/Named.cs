namespace TinyBI
{
    public interface INamed
    {
        string DbName { get; }
        string RefName { get; }
    }

    public class Named : INamed
    {
        public string DbName { get; protected set; }
        public string RefName { get; protected set; }
    }
}
