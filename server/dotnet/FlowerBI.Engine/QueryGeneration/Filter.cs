﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlowerBI.Engine.JsonModels;

namespace FlowerBI
{
    public class Filter
    {
        public LabelledColumn Column { get; }

        public string Operator { get; }

        public object Value { get; }

        public static IList<Filter> Load(IEnumerable<FilterJson> filters, Schema schema)
            => filters?.Select(x => new Filter(x, schema)).ToList()
                ?? new List<Filter>();

        public Filter(LabelledColumn column, string op, object val)
        {
            Column = column;
            Operator = CheckOperator(op);
            Value = val;
        }

        public Filter(IColumn column, string op, object val)
            : this(new LabelledColumn(null, column), op, val) {}
        
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

            if (json is IEnumerable<object> l)
            {
                return l.Select(UnpackNewtonsoft);
            }

            return json;
        }

        // Don't want to depend on a specific version, so...
        private static object UnpackNewtonsoft(object val)
        {
            if (val == null)
            {
                return null;
            }

            var type = val.GetType();
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal))
            {
                return val;
            }

            // Assume it's a Newtonsoft value wrapper (don't want to depend on a specific version)
            var d = (dynamic)val;
            return d.Value;
        }

        private static readonly HashSet<string> _allowedOperators = new HashSet<string>
        {
            "=", "<>", "!=", ">", "<", ">=", "<=", "IN", "NOT IN", "BITS ON", "BITS OFF"
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
