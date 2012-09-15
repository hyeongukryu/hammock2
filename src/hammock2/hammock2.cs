using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace hammock2
{
    public interface IMediaConverter
    {
        string DynamicToString(dynamic instance);
        IDictionary<string, object> StringToHash(string json);
        string HashToString(IDictionary<string, object> hash);
        T DynamicTo<T>(dynamic instance);
        T StringTo<T>(string instance);
    }

    public partial class HttpBody : DynamicObject
    {
        private readonly IDictionary<string, object> _hash = new Dictionary<string, object>();
        
        protected internal static readonly Null Null = new Null();
        protected internal static readonly IMediaConverter Converter;
        
        public static string Serialize(dynamic instance)
        {
            return Converter.DynamicToString(instance);
        }
        public static dynamic Deserialize(string content)
        {
            return new HttpBody(Converter.StringToHash(content));
        }
        public static T Deserialize<T>(dynamic instance)
        {
            return Converter.DynamicTo<T>(instance);
        }
        public T Deserialize<T>(string content)
        {
            return Converter.StringTo<T>(content);
        }
        public T Deserialize<T>()
        {
            return Converter.DynamicTo<T>(this);
        }

        public HttpBody(IEnumerable<KeyValuePair<string, object>> hash)
        {
            _hash.Clear();
            foreach (var entry in hash ?? new Dictionary<string, object>())
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
            if (name.Equals("null"))
            {
                result = Null;
                return true;
            }
            return YieldMember(name, out result);
        }

        public override string ToString()
        {
            return Converter.HashToString(_hash);
        }

        private bool YieldMember(string name, out object result)
        {
            object value;
            if (_hash.TryGetValue(name, out value))
            {
                var json = value.ToString();
                if (json.TrimStart(' ').StartsWith("{"))
                {
                    var nested = Converter.StringToHash(json);
                    result = new HttpBody(nested);
                    return true;
                }
                result = json;
                return _hash[name] == result;
            }
            result = Null;
            return true;
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

    public class Null : DynamicObject
    {
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return true;
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this;
            return true;
        }
    }

    public interface IHttpEngine
    {
        dynamic Request(string url, string method, NameValueCollection headers, dynamic body, bool trace);
    }

    public partial class Http : DynamicObject
    {
        private static readonly IHttpEngine Engine;
        private UrlSegment _node;
        private readonly NameValueCollection _headers;
        private Action<Http> _auth;

        public string Endpoint { get; private set; }

        public NameValueCollection Headers
        {
            get { return _headers; }
        }

        public Action<Http> Auth
        {
            get { return _auth; }
            set { _auth = value; }
        }

        public bool Trace { get; set; }

        public Http(string endpoint)
        {
            if (endpoint.EndsWith("/")) endpoint = endpoint.TrimEnd('/');
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
            if (name.Equals("auth"))
            {
                if(value is Action<Http>)
                {
                    _auth = value as Action<Http>;
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
            if (name.Equals("auth"))
            {
                result = _auth;
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
        
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            var argTypes = args.Select(arg => arg.GetType()).ToArray();
            if(argTypes.Length == 0)
            {
                result = Get(Endpoint);
                return true;
            }
            var ctor = typeof(Http).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, argTypes, null);
            if (ctor == null)
            {
                result = null;
                return false;
            }
            result = ctor.Invoke(args);
            return true;
        }
        
        internal dynamic Get(string url)
        {
            if(_auth != null)
            {
                _auth(this);
            }
            return Engine.Request(url, "GET", _headers, null, Trace);
        }

        internal dynamic Post(string url, dynamic body)
        {
            if (_auth != null)
            {
                _auth(this);
            }
            return Engine.Request(url, "POST", _headers, body, Trace);
        }

        public class UrlSegment : DynamicObject
        {
            private const string PrivateParameter = "__";
            private UrlSegment _inner;
            private readonly Http _http;
            private string Name { get; set; }
            private string Separator { get; set; }

            public UrlSegment(Http client, string name)
            {
                _http = client;
                Name = name.Equals("dot") ? "" : name;
                Separator = name.Equals("dot") ? "." : "/";
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                var name = binder.Name.ToLower();
                if (_inner != null)
                {
                    return _inner.TryGetMember(binder, out result);
                }
                _inner = new UrlSegment(_http, name);
                result = _inner;
                return true;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                // A single nameless parameter means a POST entity
                dynamic body = null;
                if (binder.CallInfo.ArgumentCount == 1 && binder.CallInfo.ArgumentNames.Count == 0)
                {
                    body = HttpBody.Serialize(args[0]);
                }
                var url = BuildUrl(binder);
                var method = "GET";
                if (body != null)
                {
                    method = "POST";
                }
                var names = binder.CallInfo.ArgumentNames.Select(n => n.ToLowerInvariant()).ToList();
                method = GetMethodOverride(method, args, names);
                var queryString = BuildQueryString(names, args);
                dynamic response;
                switch (method)
                {
                    case "POST":
                        response = _http.Post(string.Concat(url, queryString), body);
                        break;
                    case "GET":
                    default:
                        response = _http.Get(string.Concat(url, queryString));
                        break;
                }
                result = response;
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
                if (_http._node != null)
                {
                    segments.Add(_http._node.Separator);
                    segments.Add(_http._node.Name);
                }
                WalkSegments(segments, _http._node);
                
                // Don't double count if we're invoking on the first segment
                if(segments.Count > 2)
                {
                    var last = binder.Name.ToLower();
                    segments.Add(last);     
                }
                
                var sb = new StringBuilder();
                sb.Append(_http.Endpoint);
                foreach (var segment in segments)
                {
                    sb.Append(segment);
                }
                var url = sb.ToString();
                return url;
            }

            //private string BuildUrl(InvokeMemberBinder binder, bool isLast = false)
            //{
            //    var segments = new List<string>();
            //    if (_http._node != null)
            //    {
            //        segments.Add(_http._node.Separator);
            //        segments.Add(_http._node.Name);
            //    }
            //    WalkSegments(segments, _http._node);

            //    //if(!isLast)
            //    //{
            //    //    var last = binder.Name.ToLower();
            //    //    segments.Add(last.Equals("json") ? "." : "/");
            //    //    segments.Add(last);    
            //    //}

            //    var sb = new StringBuilder();
            //    sb.Append(_http.Endpoint);
            //    foreach (var segment in segments)
            //    {
            //        sb.Append(segment);
            //    }
            //    var url = sb.ToString();
            //    return url;
            //}


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

    public class HttpReply
    {
        public HttpResponseMessage Response { get; set; }
        public HttpBody Body { get; set; }
    }
    
    public class HttpAuth
    {
        public static Action<Http> Basic(string token)
        {
            return Basic(token, "");
        }
        public static Action<Http> Basic(string username, string password)
        {
            return http =>
            {
                var authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
                http.Headers.Add("Authorization", "Basic " + authorization);
            };
        }
        public static Action<Http> Ntlm()
        {
            return http =>
            {
                // This breaks contract, since it relies on an implementation
                HttpClientEngine.PerRequestHandler = HttpClientEngine.NtlmHandler;
            };
        }
    }
}

