using System;
using System.Collections.Specialized;
using System.Net;
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
        public static Func<HttpClient> ClientFactory = () => new HttpClient(DefaultHandler);
        public static HttpClientHandler DefaultHandler = new HttpClientHandler
        {
            PreAuthenticate = true,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None
        };

        public dynamic Request(string url, string method, NameValueCollection headers, dynamic body, bool trace)
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
                content = new StringContent(HttpBody.Serialize(body));
            }
            request.Content = content;
            return BuildResponse(request, url, method);
        }

        public dynamic BuildResponse(HttpRequestMessage request, string url, string method)
        {
            var client = ClientFactory();
            foreach(var header in request.Headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            var requestMethod = method.Trim().ToUpperInvariant();
            var response = GetHttpResponse(client, url, request.Content, requestMethod);
            var bodyString = response.Content != null ? response.Content.ReadAsStringAsync().Result : null;
            
            // Content negotiation goes here...
            HttpBody body = bodyString != null ? HttpBody.Deserialize(bodyString) : null;
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

// Todo:
// MethodOverride is horrid
// Content negotiation both ways
// NTLM
//var ntlm = new HttpClientHandler();
//ntlm.UseDefaultCredentials = true;
//ntlm.PreAuthenticate = true;
//ntlm.ClientCertificateOptions = ClientCertificateOption.Automatic;
