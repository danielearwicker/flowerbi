namespace FlowerBI;

public interface ISqlFormatter
{
    string Identifier(string name);
    string EscapedIdentifierPair(string id1, string id2);
    string SkipAndTake(long skip, int take);
    string Conditional(string predExpr, string thenExpr, string elseExpr);
    string CastToFloat(string valueExpr);
    string GetParamPrefix();
}

public class NullSqlFormatter : ISqlFormatter
{
    public string CastToFloat(string valueExpr) => string.Empty;

    public string Conditional(string predExpr, string thenExpr, string elseExpr) => string.Empty;

    public string EscapedIdentifierPair(string id1, string id2) => string.Empty;

    public string Identifier(string name) => string.Empty;

    public string SkipAndTake(long skip, int take) => string.Empty;

    public string GetParamPrefix() => string.Empty;

    public static readonly ISqlFormatter Singleton = new NullSqlFormatter();
}

public static class SqlFormatterExtensions
{
    public static string IdentifierPair(this ISqlFormatter sql, string id1, string id2) =>
        sql.EscapedIdentifierPair(sql.Identifier(id1), sql.Identifier(id2));
}

public class SqlServerFormatter : ISqlFormatter
{
    public string Identifier(string name) => $"[{name}]";

    public string EscapedIdentifierPair(string id1, string id2) => $"{id1}.{id2}";

    public string SkipAndTake(long skip, int take) =>
        @$"
            offset {skip} rows
            fetch next {take} rows only";

    public string Conditional(string predExpr, string thenExpr, string elseExpr) =>
        $"iif({predExpr}, {thenExpr}, {elseExpr})";

    public string CastToFloat(string valueExpr) => $"cast({valueExpr} as float)";

    public string GetParamPrefix() => "@";
}

public class SqlLiteFormatter : ISqlFormatter
{
    public string Identifier(string name) => $"[{name}]";

    public string EscapedIdentifierPair(string id1, string id2) => $"{id1}.{id2}";

    public string SkipAndTake(long skip, int take) => $"limit {take} offset {skip}";

    public string Conditional(string predExpr, string thenExpr, string elseExpr) =>
        $"iif({predExpr}, {thenExpr}, {elseExpr})";

    public string CastToFloat(string valueExpr) => $"cast({valueExpr} as real)";

    public string GetParamPrefix() => "@";
}

public class PostgresFormatter : ISqlFormatter
{
    public string Identifier(string name) => name;

    public string EscapedIdentifierPair(string id1, string id2) => $"{id1}.{id2}";

    public string SkipAndTake(long skip, int take) => $"limit {take} offset {skip}";

    public string Conditional(string predExpr, string thenExpr, string elseExpr) =>
        $"CASE WHEN {predExpr} THEN {thenExpr} ELSE {elseExpr} END";

    public string CastToFloat(string valueExpr) => $"cast({valueExpr} as real)";

    public string GetParamPrefix() => "@";
}
