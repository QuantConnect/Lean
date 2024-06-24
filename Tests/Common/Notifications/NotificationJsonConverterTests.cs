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
using System.Text;
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

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void TelegramRoundTrip(bool nullMessage, bool nullToken)
        {
            var expected = new NotificationTelegram("pepe", nullMessage ? null : "ImAMessage", nullToken ? null : "botToken");

            var serialized = JsonConvert.SerializeObject(expected);

            var result = (NotificationTelegram)JsonConvert.DeserializeObject<Notification>(serialized);

            Assert.AreEqual(expected.Id, result.Id);
            Assert.AreEqual(expected.Message, result.Message);
            Assert.AreEqual(expected.Token, result.Token);
        }

        [Test]
        public void FtpWithPasswordRoundTrip([Values] bool secure, [Values] bool withPort)
        {
            var expected = new NotificationFtp(
                "qc.com",
                "username",
                "password",
                "path/to/file.json",
                Encoding.ASCII.GetBytes("{}"),
                secure,
                withPort ? 2121: null);

            var serialized = JsonConvert.SerializeObject(expected);
            var result = (NotificationFtp)JsonConvert.DeserializeObject<Notification>(serialized);

            Assert.AreEqual(expected.Hostname, result.Hostname);
            Assert.AreEqual(expected.Username, result.Username);
            Assert.AreEqual(expected.Password, result.Password);
            Assert.AreEqual(expected.FilePath, result.FilePath);
            Assert.AreEqual(expected.FileContent, result.FileContent);
            Assert.AreEqual(expected.Port, result.Port);
            Assert.AreEqual(expected.Secure, result.Secure);
            Assert.IsNull(result.PublicKey);
            Assert.IsNull(result.PrivateKey);
            Assert.IsNull(result.PrivateKeyPassphrase);
        }

        [Test]
        public void FtpWithKeyRoundTrip([Values] bool withPort, [Values] bool withPassphrase)
        {
            var expected = new NotificationFtp(
                "qc.com",
                "username",
                "publickey",
                "privatekey",
                "path/to/file.json",
                Encoding.ASCII.GetBytes("{}"),
                withPort ? 2121 : null,
                withPassphrase ? "passphrase" : null);

            var serialized = JsonConvert.SerializeObject(expected);
            var result = (NotificationFtp)JsonConvert.DeserializeObject<Notification>(serialized);

            Assert.AreEqual(expected.Hostname, result.Hostname);
            Assert.AreEqual(expected.Username, result.Username);
            Assert.AreEqual(expected.PublicKey, result.PublicKey);
            Assert.AreEqual(expected.PrivateKey, result.PrivateKey);
            Assert.AreEqual(expected.FilePath, result.FilePath);
            Assert.AreEqual(expected.FileContent, result.FileContent);
            Assert.AreEqual(expected.Port, result.Port);
            Assert.IsNull(result.Password);
            Assert.IsTrue(result.Secure);

            if (withPassphrase)
            {
                Assert.AreEqual(expected.PrivateKeyPassphrase, result.PrivateKeyPassphrase);
            }
            else
            {
                Assert.IsNull(result.PrivateKeyPassphrase);
            }
        }

        [Test]
        public void CaseInsensitive()
        {
            var serialized = @"[{
			""address"": ""p@p.com"",
			""subject"": ""sdads""
		}, {
			""phoneNumber"": ""11111111111""
		}, {
			""headers"": {
				""1"": ""2""
			},
			""address"": ""qc.com""
		}, {
			""address"": ""qc.com/1234""
		},{
			""hostname"": ""qc.com"",
			""username"": ""username"",
			""password"": ""password"",
			""filePath"": ""path/to/file.csv"",
			""fileContent"": ""abcde"",
			""secure"": ""true"",
			""port"": 2222
		},{
			""hostname"": ""qc.com"",
			""username"": ""username"",
			""password"": ""password"",
			""filePath"": ""path/to/file.csv"",
			""filecontent"": ""abcde"",
			""secure"": ""false"",
			""port"": 2222
		},{
			""hostname"": ""qc.com"",
			""username"": ""username"",
            ""publickey"": ""publickey"",
            ""privatekey"": ""privatekey"",
            ""privatekeyPassphrase"": ""privatekeyPassphrase"",
			""filePath"": ""path/to/file.csv"",
			""filecontent"": ""abcde"",
			""secure"": ""false"",
			""port"": 2222
		}]";
            var result = JsonConvert.DeserializeObject<List<Notification>>(serialized);

            Assert.AreEqual(7, result.Count);

            var email = result[0] as NotificationEmail;
            Assert.AreEqual("sdads", email.Subject);
            Assert.AreEqual("p@p.com", email.Address);

            var sms = result[1] as NotificationSms;
            Assert.AreEqual("11111111111", sms.PhoneNumber);

            var web = result[2] as NotificationWeb;
            Assert.AreEqual(1, web.Headers.Count);
            Assert.AreEqual("2", web.Headers["1"]);
            Assert.AreEqual("qc.com", web.Address);

            var web2 = result[3] as NotificationWeb;
            Assert.AreEqual("qc.com/1234", web2.Address);

            var ftp = result[4] as NotificationFtp;
            Assert.AreEqual("qc.com", ftp.Hostname);
            Assert.AreEqual("username", ftp.Username);
            Assert.AreEqual("password", ftp.Password);
            Assert.AreEqual("path/to/file.csv", ftp.FilePath);
            Assert.AreEqual("abcde", ftp.FileContent);
            Assert.IsTrue(ftp.Secure);
            Assert.AreEqual(2222, ftp.Port);
            Assert.IsNull(ftp.PublicKey);
            Assert.IsNull(ftp.PrivateKey);
            Assert.IsNull(ftp.PrivateKeyPassphrase);

            var ftp2 = result[5] as NotificationFtp;
            Assert.AreEqual("qc.com", ftp2.Hostname);
            Assert.AreEqual("username", ftp2.Username);
            Assert.AreEqual("password", ftp2.Password);
            Assert.AreEqual("path/to/file.csv", ftp2.FilePath);
            Assert.AreEqual("abcde", ftp2.FileContent);
            Assert.IsFalse(ftp2.Secure);
            Assert.AreEqual(2222, ftp2.Port);
            Assert.IsNull(ftp.PublicKey);
            Assert.IsNull(ftp.PrivateKey);
            Assert.IsNull(ftp.PrivateKeyPassphrase);

            var ftp3 = result[6] as NotificationFtp;
            Assert.AreEqual("qc.com", ftp3.Hostname);
            Assert.AreEqual("username", ftp3.Username);
            Assert.AreEqual("publickey", ftp3.PublicKey);
            Assert.AreEqual("privatekey", ftp3.PrivateKey);
            Assert.AreEqual("privatekeyPassphrase", ftp3.PrivateKeyPassphrase);
            Assert.AreEqual("path/to/file.csv", ftp3.FilePath);
            Assert.AreEqual("abcde", ftp3.FileContent);
            Assert.IsTrue(ftp3.Secure);
            Assert.AreEqual(2222, ftp3.Port);
            Assert.IsNull(ftp3.Password);
        }
    }
}
