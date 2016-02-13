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
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class LocalDiskMapFileProviderTests
    {
        [Test]
        public void RetrievesFromDisk()
        {
            var provider = new LocalDiskMapFileProvider();
            var mapFiles = provider.Get(QuantConnect.Market.USA);
            Assert.IsNotEmpty(mapFiles);
        }

        [Test]
        public void CachesValueAndReturnsSameReference()
        {
            var provider = new LocalDiskMapFileProvider();
            var mapFiles1 = provider.Get(QuantConnect.Market.USA);
            var mapFiles2 = provider.Get(QuantConnect.Market.USA);
            Assert.IsTrue(ReferenceEquals(mapFiles1, mapFiles2));
        }
    }
}
