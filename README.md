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

Todo:
- Really just a prototype right now, needs real use to flush out trigger methods
- Use the new HttpClient with async/await
- Maybe an embeddable serializer if it's worth being a true "single file"; but SS is too fast to ignore
