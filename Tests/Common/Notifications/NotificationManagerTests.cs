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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Notifications;

namespace QuantConnect.Tests.Common.Notifications
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class NotificationManagerTests
    {
        private readonly bool _liveMode;
        private NotificationManager _notify;

        public NotificationManagerTests(bool liveMode)
        {
            _liveMode = liveMode;
        }

        [SetUp]
        public void Setup()
        {
            _notify = new NotificationManager(_liveMode);
        }

        [Test]
        public void Email_AddsNotificationEmail_ToMessages_WhenLiveModeIsTrue()
        {
            Assert.AreEqual(_liveMode, _notify.Email("address@domain.com", "subject", "message", "data"));
            Assert.AreEqual(_liveMode ? 1 : 0, _notify.Messages.Count);
            if (_liveMode)
            {
                Assert.IsInstanceOf<NotificationEmail>(_notify.Messages.Single());
            }
        }

        [Test]
        public void Sms_AddsNotificationSms_ToMessages_WhenLiveModeIsTrue()
        {
            Assert.AreEqual(_liveMode, _notify.Sms("phone-number", "message"));
            Assert.AreEqual(_liveMode ? 1 : 0, _notify.Messages.Count);
            if (_liveMode)
            {
                Assert.IsInstanceOf<NotificationSms>(_notify.Messages.Single());
            }
        }

        [Test]
        public void Web_AddsNotificationWeb_ToMessages_WhenLiveModeIsTrue()
        {
            Assert.AreEqual(_liveMode, _notify.Web("address", "data"));
            Assert.AreEqual(_liveMode ? 1 : 0, _notify.Messages.Count);
            if (_liveMode)
            {
                Assert.IsInstanceOf<NotificationWeb>(_notify.Messages.Single());
            }
        }

        [Test]
        [TestCase("email")]
        [TestCase("sms")]
        [TestCase("web")]
        public void RateLimits_Notifications_AfterThirtyCalls(string method)
        {
            for (var invocationNumber = 1; invocationNumber <= 31; invocationNumber++)
            {
                bool result;
                switch (method)
                {
                    case "email":
                        result = _notify.Email("address@domain.com", "subject", "message", "data");
                        break;

                    case "sms":
                        result = _notify.Sms("phone-number", "message");
                        break;

                    case "web":
                        result = _notify.Web("address", "data");
                        break;

                    default:
                        throw new ArgumentException($"Invalid method: {method}");
                }

                if (_liveMode && invocationNumber <= 30)
                {
                    Assert.IsTrue(result);
                }
                else
                {
                    Assert.IsFalse(result);
                    if (!_liveMode)
                    {
                        // no need to test further
                        Assert.Pass();
                    }
                }
            }
        }
    }
}
