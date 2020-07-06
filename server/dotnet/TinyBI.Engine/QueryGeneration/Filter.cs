using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TinyBI.Engine.JsonModels;

namespace TinyBI
{
    public class Filter
    {
        public IColumn Column { get; }

        public string Operator { get; }

        public object Value { get; }

        public static IList<Filter> Load(IEnumerable<FilterJson> filters, Schema schema)
            => filters?.Select(x => new Filter(x, schema)).ToList()
                ?? new List<Filter>();

        public Filter(IColumn column, string op, object val)
        {
            Column = column;
            Operator = CheckOperator(op);
            Value = val;
        }

        public Filter(FilterJson json, Schema schema)
            : this(schema.GetColumn(json.Column), json.Operator, UnpackValue(json.Value)) { }

        private static object UnpackValue(JsonElement json)
        {
            return json.ValueKind == JsonValueKind.False ? false :
                    json.ValueKind == JsonValueKind.True ? true :
                    json.ValueKind == JsonValueKind.Number ? json.GetDouble() :
                    json.ValueKind == JsonValueKind.String ? (object)json.GetString() :
                    throw new InvalidOperationException("Unsupported filter value format");
        }

        private static readonly HashSet<string> _allowedOperators = new HashSet<string>
        {
            "=", "<>", "!=", ">", "<", ">=", "<=", "IN"
        };

        private static string CheckOperator(string op)
        {
            if (!_allowedOperators.Contains(op))
            {
                throw new InvalidOperationException($"{op} is not an allowed operator");
            }

            return op;
        }
    }
}
