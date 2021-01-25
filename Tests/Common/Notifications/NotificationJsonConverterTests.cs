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

namespace QuantConnect.Tests.Common.Notifications
{
    [TestFixture]
    public class NotificationJsonConverterTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void EmailRoundTrip(bool nullFields)
        {
            var expected = new NotificationEmail("p@p.com", "subjectP", null, null);
            if (!nullFields)
            {
                expected.Headers = new Dictionary<string, string> { { "key", "value" } };
                expected.Data = "dataContent";
            }

            var serialized = JsonConvert.SerializeObject(expected);

            var result = (NotificationEmail)JsonConvert.DeserializeObject<Notification>(serialized);

            Assert.AreEqual(expected.Subject, result.Subject);
            Assert.AreEqual(expected.Address, result.Address);
            Assert.AreEqual(expected.Data, result.Data);
            Assert.AreEqual(expected.Message, result.Message);
            Assert.AreEqual(expected.Headers, result.Headers);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SmsRoundTrip(bool nullFields)
        {
            var expected = new NotificationSms("123", nullFields ? null : "ImATextMessage");

            var serialized = JsonConvert.SerializeObject(expected);

            var result = (NotificationSms)JsonConvert.DeserializeObject<Notification>(serialized);

            Assert.AreEqual(expected.PhoneNumber, result.PhoneNumber);
            Assert.AreEqual(expected.Message, result.Message);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void WebRoundTrip(bool nullFields)
        {
            var expected = new NotificationWeb("qc.com",
                nullFields ? null : "JijiData",
                nullFields ? null : new Dictionary<string, string> { { "key", "value" } });

            var serialized = JsonConvert.SerializeObject(expected);

            var result = (NotificationWeb)JsonConvert.DeserializeObject<Notification>(serialized);

            Assert.AreEqual(expected.Address, result.Address);
            Assert.AreEqual(expected.Data, result.Data);
            Assert.AreEqual(expected.Headers, result.Headers);
        }
    }
}
