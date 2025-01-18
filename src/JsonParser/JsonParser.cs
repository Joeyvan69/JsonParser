using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DesignPattern;
using Native;

namespace LowGCJsonParser
{
    public class JsonParser
    {
        private ObjectPool<StringBuilder> _stringBuilderPool;
        private ObjectPool<List<JsonToken>> _tokenListPool;
        private ObjectPool<NativeMemoryPool> _memoryPool;

        public JsonParser(int stringBuilderPoolSize = 10, int tokenListPoolSize = 10, int memoryPoolSize = 5)
        {
            _stringBuilderPool = new ObjectPool<StringBuilder>(() => new StringBuilder(), stringBuilderPoolSize);
            _tokenListPool = new ObjectPool<List<JsonToken>>(() => new List<JsonToken>(), tokenListPoolSize);
            _memoryPool = new ObjectPool<NativeMemoryPool>(() => new NativeMemoryPool(4096), memoryPoolSize);
        }

        private List<JsonToken> Lex(string json)
        {
            NativeMemoryPool nativeMemory = _memoryPool.Get();
            ReadOnlySpan<char> jsonSpan = json.AsSpan();
            List<JsonToken> tokens = _tokenListPool.Get();
            int index = 0;
            while (index < jsonSpan.Length)
            {
                char c = jsonSpan[index];
                switch (c)
                {
                    case '{':
                        tokens.Add(new JsonToken { Type = JsonTokenType.ObjectStart });
                        index++;
                        break;
                    case '}':
                        tokens.Add(new JsonToken { Type = JsonTokenType.ObjectEnd });
                        index++;
                        break;
                    case '[':
                        tokens.Add(new JsonToken { Type = JsonTokenType.ArrayStart });
                        index++;
                        break;
                    case ']':
                        tokens.Add(new JsonToken { Type = JsonTokenType.ArrayEnd });
                        index++;
                        break;
                    case ',':
                        tokens.Add(new JsonToken { Type = JsonTokenType.Comma });
                        index++;
                        break;
                    case ':':
                        tokens.Add(new JsonToken { Type = JsonTokenType.Colon });
                        index++;
                        break;
                    case '"':
                        index++;
                        var sb = _stringBuilderPool.Get();
                        while (index < jsonSpan.Length && jsonSpan[index] != '"')
                        {
                            sb.Append(jsonSpan[index]);
                            index++;
                        }
                        if (index < jsonSpan.Length && jsonSpan[index] == '"')
                        {
                            index++;
                            tokens.Add(new JsonToken { Type = JsonTokenType.String, Value = sb.ToString() });
                        }
                        else
                        {
                            // 錯誤處理
                        }
                        _stringBuilderPool.Release(sb);
                        break;
                    case 't': // true
                        if (index + 3 < jsonSpan.Length && jsonSpan[index + 1] == 'r' && jsonSpan[index + 2] == 'u' && jsonSpan[index + 3] == 'e')
                        {
                            tokens.Add(new JsonToken { Type = JsonTokenType.Boolean, Value = "true" });
                            index += 4;
                            break;
                        }
                        // 錯誤處理
                        break;
                    case 'f': // false
                        if (index + 4 < jsonSpan.Length && jsonSpan[index + 1] == 'a' && jsonSpan[index + 2] == 'l' && jsonSpan[index + 3] == 's' && jsonSpan[index + 4] == 'e')
                        {
                            tokens.Add(new JsonToken { Type = JsonTokenType.Boolean, Value = "false" });
                            index += 5;
                            break;
                        }
                        // 錯誤處理
                        break;
                    case 'n': // null
                        if (index + 3 < jsonSpan.Length && jsonSpan[index + 1] == 'u' && jsonSpan[index + 2] == 'l' && jsonSpan[index + 3] == 'l')
                        {
                            tokens.Add(new JsonToken { Type = JsonTokenType.Null, Value = "null" });
                            index += 4;
                            break;
                        }
                        // 錯誤處理
                        break;
                    case '-':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        var numberSb = _stringBuilderPool.Get();
                        while (index < jsonSpan.Length && (char.IsDigit(jsonSpan[index]) || jsonSpan[index] == '.' || jsonSpan[index] == '-' || jsonSpan[index] == 'e' || jsonSpan[index] == 'E' || jsonSpan[index] == '+'))
                        {
                            numberSb.Append(jsonSpan[index]);
                            index++;
                        }
                        tokens.Add(new JsonToken { Type = JsonTokenType.Number, Value = numberSb.ToString() });
                        _stringBuilderPool.Release(numberSb);
                        break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        index++;
                        break;
                    default:
                        // 錯誤處理
                        index++;
                        break;
                }
            }
            _memoryPool.Release(nativeMemory);
            return tokens;
        }

