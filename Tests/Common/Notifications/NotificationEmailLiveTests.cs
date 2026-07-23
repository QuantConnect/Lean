/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Notifications;
using QuantConnect.Tests.API;

namespace QuantConnect.Tests.Common.Notifications
{
    /// <summary>
    /// Live test: enqueues an email through Notify.Email with a null address and sends it
    /// through the QuantConnect cloud API to verify it is emailed to all members in the project.
    /// Requires "job-user-id", "api-access-token" and "notifications-endpoint" in the config.
    /// </summary>
    [TestFixture, Explicit("Sends a real email through the QuantConnect cloud API")]
    public class NotificationEmailLiveTests
    {
        // Create a project on QC Cloud with more than one member and set its ID here:
        // every member of the project should receive the test email
        private const int ProjectId = 34485046;

        [Test]
        public void NotifyEmailWithNullAddress_SendsToAllProjectMembers()
        {
            // enqueue the email like an algorithm would: self.notify.email(None, ...)
            var notify = new NotificationManager(liveMode: true);
            Assert.IsTrue(notify.Email(null, $"Notify.Email All Members Test - Project {ProjectId}",
                $"Sent by NotificationEmailLiveTests at {DateTime.UtcNow:u} with a null address."));

            // dequeue and send, like the live result handler hands notifications to the messaging handler
            Assert.IsTrue(notify.Messages.TryDequeue(out var notification));
            var email = notification as NotificationEmail;
            Assert.IsNotNull(email);
            Assert.IsNull(email.Address);

            Assert.IsTrue(Send(notification));
        }

        private static bool Send(Notification notification)
        {
            ApiTestBase.ReloadConfiguration();
            var endpoint = Config.Get("notifications-endpoint");
            if (string.IsNullOrEmpty(endpoint))
            {
                Assert.Ignore("The 'notifications-endpoint' configuration is not set");
            }
            var userId = Globals.UserId.ToStringInvariant();
            var apiToken = Globals.UserToken;

            var serialized = JsonConvert.SerializeObject(notification,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            StringAssert.Contains("\"Address\":null", serialized);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToStringInvariant();
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{apiToken}:{timestamp}")))
                .ToLowerInvariant();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Timestamp", timestamp);
            client.DefaultRequestHeaders.Add("Authorization",
                "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userId}:{hash}")));

            using var response = client.PostAsync(endpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "projectId", ProjectId.ToStringInvariant() },
                    { "notification", serialized }
                })).Result;
            var body = response.Content.ReadAsStringAsync().Result;

            Assert.IsTrue(response.IsSuccessStatusCode, body);
            return JObject.Parse(body).Value<bool>("success");
        }
    }
}
