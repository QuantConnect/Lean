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
 *
*/

using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Notifications;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Common.Packets
{
    [TestFixture]
    public class LiveNodePacketTests
    {
        [Test]
        public void NotificationRoundTrip()
        {
            var expectedEmail = new NotificationEmail("pipi@google.com", "crypto", null,
                null, new Dictionary<string, string> {{"header-key", "header-value"}});
            var packet = new LiveNodePacket
            {
                NotificationTargets = new List<Notification>
                {
                    expectedEmail,
                    new NotificationSms("123", null),
                    new NotificationWeb("www.pupu.com", headers: new Dictionary<string, string> {{"header-key", "header-value"}})
                }
            };

            var serialized = JsonConvert.SerializeObject(packet);

            var instance = JsonConvert.DeserializeObject<LiveNodePacket>(serialized);

            var email = instance.NotificationTargets[0] as NotificationEmail;
            Assert.IsNotNull(email);
            Assert.AreEqual(expectedEmail.Address, email.Address);
            Assert.AreEqual(expectedEmail.Subject, email.Subject);
            Assert.AreEqual(expectedEmail.Message, email.Message);
            Assert.AreEqual(expectedEmail.Data, email.Data);
            Assert.AreEqual(expectedEmail.Headers, email.Headers);

            var sms = instance.NotificationTargets[1] as NotificationSms;
            Assert.IsNotNull(sms);
            Assert.AreEqual("123", sms.PhoneNumber);
            Assert.AreEqual(null, sms.Message);

            var web = instance.NotificationTargets[2] as NotificationWeb;
            Assert.IsNotNull(web);
            Assert.AreEqual("www.pupu.com", web.Address);
            Assert.AreEqual(null, web.Data);
            Assert.AreEqual((packet.NotificationTargets[2] as NotificationWeb).Headers, web.Headers);
        }
    }
}
