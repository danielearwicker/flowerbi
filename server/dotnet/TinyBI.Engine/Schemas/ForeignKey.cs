namespace TinyBI
{
    public interface IForeignKey : IColumn
    {
        IColumn To { get; }
    }

    public sealed class ForeignKey<T> : Column<T>, IForeignKey
    {
        public PrimaryKey<T> To { get; }

        public ForeignKey(string name, PrimaryKey<T> to)
            : base(name)
        {
            To = to;
        }

        IColumn IForeignKey.To => To;
    }
}
