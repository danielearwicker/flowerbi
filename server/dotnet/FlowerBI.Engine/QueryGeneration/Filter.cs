using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlowerBI.Engine.JsonModels;
using Microsoft.Win32.SafeHandles;

namespace FlowerBI
{
    public class Filter(LabelledColumn column, string op, object val, object constant)
    {
        public LabelledColumn Column { get; } = column;

        public string Operator { get; } = CheckOperator(op);

        public object Value { get; } = val;

        public object Constant { get; } = constant;

        public static IList<Filter> Load(IEnumerable<FilterJson> filters, Schema schema) =>
            filters?.Select(x => new Filter(x, schema)).ToList() ?? [];

        public Filter(IColumn column, string op, object val, object constant)
            : this(new LabelledColumn(null, column), op, val, constant) { }

        public Filter(FilterJson json, Schema schema)
            : this(
                schema.GetColumn(json.Column),
                json.Operator,
                UnpackAndValidateValue(json.Value),
                UnpackAndValidateValue(json.Constant)
            ) { }

        private static readonly HashSet<Type> _basicValueTypes = [
            typeof(bool),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(DateTime)
        ];

        private static void ValidateBasicType(object value)
        {
            var type = value.GetType();
            if (!_basicValueTypes.Contains(type))
            {
                throw new InvalidOperationException($"Unsupported filter value");
            }
        }

        private static object UnpackAndValidateValue(object json)
        {
            var unpacked = UnpackValue(json);
            if (unpacked is null) return null;
            if (_basicValueTypes.Contains(unpacked.GetType())) return unpacked;

            if (unpacked is IEnumerable enumerable)
            {
                var empty = true;

                foreach (var item in enumerable)
                {
                    ValidateBasicType(item);
                    empty = false;
                }

                if (empty)
                {
                    // We don't silently strip out filters with empty lists of required values so
                    // that filtering can be used for security controls (denying access to data).
                    //
                    // This has always been the case, but now it produces a descriptive error
                    // instead of a SQL syntax error.
                    throw new InvalidOperationException("Filter JSON contains empty array");
                }

                return unpacked;
            }
            
            ValidateBasicType(unpacked);
            return unpacked;
        }

        private static object UnpackValue(object json)
        {
            if (json is JsonElement e)
            {
                return e.ValueKind == JsonValueKind.False ? false
                    : e.ValueKind == JsonValueKind.True ? true
                    : e.ValueKind == JsonValueKind.Number ? e.GetDouble()
                    : e.ValueKind == JsonValueKind.String
                        ? (
                            DateTime.TryParse(e.GetString(), out var dt)
                                ? dt
                                : e.GetString()
                        )
                    : e.ValueKind == JsonValueKind.Array
                        ? e.EnumerateArray()
                            .Select(item =>
                                item.ValueKind == JsonValueKind.False ? false
                                : item.ValueKind == JsonValueKind.True ? true
                                : item.ValueKind == JsonValueKind.Number ? (object)item.GetDouble()
                                : item.ValueKind == JsonValueKind.String ? item.GetString()
                                : new object()
                            )
                            .ToList()
                    : new object();
            }

            if (json is IEnumerable<object> l)
            {
                return l.Select(UnpackNewtonsoft).ToList();
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
            if (
                type.IsPrimitive
                || type == typeof(string)
                || type == typeof(DateTime)
                || type == typeof(decimal)
            )
            {
                return val;
            }

            // Assume it's a Newtonsoft value wrapper (don't want to depend on a specific version)
            var d = (dynamic)val;
            return d.Value;
        }

        private static readonly HashSet<string> _allowedOperators = new HashSet<string>
        {
            "=",
            "<>",
            "!=",
            ">",
            "<",
            ">=",
            "<=",
            "IN",
            "NOT IN",
            "BITS IN",
            "LIKE",
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
