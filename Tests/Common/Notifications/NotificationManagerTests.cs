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
using System.Text;
using NUnit.Framework;
using Python.Runtime;
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
            Assert.AreEqual(
                _liveMode,
                _notify.Email("address@domain.com", "subject", "message", "data")
            );
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
        public void TelegramAddsNotificationToMessagesWhenLiveModeIsTrue()
        {
            Assert.AreEqual(_liveMode, _notify.Telegram("pepe", "ImAMessage", "botToken"));
            Assert.AreEqual(_liveMode ? 1 : 0, _notify.Messages.Count);
            if (_liveMode)
            {
                Assert.IsInstanceOf<NotificationTelegram>(_notify.Messages.Single());
            }
        }

        [Test]
        public void FtpAddsNotificationToMessagesWhenLiveModeIsTrue()
        {
            Assert.AreEqual(
                _liveMode,
                _notify.Ftp(
                    "qc.com",
                    "username",
                    "password",
                    "path/to/file.json",
                    Encoding.ASCII.GetBytes("{}")
                )
            );
            Assert.AreEqual(_liveMode ? 1 : 0, _notify.Messages.Count);
            if (_liveMode)
            {
                Assert.IsInstanceOf<NotificationFtp>(_notify.Messages.Single());
            }
        }

        [Test]
        public void FtpAddsNotificationToMessagesWhenLiveModeIsTrueFromStringContents()
        {
            Assert.AreEqual(
                _liveMode,
                _notify.Ftp("qc.com", "username", "password", "path/to/file.json", "{}")
            );
            Assert.AreEqual(_liveMode ? 1 : 0, _notify.Messages.Count);
            if (_liveMode)
            {
                Assert.IsInstanceOf<NotificationFtp>(_notify.Messages.Single());
            }
        }

        [Test]
        [TestCase("email")]
        [TestCase("sms")]
        [TestCase("web")]
        [TestCase("telegram")]
        [TestCase("ftp")]
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

                    case "telegram":
                        result = _notify.Telegram("pepe", "ImAMessage", "botToken");
                        break;

                    case "ftp":
                        result = _notify.Ftp(
                            "qc.com",
                            "username",
                            "password",
                            "path/to/file.json",
                            Encoding.ASCII.GetBytes("{}")
                        );
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

        [TestCase("email")]
        [TestCase("web")]
        public void PythonOverloads(string notificationType)
        {
            using (Py.GIL())
            {
                dynamic function;
                bool result;
                var test = PyModule.FromString(
                    "testModule",
                    @"
from AlgorithmImports import *

def email(notifier):
    headers = {'header-key': 'header-value'}
    return notifier.Email('me@email.com', 'subject', 'message', 'data', headers)
    
def web(notifier):
    headers = {'header-key': 'header-value'}
    data = {'objectA':'valueA', 'objectB':{'PropertyA':10, 'PropertyB':'stringB'}}
    return notifier.Web('api.quantconnect.com', data, headers)"
                );

                switch (notificationType)
                {
                    case "email":
                        function = test.GetAttr("email");
                        result = function(_notify);
                        break;

                    case "web":
                        function = test.GetAttr("web");
                        result = function(_notify);
                        break;

                    default:
                        throw new ArgumentException($"Invalid method: {notificationType}");
                }

                if (_liveMode)
                {
                    Assert.IsTrue(result);
                }
                else
                {
                    Assert.IsFalse(result);
                }
            }
        }
    }
}
