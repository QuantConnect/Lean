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

using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class VersionHelperTests
    {
        [Test]
        public void CompareVersionsCorrectly()
        {
            const string version = "1.2.3.4";
            Assert.AreEqual(0, VersionHelper.CompareVersions(version, version));

            string oldVersion = "1.2.3.3";
            Assert.AreEqual(-1, VersionHelper.CompareVersions(oldVersion, version));

            oldVersion = "1.1.9.9";
            Assert.AreEqual(-1, VersionHelper.CompareVersions(oldVersion, version));

            oldVersion = "0.9.9.9";
            Assert.AreEqual(-1, VersionHelper.CompareVersions(oldVersion, version));

            string newVersion = "1.2.3.5";
            Assert.AreEqual(1, VersionHelper.CompareVersions(newVersion, version));

            newVersion = "1.2.4.0";
            Assert.AreEqual(1, VersionHelper.CompareVersions(newVersion, version));

            newVersion = "1.3.0.0";
            Assert.AreEqual(1, VersionHelper.CompareVersions(newVersion, version));

            newVersion = "2.0.0.0";
            Assert.AreEqual(1, VersionHelper.CompareVersions(newVersion, version));
        }
    }
}
