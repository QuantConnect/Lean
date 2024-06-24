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

using NUnit.Framework;
using QuantConnect.Notifications;
using System;
using System.Text;

namespace QuantConnect.Tests.Common.Notifications
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class NotificationFtpTests
    {
        private byte[] _testContent = Encoding.ASCII.GetBytes("{}");

        [Test]
        public void PortDefaultsTo21()
        {
            var notification = new NotificationFtp("qc.com", "username", "password", "path/to/file.json", _testContent);
            Assert.AreEqual(21, notification.Port);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ThrowsOnMissingPassword(string password)
        {
            Assert.Throws<ArgumentException>(() => new NotificationFtp("qc.com", "username", password, "path/to/file.json", _testContent));
        }

        [TestCase(null, "privatekey")]
        [TestCase("", "privatekey")]
        [TestCase("publickey", null)]
        [TestCase("publickey", "")]
        public void ThrowsOnMissingSSHKeys(string publicKey, string privateKey)
        {
            Assert.Throws<ArgumentException>(() => new NotificationFtp("qc.com", "username", publicKey, privateKey, "path/to/file.json", _testContent));
        }

        // Protocol as in a URI
        [TestCase(@"ftp://qc.com")]
        [TestCase(@"sftp://qc.com")]
        [TestCase(@"http://qc.com")]
        [TestCase(@"https://qc.com")]
        // Trailing slashes
        [TestCase(@"qc.com/")]
        [TestCase(@"qc.com//")]
        [TestCase(@"qc.com")]
        public void NormalizesHostname(string hostname)
        {
            var notification = new NotificationFtp(hostname, "username", "password", "path/to/file.json", _testContent);
            Assert.AreEqual("qc.com", notification.Hostname);
        }

        [Test]
        public void EncodesFileContent()
        {
            var contentStr = @"{""someKey"": ""this is a sample json file"", ""anotherKey"": 123456}";
            var contentBytes = Encoding.ASCII.GetBytes(contentStr);
            var notification = new NotificationFtp("qc.com", "username", "password", "path/to/file.json", contentBytes);

            var decodedBytes = Convert.FromBase64String(notification.FileContent);
            var decodedStr = Encoding.ASCII.GetString(decodedBytes);

            Assert.AreEqual(contentStr, decodedStr);
        }
    }
}
