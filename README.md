hammock2
========
A single .cs file for making munchy munchy API. Fully dynamic, so no "client library" required.

Usage:
```csharp
// Make the request
dynamic twitter = new Http("http://api.twitter.com");
var user = twitter.Users.Show.Json(screen_name: "danielcrenna");

// Get the result
Console.WriteLine(user.ScreenName + ":" + user.Status.Text);
```

Bring your own serializer:
hammock2 needs something to serialize JSON with. If you don't want to use ServiceStack then don't bring 
in the partial class that implements it. If you take only the hammock.cs file, then add the following lines 
of code to provide your own serializer implementation:
```csharp
using System.Collections.Generic;
using ServiceStack.Text;
namespace hammock2
{
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
```

Todo:
- Really just a prototype right now, needs real use to flush out trigger methods
- Use the new HttpClient with async/await
- Maybe an embeddable serializer if it's worth being a true "single file"; but SS is too fast to ignore
