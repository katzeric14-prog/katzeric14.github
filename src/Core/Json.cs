using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Vexor.Core
{
    public enum JsonKind { Null, Bool, Number, String, Array, Object }

    public sealed class JsonValue
    {
        public JsonKind Kind { get; private set; }

        private readonly bool _bool;
        private readonly double _number;
        private readonly string _text;
        private readonly List<JsonValue> _items;
        private readonly Dictionary<string, JsonValue> _props;

        public static readonly JsonValue NullValue = new JsonValue();
        private static readonly List<JsonValue> EmptyItems = new List<JsonValue>();
        private static readonly Dictionary<string, JsonValue> EmptyProps = new Dictionary<string, JsonValue>();

        private JsonValue() { Kind = JsonKind.Null; }
        public JsonValue(bool value) { Kind = JsonKind.Bool; _bool = value; }
        public JsonValue(double value) { Kind = JsonKind.Number; _number = value; }
        public JsonValue(string value) { Kind = JsonKind.String; _text = value; }
        public JsonValue(List<JsonValue> value) { Kind = JsonKind.Array; _items = value; }
        public JsonValue(Dictionary<string, JsonValue> value) { Kind = JsonKind.Object; _props = value; }

        public bool IsNull { get { return Kind == JsonKind.Null; } }
        public bool IsObject { get { return Kind == JsonKind.Object; } }
        public bool IsArray { get { return Kind == JsonKind.Array; } }
        public bool IsString { get { return Kind == JsonKind.String; } }

        public JsonValue this[string key]
        {
            get
            {
                JsonValue value;
                if (_props != null && _props.TryGetValue(key, out value)) return value;
                return NullValue;
            }
        }

        public JsonValue this[int index]
        {
            get
            {
                if (_items != null && index >= 0 && index < _items.Count) return _items[index];
                return NullValue;
            }
        }

        public IList<JsonValue> Items { get { return _items ?? EmptyItems; } }
        public IDictionary<string, JsonValue> Properties { get { return _props ?? EmptyProps; } }

        public int Count
        {
            get
            {
                if (_items != null) return _items.Count;
                if (_props != null) return _props.Count;
                return 0;
            }
        }

        public bool Has(string key) { return _props != null && _props.ContainsKey(key); }

        public string AsString(string fallback = null)
        {
            if (Kind == JsonKind.String) return _text;
            if (Kind == JsonKind.Number) return _number.ToString(CultureInfo.InvariantCulture);
            return fallback;
        }

        public double AsDouble(double fallback = 0)
        {
            return Kind == JsonKind.Number ? _number : fallback;
        }

        public long AsLong(long fallback = 0)
        {
            return Kind == JsonKind.Number ? (long)_number : fallback;
        }

        public int AsInt(int fallback = 0)
        {
            return Kind == JsonKind.Number ? (int)_number : fallback;
        }

        public bool AsBool(bool fallback = false)
        {
            return Kind == JsonKind.Bool ? _bool : fallback;
        }
    }

    public static class JsonParser
    {
        public static JsonValue Parse(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            int pos = 0;
            JsonValue result = ParseValue(text, ref pos);
            SkipWhitespace(text, ref pos);
            if (pos != text.Length) throw Error(text, pos, "лишние символы после JSON");
            return result;
        }

        private static JsonValue ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) throw Error(s, i, "неожиданный конец JSON");
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return new JsonValue(ParseString(s, ref i));
            if (c == 't') { Expect(s, ref i, "true"); return new JsonValue(true); }
            if (c == 'f') { Expect(s, ref i, "false"); return new JsonValue(false); }
            if (c == 'n') { Expect(s, ref i, "null"); return JsonValue.NullValue; }
            return new JsonValue(ParseNumber(s, ref i));
        }

        private static JsonValue ParseObject(string s, ref int i)
        {
            var props = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            i++;
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return new JsonValue(props); }
            while (true)
            {
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Error(s, i, "ожидалось имя поля");
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Error(s, i, "ожидалось ':'");
                i++;
                props[key] = ParseValue(s, ref i);
                SkipWhitespace(s, ref i);
                if (i >= s.Length) throw Error(s, i, "не закрыт объект");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; break; }
                throw Error(s, i, "ожидалось ',' или '}'");
            }
            return new JsonValue(props);
        }

        private static JsonValue ParseArray(string s, ref int i)
        {
            var items = new List<JsonValue>();
            i++;
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return new JsonValue(items); }
            while (true)
            {
                items.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (i >= s.Length) throw Error(s, i, "не закрыт массив");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; break; }
                throw Error(s, i, "ожидалось ',' или ']'");
            }
            return new JsonValue(items);
        }

        private static string ParseString(string s, ref int i)
        {
            i++;
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) throw Error(s, i, "незавершённая escape-последовательность");
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Error(s, i, "неверный \\u escape");
                        int code;
                        if (!int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            throw Error(s, i, "неверный \\u escape");
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default: throw Error(s, i, "неизвестный escape: \\" + e);
                }
            }
            throw Error(s, i, "не закрыта строка");
        }

        private static double ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') i++;
                else break;
            }
            if (i == start) throw Error(s, i, "ожидалось число");
            double value;
            if (!double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw Error(s, start, "неверное число");
            return value;
        }

        private static void Expect(string s, ref int i, string word)
        {
            if (i + word.Length > s.Length || string.CompareOrdinal(s, i, word, 0, word.Length) != 0)
                throw Error(s, i, "ожидалось '" + word + "'");
            i += word.Length;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') i++;
                else break;
            }
        }

        private static FormatException Error(string s, int pos, string message)
        {
            int line = 1, col = 1;
            for (int k = 0; k < pos && k < s.Length; k++)
            {
                if (s[k] == '\n') { line++; col = 1; }
                else col++;
            }
            return new FormatException("JSON (" + line + ":" + col + "): " + message);
        }
    }

    public static class JsonText
    {
        public static string Quote(string value)
        {
            if (value == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        public static string Serialize(JsonValue value)
        {
            var sb = new StringBuilder();
            WriteValue(value, sb);
            return sb.ToString();
        }

        private static void WriteValue(JsonValue value, StringBuilder sb)
        {
            if (value == null || value.IsNull)
            {
                sb.Append("null");
                return;
            }
            if (value.Kind == JsonKind.Bool)
            {
                sb.Append(value.AsBool() ? "true" : "false");
                return;
            }
            if (value.Kind == JsonKind.Number)
            {
                sb.Append(value.AsDouble().ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            if (value.Kind == JsonKind.String)
            {
                sb.Append(Quote(value.AsString()));
                return;
            }
            if (value.Kind == JsonKind.Array)
            {
                sb.Append('[');
                for (int i = 0; i < value.Items.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    WriteValue(value.Items[i], sb);
                }
                sb.Append(']');
                return;
            }

            sb.Append('{');
            bool first = true;
            foreach (var pair in value.Properties)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append(Quote(pair.Key)).Append(':');
                WriteValue(pair.Value, sb);
            }
            sb.Append('}');
        }
    }
}
