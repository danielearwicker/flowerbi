namespace FlowerBI
{
    public interface ISqlFormatter
    {
        string Identifier(string name);
        string EscapedIdentifierPair(string id1, string id2);
        string SkipAndTake(long skip, int take);
    }

    public static class SqlFormatterExtensions
    {
        public static string IdentifierPair(this ISqlFormatter sql, string id1, string id2)
            => sql.EscapedIdentifierPair(sql.Identifier(id1), sql.Identifier(id2));
    }

    public class SqlServerFormatter : ISqlFormatter
    {
        public string Identifier(string name) => $"[{name}]";
        public string EscapedIdentifierPair(string id1, string id2) => $"{id1}.{id2}";

        public string SkipAndTake(long skip, int take) => @$"
            offset {skip} rows
            fetch next {take} rows only";
    }

    public class SqlLiteFormatter : ISqlFormatter
    {
        public string Identifier(string name) => $"`{name}`";
        public string EscapedIdentifierPair(string id1, string id2) => $"{id1}.{id2}";
        public string SkipAndTake(long skip, int take) => $"limit {take} offset {skip}";
    }
}
