using System.Collections.Generic;
using ServiceStack.Text;

namespace hammock2
{
    public partial class Json
    {
        static Json()
        {
            Converter = new ServiceStackJsonConverter();
        }
    }

    public class ServiceStackJsonConverter : IJsonConverter
    {
        public string DynamicToString(dynamic thing)
        {
            var @string = JsonSerializer.SerializeToString(thing);
            return @string;
        }

        public IDictionary<string, object> StringToHash(string json)
        {
            var hash = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
            return hash;
        }

        public string HashToString(IDictionary<string, object> hash)
        {
            var @string = JsonSerializer.SerializeToString(hash);
            return @string;
        }
    }
}
