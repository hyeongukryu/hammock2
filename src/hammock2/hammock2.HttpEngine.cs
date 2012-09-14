using System;
using System.Collections.Specialized;
using System.Net.Http;

namespace hammock2
{
    public partial class Http
    {
        static Http()
        {
            Engine = new HttpClientEngine();
        }
    }
    public class HttpClientEngine : IHttpEngine
    {
        public static Func<HttpClient> ClientFactory = () => new HttpClient();

        public dynamic Request(string url, string method, NameValueCollection headers, bool trace, dynamic body)
        {
            var request = new HttpRequestMessage();
            foreach (var name in headers.AllKeys)
            {
                var value = headers[name];
                request.Headers.Add(name, value);
            }
            HttpContent content = null;
            if (body != null)
            {
                content = new StringContent(Json.Serialize(body));
            }
            return BuildResponse(request, url, method, content);
        }

        public dynamic BuildResponse(HttpRequestMessage message, string url, string method, HttpContent content = null)
        {
            var client = ClientFactory();
            var requestMethod = method.Trim().ToUpperInvariant();
            var response = GetHttpResponse(client, url, content, requestMethod);
            var bodyString = response.Content != null ? response.Content.ReadAsStringAsync().Result : null;
            
            // Content negotiation goes here...
            Json body = bodyString != null ? Json.Deserialize(bodyString) : null;
            return new HttpReply
            {
                Body = body,
                Response = response
            };
        }

        private static HttpResponseMessage GetHttpResponse(HttpClient client, string url, HttpContent content, string requestMethod)
        {
            HttpResponseMessage response;
            switch (requestMethod)
            {
                case "GET":
                    response = client.GetAsync(url).Result;
                    break;
                case "PUT":
                    response = client.PutAsync(url, content).Result;
                    break;
                case "DELETE":
                    response = client.DeleteAsync(url).Result;
                    break;
                case "POST":
                    response = client.PostAsync(url, content).Result;
                    break;
                default:
                    throw new NotSupportedException(requestMethod);
            }
            return response;
        }
    }
}