        public class JsonValue {
                public enum ValueType {
                    Null,
                    Boolean,
                    Number,
                    String,
                    Object,
                    Array
                }

                public ValueType Type;
                public bool BooleanValue;
                public double NumberValue;
                public string StringValue;
                public Dictionary<string, JsonValue> ObjectValue;
                public List<JsonValue> ArrayValue;
            }
            private JsonValue Parse(List<JsonToken> tokens) {
                var result = ParseValue(tokens, 0, out _);
                _tokenListPool.Release(tokens);
                return result;
            }

            private JsonValue ParseValue(List<JsonToken> tokens, int index, out int nextIndex)
            {
                nextIndex = index;
                if(index >= tokens.Count) return null;
                var token = tokens[index];
                switch (token.Type) {
                case JsonTokenType.ObjectStart:
                    var obj = ParseObject(tokens, index, out nextIndex);
                    return obj;
                case JsonTokenType.ArrayStart:
                    var arr = ParseArray(tokens, index, out nextIndex);
                    return arr;
                case JsonTokenType.String:
                        nextIndex = index + 1;
                        return new JsonValue {Type = JsonValue.ValueType.String, StringValue = token.Value};
                case JsonTokenType.Number:
                    nextIndex = index + 1;
                    if(double.TryParse(token.Value, out var numberValue)) {
                        return new JsonValue { Type = JsonValue.ValueType.Number, NumberValue = numberValue};
                    }
                    // 錯誤處理
                    return new JsonValue{ Type = JsonValue.ValueType.Null};
                case JsonTokenType.Boolean:
                    nextIndex = index + 1;
                    if(bool.TryParse(token.Value, out var boolValue)) {
                        return new JsonValue {Type = JsonValue.ValueType.Boolean, BooleanValue = boolValue};
                    }
                    // 錯誤處理
                    return new JsonValue{ Type = JsonValue.ValueType.Null};
                case JsonTokenType.Null:
                        nextIndex = index + 1;
                    return new JsonValue{ Type = JsonValue.ValueType.Null};
                    default:
                        // 錯誤處理
                        return new JsonValue{ Type = JsonValue.ValueType.Null};
                }
            }

            private JsonValue ParseObject(List<JsonToken> tokens, int index, out int nextIndex) {
                nextIndex = index + 1;
                var obj = new JsonValue { Type = JsonValue.ValueType.Object, ObjectValue = new Dictionary<string, JsonValue>() };
                while (nextIndex < tokens.Count) {
                var token = tokens[nextIndex];
                if(token.Type == JsonTokenType.ObjectEnd)
                {
                    nextIndex++;
                    break;
                }

                if(token.Type == JsonTokenType.String)
                {
                    var key = token.Value;
                    if(nextIndex + 1 >= tokens.Count || tokens[nextIndex + 1].Type != JsonTokenType.Colon) {
                        // 錯誤處理
                        break;
                    }

                    var value = ParseValue(tokens, nextIndex + 2, out int valueNextIndex);
                    nextIndex = valueNextIndex;
                    obj.ObjectValue.Add(key, value);
                    
                        if (nextIndex < tokens.Count && tokens[nextIndex].Type == JsonTokenType.Comma)
                        {
                            nextIndex++;
                        }

                    }
                    else
                    {
                        // 錯誤處理
                        break;
                    }
                }
                return obj;
            }
            private JsonValue ParseArray(List<JsonToken> tokens, int index, out int nextIndex)
            {
            nextIndex = index + 1;
            var array = new JsonValue { Type = JsonValue.ValueType.Array, ArrayValue = new List<JsonValue>()};
            while(nextIndex < tokens.Count) {
                var token = tokens[nextIndex];
                if(token.Type == JsonTokenType.ArrayEnd) {
                    nextIndex++;
                    break;
                }
                var value = ParseValue(tokens, nextIndex, out int valueNextIndex);
                nextIndex = valueNextIndex;
                array.ArrayValue.Add(value);

                if (nextIndex < tokens.Count && tokens[nextIndex].Type == JsonTokenType.Comma)
                    {
                        nextIndex++;
                    }
            }
            return array;
        }

        public JsonValue ParseJson(string json) {
            var tokens = Lex(json);
            return Parse(tokens);
        }
            public async Task<JsonValue> ParseJsonAsync(string json)
            {
            return await Task.Run(() => {
                return ParseJson(json);
            });
            }
        }
}