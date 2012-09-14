using System;
using NUnit.Framework;

namespace hammock2.Tests
{
    [TestFixture]
    public class HttpTests
    {
        [Test]
        public void Can_get_an_entity_from_a_url()
        {
            // URL: http://api.twitter.com/users/show.format 
            dynamic twitter = new Http("http://api.twitter.com");
            var user = twitter.Users.Show.Json(screen_name: "danielcrenna");
            Assert.IsNotNull(user);
            Assert.AreEqual("danielcrenna", user.ScreenName);
            Console.WriteLine(user.ScreenName + ":" + user.Status.Text);
        }

        [Test]
        public void Can_post_an_entity_from_a_url()
        {
            // URL: http://api.twitter.com/users/show.format 
            dynamic twitter = new Http("http://api.twitter.com");
            twitter.Trace = true;
            twitter.Users.Show.Json(new { Purpose = "To rock!"});
        }

        [Test]
        public void Can_define_headers()
        {
            dynamic twitter = new Http("http://api.twitter.com");
            twitter.Headers.Add("bob", "loblaw");
            Assert.AreEqual(1, twitter.Headers.Count);
        }

        [Test]
        public void Can_define_authentication_function()
        {
            var twitter = new Http("http://api.twitter.com");
            twitter.Headers.Add("bob", "loblaw");
            twitter.Authentication = http => http.Headers.Add("X-Blah-Sauce", "Wizzywig!");
            var user = twitter.Query.Users.Show.Json(screen_name: "danielcrenna");

            Assert.IsNotNull(user);
            Assert.AreEqual("danielcrenna", user.ScreenName);
            Console.WriteLine(user.ScreenName + ":" + user.Status.Text);
        }
    }
}
