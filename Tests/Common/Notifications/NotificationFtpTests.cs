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
using NUnit.Framework;
using QuantConnect.Notifications;

namespace QuantConnect.Tests.Common.Notifications
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class NotificationFtpTests
    {
        [Test]
        public void PortDefaultsTo21()
        {
            var notification = new NotificationFtp("qc.com", "username", "password", "path/to/file.json", "{}");
            Assert.AreEqual(21, notification.Port);
        }

        // Protol as in a URI
        [TestCase(@"ftp://qc.com", false)]
        [TestCase(@"sftp://qc.com", false)]
        [TestCase(@"http://qc.com", false)]
        [TestCase(@"https://qc.com", false)]
        // Trailing slashes
        [TestCase(@"qc.com/", false)]
        [TestCase(@"qc.com//", false)]
        [TestCase(@"qc.com", true)]
        public void ConstructorThrowsOnInvalidHostname(string hostname, bool isValid)
        {
            TestDelegate ctor = () => new NotificationFtp(hostname, string.Empty, string.Empty, string.Empty, string.Empty);

            if (isValid)
            {
                Assert.DoesNotThrow(ctor);
            }
            else
            {
                Assert.Throws<ArgumentException>(ctor);
            }
        }
    }
}
