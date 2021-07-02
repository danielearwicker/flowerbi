using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI
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

        private static object UnpackValue(object json)
        {
            if (json is JsonElement e)
            {
                return e.ValueKind == JsonValueKind.False ? false :
                    e.ValueKind == JsonValueKind.True ? true :
                    e.ValueKind == JsonValueKind.Number ? e.GetDouble() :
                    e.ValueKind == JsonValueKind.String ? (DateTime.TryParse(e.GetString(), out var dt) ? dt : (object)e.GetString()) :
                    e.ValueKind == JsonValueKind.Array ? e.EnumerateArray().Select(item =>
                        item.ValueKind == JsonValueKind.Number ? (object)item.GetDouble() :
                        item.ValueKind == JsonValueKind.String ? item.GetString()
                        : throw new InvalidOperationException("Unsupported filter value format in list item"))
                    : throw new InvalidOperationException("Unsupported filter value format");
            }

            return json;
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