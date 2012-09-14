using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.Text;

namespace hammock2
{
    public class Http : DynamicObject
    {
        private UrlSegment _node;
        private readonly NameValueCollection _headers;
        private Action<Http> _authentication;

        private static readonly IDictionary<string, Action<HttpWebRequest, string>> _specialHeaders
            = new Dictionary<string, Action<HttpWebRequest, string>>(StringComparer.OrdinalIgnoreCase)
                  {
                      {"Accept", (r, v) => r.Accept = v},
                      {"Connection", (r, v) => r.Connection = v},
                      {"Content-Length", (r, v) => r.ContentLength = Convert.ToInt64(v)},
                      {"Content-Type", (r, v) => r.ContentType = v},
                      {"Expect", (r, v) => r.Expect = v},
                      {"Date", (r, v) => { /* Set by system */ }},
                      {"Host", (r, v) => { /* Set by system */ }},
                      {"If-Modified-Since", (r, v) => r.IfModifiedSince = Convert.ToDateTime(v)},
                      { "Range", (r, v) => { /* r.AddRange(); */ }}, 
                      {"Referer", (r, v) => r.Referer = v}, 
                      {"Transfer-Encoding", (r, v) => { r.TransferEncoding = v; r.SendChunked = true;}},
                      {"User-Agent", (r, v) => r.UserAgent = v}
                  };

        public string Endpoint { get; private set; }

        public NameValueCollection Headers
        {
            get { return _headers; }
        }

        public Action<Http> Authentication
        {
            get { return _authentication; }
            set { _authentication = value; }
        }

        public bool Trace { get; set; }

        public Http(string endpoint)
        {
            Endpoint = endpoint;
            _headers = new NameValueCollection();
        }

        public dynamic Query
        {
            get { return this; }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = binder.Name.ToLowerInvariant();

            if (name.Equals("authentication"))
            {
                if (value is Action<Http>)
                {
                    _authentication = (Action<Http>) value;
                }
            }

            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name.ToLowerInvariant();

            if (name.Equals("headers"))
            {
                result = _headers;
                return true;
            }

            if (name.Equals("authentication"))
            {
                result = _authentication;
                return true;
            }

            if (_node != null)
            {
                return _node.TryGetMember(binder, out result);
            }
            _node = new UrlSegment(this, name);
            result = _node;

            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var name = binder.Name.ToLowerInvariant();
            var node = _node ?? (_node = new UrlSegment(this, name));
            return node.TryInvokeMember(binder, args, out result);
        }

        internal string Get(string url)
        {
            return Request(url, "GET");
        }

        internal string Post(string url, dynamic body)
        {
            return Request(url, "POST");
        }

        private string Request(string url, string method)
        {
            if (_authentication != null)
            {
                _authentication(this);
            }

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = method;

            foreach (var name in _headers.AllKeys)
            {
                if (_specialHeaders.ContainsKey(name))
                {
                    _specialHeaders[name](request, _headers[name]);
                }
                else
                {
                    request.Headers.Add(name, _headers[name]);
                }
            }
            TraceRequest(request);

            var response = (HttpWebResponse) request.GetResponse();
            try
            {
                string result;
                using (var stream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(stream))
                    {
                        result = sr.ReadToEnd();
                        sr.Close();
                    }
                }
                return result;
            }
            catch (WebException ex)
            {
                string result = null;
                if (ex.Response is HttpWebResponse)
                {
                    var sr = new StreamReader(ex.Response.GetResponseStream());
                    result = sr.ReadToEnd();
                    sr.Close();
                }
                return result;
            }
        }

        [Conditional("TRACE")]
        private void TraceRequest(WebRequest request)
        {
            if (!Trace) return;
            var version = request is HttpWebRequest
                              ? string.Concat("HTTP/", ((HttpWebRequest) request).ProtocolVersion)
                              : "HTTP/v1.1";
            System.Diagnostics.Trace.WriteLine(string.Concat("--REQUEST: ", request.RequestUri.Scheme, "://",
                                                             request.RequestUri.Host));
            var pathAndQuery = string.Concat(request.RequestUri.AbsolutePath,
                                             string.IsNullOrEmpty(request.RequestUri.Query)
                                                 ? ""
                                                 : string.Concat(request.RequestUri.Query));
            System.Diagnostics.Trace.WriteLine(string.Concat(request.Method, " ", pathAndQuery, " ", version));
            TraceHeaders(request);
        }

        [Conditional("TRACE")]
        private void TraceHeaders(WebRequest request)
        {
            if (!Trace) return;
            var restricted =
                _specialHeaders.Keys.Where(key => !string.IsNullOrWhiteSpace(request.Headers[(string) key])).Select(
                    key => string.Concat(key, ": ", request.Headers[key]));
            var remaining =
                request.Headers.AllKeys.Except(_specialHeaders.Keys).Where(
                    key => !string.IsNullOrWhiteSpace(request.Headers[key])).Select(
                        key => string.Concat(key, ": ", request.Headers[key]));
            var all = restricted.ToList();
            all.AddRange(remaining);
            all.Sort();
            foreach (var trace in all)
            {
                System.Diagnostics.Trace.WriteLine(trace);
            }
        }

