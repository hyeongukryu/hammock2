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
var reply = twitter.Users.Show.Json(screen_name: "danielcrenna");
var response = reply.Response;
var user = reply.Body;

// Get the result
Console.WriteLine(response.StatusCode + response.ReasonPhrase);
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
    dynamic Request(string url, string method, NameValueCollection headers, dynamic body, bool trace);
}
```

_Authentication_

*Use the `HttpAuth` helper:*
```
	_stripe = new Http("https://api.stripe.com/v1/");
	_stripeKey = ConfigurationManager.AppSettings["StripeTestKey"];
	_stripe.Auth = HttpAuth.Basic(_stripeKey);
```

*Pass in your own custom pre-request code:*
```
Action<Http> auth = http =>
{
    var authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(_stripeKey + ":"));
    http.Headers.Add("Authorization", "Basic " + authorization);
};
_stripe.Auth = auth;
```


Todo:
- blast dynamic body into POCOs if that's your thing
- content negotiation / formatters
- async/await



