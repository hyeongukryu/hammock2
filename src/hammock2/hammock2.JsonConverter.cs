using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace hammock2
{
    public partial class HttpBody
    {
        static HttpBody()
        {
            Converter = new ServiceStackJsonConverter();
        }
    }

    public class ServiceStackJsonConverter : IMediaConverter
    {
        public string DynamicToString(dynamic instance)
        {
            var @string = JsonSerializer.SerializeToString(instance);
            return @string;
        }
        public IDictionary<string, object> StringToHash(string json)
        {
            var hash = JsonSerializer.DeserializeFromString<JsonObject>(json);
            var result = hash.ToDictionary<KeyValuePair<string, string>, string, object>(entry => entry.Key, entry => entry.Value);
            return result;
        }
        public string HashToString(IDictionary<string, object> hash)
        {
            var @string = JsonSerializer.SerializeToString(hash);
            return @string;
        }
        public T DynamicTo<T>(dynamic instance)
        {
            var @string = instance.ToString();
            return StringTo<T>(@string); // <-- Two pass, could be faster
        }
        public T StringTo<T>(string instance)
        {
            return JsonSerializer.DeserializeFromString<T>(instance);
        }
    }
}