        private class UrlSegment : DynamicObject
        {
            private const string PrivateParameter = "__";
            private UrlSegment _inner;
            private readonly Http _client;
            private string Name { get; set; }
            private string Separator { get; set; }

            public UrlSegment(Http client, string name)
            {
                _client = client;
                Name = name;
                Separator = name.Equals("json") ? "." : "/";
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                var name = binder.Name.ToLower();
                if (_inner != null)
                {
                    return _inner.TryGetMember(binder, out result);
                }
                _inner = new UrlSegment(_client, name);
                result = _inner;
                return true;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                // A single nameless, parameter means a POST entity
                dynamic body = null;
                if (binder.CallInfo.ArgumentCount == 1 && binder.CallInfo.ArgumentNames.Count == 0)
                {
                    body = Json.Serialize(args[0]);
                }

                var names = binder.CallInfo.ArgumentNames.Select(n => n.ToLowerInvariant()).ToList();
                var url = BuildUrl(binder);

                var method = "GET";
                if (body != null)
                {
                    method = "POST";
                }
                method = GetMethodOverride(method, args, names);

                var queryString = BuildQueryString(names, args);

                string response;
                switch (method)
                {
                    case "POST":
                        if (body != null)
                        {
                            response = _client.Post(string.Concat(url, queryString), body);
                        }
                        else
                        {
                            response = _client.Post(string.Concat(url, queryString), null);
                        }
                        break;
                    case "GET":
                    default:
                        response = _client.Get(string.Concat(url, queryString));
                        break;
                }
                result = Json.Deserialize(response);
                return true;
            }

            private static string GetMethodOverride(string method, IList<object> args, IList<string> names)
            {
                foreach (var name in names.Where(n => n.StartsWith(PrivateParameter)))
                {
                    var parameter = name.Remove(0, 2);
                    if (!parameter.Equals("method"))
                    {
                        continue;
                    }
                    var index = names.IndexOf(name);
                    method = args[index].ToString();
                }
                return method;
            }

            private static string BuildQueryString(IList<string> names, IList<object> values)
            {
                var sb = new StringBuilder();
                if (names.Any())
                {
                    for (var i = 0; i < values.Count; i++)
                    {
                        var name = names[i];
                        if (name.StartsWith(PrivateParameter))
                        {
                            continue;
                        }
                        var value = values[i];
                        sb.Append(i == 0 ? "?" : "&");
                        sb.Append(name).Append("=").Append(Uri.EscapeDataString(value.ToString()));
                    }
                }
                return sb.ToString();
            }

            private string BuildUrl(InvokeMemberBinder binder)
            {
                var segments = new List<string>();
                if (_client._node != null)
                {
                    segments.Add(_client._node.Separator);
                    segments.Add(_client._node.Name);
                }
                WalkSegments(segments, _client._node);

                var last = binder.Name.ToLower();
                segments.Add(last.Equals("json") ? "." : "/");
                segments.Add(last);

                var sb = new StringBuilder();
                sb.Append(_client.Endpoint);
                foreach (var segment in segments)
                {
                    sb.Append(segment);
                }
                var url = sb.ToString();
                return url;
            }

            private static void WalkSegments(ICollection<string> segments, UrlSegment node)
            {
                if (node._inner == null)
                {
                    return;
                }
                segments.Add(node._inner.Separator);
                segments.Add(node._inner.Name);
                WalkSegments(segments, node._inner);
            }
        }
    }

    public class Json : DynamicObject
    {
        private readonly IDictionary<string, object> _hash = new Dictionary<string, object>();

        public static string Serialize(dynamic instance)
        {
            var json = JsonSerializer.SerializeToString(instance);
            return json;
        }

        public static dynamic Deserialize(string json)
        {
            var hash = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
            return new Json(hash);
        }

        public Json(IEnumerable<KeyValuePair<string, object>> hash)
        {
            _hash.Clear();
            foreach (var entry in hash)
            {
                _hash.Add(Underscored(entry.Key), entry.Value);
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var name = Underscored(binder.Name);
            _hash[name] = value;
            return _hash[name] == value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = Underscored(binder.Name);
            return YieldMember(name, out result);
        }

        public override string ToString()
        {
            return JsonSerializer.SerializeToString(_hash);
        }

        private bool YieldMember(string name, out object result)
        {
            if (_hash.ContainsKey(name))
            {
                var json = _hash[name].ToString();
                if (json.TrimStart(' ').StartsWith("{"))
                {
                    var nested = JsonSerializer.DeserializeFromString<IDictionary<string, object>>(json);
                    result = new Json(nested);
                    return true;
                }
                result = json;
                return _hash[name] == result;
            }
            result = null;
            return false;
        }

        internal static string Underscored(IEnumerable<char> pascalCase)
        {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var c in pascalCase)
            {
                if (char.IsUpper(c) && i > 0)
                {
                    sb.Append("_");
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString().ToLowerInvariant();
        }
    }
}