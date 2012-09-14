using System.IO;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace hammock2.Tests.API
{
    public class FakeController : ApiController
    {
        public static void RegisterRoutes(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(name: "GetWithNoParameters", routeTemplate: "", defaults: new { Controller = "Fake", Action = "GetWithNoParameters" });
            config.Routes.MapHttpRoute(name: "GetEntity_TwitterUsersShow", routeTemplate: "users/show.json", defaults: new { Controller = "Fake", Action = "GetEntity", filename = "twitter_users_show.json" });
        }

        public HttpResponseMessage GetWithNoParameters()
        {
            return new HttpResponseMessage();
        }

        public HttpResponseMessage GetEntity(string filename)
        {
            var content = new StringContent(File.ReadAllText(Path.Combine("API/Entities", filename)), Encoding.UTF8);
            var response = new HttpResponseMessage();
            response.Content = content;
            return response;
        }
    }
}