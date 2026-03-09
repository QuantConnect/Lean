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
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SubscriptionDataSourceTests
    {
        [Test]
        public void ComparesEqualWithIdenticalSourceAndTransportMedium()
        {
            var one = new SubscriptionDataSource("source", SubscriptionTransportMedium.LocalFile);
            var two = new SubscriptionDataSource("source", SubscriptionTransportMedium.LocalFile);
            Assert.IsTrue(one == two);
            Assert.IsTrue(one.Equals(two));
        }

        [Test]
        public void ComparesNotEqualWithDifferentSource()
        {
            var one = new SubscriptionDataSource("source1", SubscriptionTransportMedium.LocalFile);
            var two = new SubscriptionDataSource("source2", SubscriptionTransportMedium.LocalFile);
            Assert.IsTrue(one != two);
            Assert.IsTrue(!one.Equals(two));
        }

        [Test]
        public void ComparesNotEqualWithDifferentTransportMedium()
        {
            var one = new SubscriptionDataSource("source", SubscriptionTransportMedium.LocalFile);
            var two = new SubscriptionDataSource("source", SubscriptionTransportMedium.RemoteFile);
            Assert.IsTrue(one != two);
            Assert.IsTrue(!one.Equals(two));
        }
    }
}
