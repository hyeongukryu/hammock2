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
var response = twitter.Users.Show.Json(screen_name: "danielcrenna");
var user = response.Body;

// Get the result
Console.WriteLine(user.ScreenName + ":" + user.Status.Text);
```

_Bring your own web stack_

hammock2 needs engines for HTTP and JSON. If you don't want to use the defaults then don't bring 
in the partial classes that implement them. If you take only the hammock.cs file, then you need to implement
these interfaces to provide your own serializer implementation:

```csharp
public interface IJsonConverter
{
    string DynamicToString(dynamic thing);
    IDictionary<string, object> StringToHash(string json);
    string HashToString(IDictionary<string, object> hash);
}

public interface IHttpEngine
{
    dynamic Request(string url, string method, NameValueCollection headers, dynamic body, bool trace, Action<Http> preRequest);
}
```

_Authentication_
- Option 1: pre-request request closure (so you can set headers on each outgoing request)
- Option 2: handler factory overload (if you're using the stock HttpClientEngine)

Todo:
- content negotiation / formatters
- async/await
- better triggers

