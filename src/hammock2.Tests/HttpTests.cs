﻿using System;
using System.Net;
using NUnit.Framework;
using hammock2.Tests.API;

namespace hammock2.Tests
{
    // TODO
    // Test for basic auth
    // Test to confirm query string received
    // Test POST entities

    [TestFixture]
    public class HttpTests
    {
        [TestFixtureSetUp]
        public void SetUp()
        {
            FakeServer.Configure(FakeController.RegisterRoutes);
            HttpClientEngine.ClientFactory = () => FakeServer.CreateClientForAServerOnPort(8787);
        }

        [Test]
        public void Can_define_headers()
        {
            var http = DynamicHttp();
            http.Headers.Add("bob", "loblaw");
            Assert.AreEqual(1, http.Headers.Count);
        }

        [Test]
        public void Request_parameterless_get()
        {
            var http = DynamicHttp();
            var result = http();
            Assert.IsNotNull(result);
            Assert.AreEqual(HttpStatusCode.OK, result.Response.StatusCode);
            Assert.IsNull(result.Body);
        }

        [Test]
        public void Can_get_an_entity_from_a_url()
        {
            // users/show.json?screen_name=danielcrenna
            var twitter = DynamicHttp();
            var result = twitter.Users.Show.Json(screen_name: "danielcrenna");
            var user = result.Body;
            Assert.IsNotNull(user);
            Assert.AreEqual("danielcrenna", user.ScreenName);
            Console.WriteLine(user.ScreenName + ":" + user.Status.Text);
        }

        private static dynamic DynamicHttp()
        {
            dynamic http = new Http("http://localhost:8787");
            return http;
        }
    }
}
