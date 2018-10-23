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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class UniverseSelectionTests
    {
        [Test]
        public void CreatedEquityIsNotAddedToSymbolCache()
        {
            SymbolCache.Clear();
            var algorithm = new AlgorithmStub(new TestDataFeed());
            algorithm.SetEndDate(Time.EndOfTime);
            algorithm.SetStartDate(DateTime.UtcNow.Subtract(TimeSpan.FromDays(10)));
            algorithm.AddUniverse(CoarseSelectionFunction, FineSelectionFunction);
            var universe = algorithm.UniverseManager.Values.First();
            var securityChanges = algorithm.DataManager.UniverseSelection.ApplyUniverseSelection(
                universe,
                algorithm.EndDate.ConvertToUtc(algorithm.TimeZone).Subtract(TimeSpan.FromDays(1)),
                new BaseDataCollection(
                    DateTime.UtcNow,
                    Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                    new List<BaseData>()
                )
            );
            Symbol symbol;
            Assert.AreEqual(1, securityChanges.AddedSecurities.Count);
            Assert.AreEqual(Symbol.Create("SPY", SecurityType.Equity, Market.USA),
                securityChanges.AddedSecurities.First().Symbol);
            Assert.IsFalse(SymbolCache.TryGetSymbol("GOOG", out symbol));
            Assert.IsFalse(SymbolCache.TryGetSymbol("SPY", out symbol));
        }

        private IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            return new List<Symbol>
            {
                Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
                Symbol.Create("SPY", SecurityType.Equity, Market.USA)
            };
        }

        private IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
        {
            return new[] { fine.First().Symbol };
        }

        private class TestDataFeed : IDataFeed
        {
            public IEnumerator<TimeSlice> GetEnumerator()
            {
                return null;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerable<Subscription> Subscriptions { get; }
            public bool IsActive { get; }

            public void Initialize(
                IAlgorithm algorithm,
                AlgorithmNodePacket job,
                IResultHandler resultHandler,
                IMapFileProvider mapFileProvider,
                IFactorFileProvider factorFileProvider,
                IDataProvider dataProvider,
                IDataFeedSubscriptionManager subscriptionManager
                )
            {
            }

            public bool AddSubscription(SubscriptionRequest request)
            {
                return true;
            }

            public bool RemoveSubscription(SubscriptionDataConfig configuration)
            {
                return true;
            }

            public void Exit()
            {
            }
        }
    }
}
