using System.Collections.Generic;
using ServiceStack.Text;

namespace hammock2
{
    // Calling this DI would be generous at best
    public partial class Json
    {
        static Json()
        {
            Thing = new ServiceStackJsonThing();
        }
    }
    public class ServiceStackJsonThing : JsonThing
    {
        public string DynamicToString(dynamic thing)
        {
            return JsonSerializer.SerializeToString(thing);
        }
        public IDictionary<string, object> StringToHash(string json)
        {
            return JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
        }
    }
}
