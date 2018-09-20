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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmSubscriptionManagerRemoveConsolidatorTests
    {
        [Test]
        public void RemoveConsolidatorClearsEventHandlers()
        {
            bool eventHandlerFired = false;
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var security = algorithm.AddEquity("SPY");
            var consolidator = new IdentityDataConsolidator<BaseData>();
            consolidator.DataConsolidated += (sender, consolidated) => eventHandlerFired = true;
            security.Subscriptions.First().Consolidators.Add(consolidator);

            algorithm.SubscriptionManager.RemoveConsolidator(security.Symbol, consolidator);

            consolidator.Update(new Tick());
            Assert.IsFalse(eventHandlerFired);
        }
    }
}