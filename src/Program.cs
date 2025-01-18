using System;
using LowGCJsonParser;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // 建立 JsonParser 實例
        JsonParser parser = new JsonParser();

        // 測試 JSON 字串
        string jsonString1 = @"{""name"":""John"", ""age"":30, ""city"":""New York""}";
        string jsonString2 = @"
            {
              ""items"": [
                {
                  ""id"": 1,
                  ""name"": ""Apple"",
                  ""price"": 1.2
                },
                {
                  ""id"": 2,
                  ""name"": ""Banana"",
                  ""price"": 0.8
                }
              ],
              ""total"": 2
            }
        ";
        // 使用同步方法解析 JSON
        Console.WriteLine("同步解析 JSON:");
        try
        {
          var result1 = parser.ParseJson(jsonString1);
          PrintJsonValue(result1);
        }
        catch(Exception e)
        {
          Console.WriteLine($"解析 JSON 時發生錯誤：{e.Message}");
        }


        // 使用非同步方法解析 JSON
        Console.WriteLine("\n非同步解析 JSON:");
        try
        {
           var result2 = await parser.ParseJsonAsync(jsonString2);
           PrintJsonValue(result2);
        }
        catch(Exception e)
        {
          Console.WriteLine($"解析 JSON 時發生錯誤：{e.Message}");
        }


        Console.WriteLine("\n完成!");
    }

    static void PrintJsonValue(JsonParser.JsonValue value, int indent = 0)
    {
        if (value == null)
        {
            Console.WriteLine("null");
            return;
        }

        string indentStr = new string(' ', indent * 2);

        switch (value.Type)
        {
            case JsonParser.JsonValue.ValueType.Null:
                Console.WriteLine($"{indentStr}null");
                break;
            case JsonParser.JsonValue.ValueType.Boolean:
                Console.WriteLine($"{indentStr}{value.BooleanValue}");
                break;
            case JsonParser.JsonValue.ValueType.Number:
                Console.WriteLine($"{indentStr}{value.NumberValue}");
                break;
            case JsonParser.JsonValue.ValueType.String:
                Console.WriteLine($"{indentStr}\"{value.StringValue}\"");
                break;
            case JsonParser.JsonValue.ValueType.Object:
                Console.WriteLine($"{indentStr}{{");
                foreach (var kvp in value.ObjectValue)
                {
                    Console.Write($"{indentStr}  \"{kvp.Key}\": ");
                    PrintJsonValue(kvp.Value, indent + 2);
                }
                Console.WriteLine($"{indentStr}}}");
                break;
            case JsonParser.JsonValue.ValueType.Array:
                Console.WriteLine($"{indentStr}[");
                 if(value.ArrayValue != null){
                   foreach(var item in value.ArrayValue){
                      PrintJsonValue(item, indent + 2);
                   }
                }

                Console.WriteLine($"{indentStr}]");
                break;
        }
    }
}