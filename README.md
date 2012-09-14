hammock2
========
A single .cs file for making munchy munchy API. Fully dynamic, so no "client library" required.

_LOL jk there's dependencies_
```
	PM> Install-Package Microsoft.Net.Http	# Or provide your own implementation of IHttpEngine
	PM> Install-Package ServiceStack.Text	# Or provide your own implementation of IJsonConverter
	PM> Install-Package AsyncBridge
```

Usage:
```csharp
// Make the request
dynamic twitter = new Http("http://api.twitter.com");
var user = twitter.Users.Show.Json(screen_name: "danielcrenna");

// Get the result
Console.WriteLine(user.ScreenName + ":" + user.Status.Text);
```

_Bring your own serializer_

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

_Authentication_
- Option 1: pre-request request closure (so you can set headers on each outgoing request)
- Option 2: handler factory overload (if you're using the stock HttpClientEngine)

Todo:
- content negotiation / formatters
- async/await
- better triggers

