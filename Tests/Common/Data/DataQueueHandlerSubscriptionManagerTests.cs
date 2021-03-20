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
using QuantConnect.Data.Market;
using System;
using System.Linq;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DataQueueHandlerSubscriptionManagerTests
    {
        private DataQueueHandlerSubscriptionManager _subscriptionManager;

        [SetUp]
        public void SetUp()
        {
            _subscriptionManager = new FakeDataQueuehandlerSubscriptionManager((t) => "quote-trade");
        }

        [Test]
        public void SubscribeSingleSingleChannel()
        {
            _subscriptionManager.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbols.AAPL, Resolution.Minute));

            Assert.NotZero(_subscriptionManager.GetSubscribedSymbols().Count());
            Assert.Contains(Symbols.AAPL, _subscriptionManager.GetSubscribedSymbols().ToArray());
        }

        [Test]
        public void SubscribeManySingleChannel()
        {
            for (int i = 0; i < 10; i++)
            {
                _subscriptionManager.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbols.AAPL, Resolution.Minute));
                Assert.Contains(Symbols.AAPL, _subscriptionManager.GetSubscribedSymbols().ToList());
                Assert.IsTrue(_subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Quote));
                Assert.IsTrue(_subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Trade));
            }

            for (int i = 9; i >= 0; i--)
            {
                _subscriptionManager.Unsubscribe(GetSubscriptionDataConfig<QuoteBar>(Symbols.AAPL, Resolution.Minute));

                Assert.AreEqual(i > 0, _subscriptionManager.GetSubscribedSymbols().Count() == 1);
                Assert.AreEqual(i > 0, _subscriptionManager.GetSubscribedSymbols().Contains(Symbols.AAPL));
                Assert.AreEqual(i > 0, _subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Quote));
            }
        }


        [TestCase(typeof(TradeBar), TickType.Trade)]
        [TestCase(typeof(QuoteBar), TickType.Quote)]
        [TestCase(typeof(OpenInterest), TickType.OpenInterest)]
        public void SubscribeSinglePerChannel(Type type, TickType tickType)
        {
            var subscriptionManager = new FakeDataQueuehandlerSubscriptionManager((t) => t.ToString());

            subscriptionManager.Subscribe(GetSubscriptionDataConfig(type, Symbols.AAPL, Resolution.Minute));

            Assert.AreEqual(1, subscriptionManager.GetSubscribedSymbols().Count());
            Assert.Contains(Symbols.AAPL, subscriptionManager.GetSubscribedSymbols().ToArray());

            foreach (var value in Enum.GetValues(typeof(TickType)))
            {
                Assert.AreEqual(tickType == (TickType)value, subscriptionManager.IsSubscribed(Symbols.AAPL, (TickType)value));
            }
        }

        [Test]
        public void SubscribeManyPerChannel()
        {
            var subscriptionManager = new FakeDataQueuehandlerSubscriptionManager((t) => t.ToString());

            for (int i = 0; i < 5; i++)
            {
                subscriptionManager.Subscribe(GetSubscriptionDataConfig<TradeBar>(Symbols.AAPL, Resolution.Minute));
            }

            Assert.IsTrue(subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Trade));
            Assert.IsFalse(subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Quote));

            subscriptionManager.Subscribe(GetSubscriptionDataConfig<QuoteBar>(Symbols.AAPL, Resolution.Minute));

            Assert.IsTrue(subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Trade));
            Assert.IsTrue(subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Quote));

            for (int i = 0; i < 5; i++)
            {
                subscriptionManager.Unsubscribe(GetSubscriptionDataConfig<TradeBar>(Symbols.AAPL, Resolution.Minute));
            }

            Assert.IsFalse(subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Trade));
            Assert.IsTrue(subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Quote));

            subscriptionManager.Unsubscribe(GetSubscriptionDataConfig<QuoteBar>(Symbols.AAPL, Resolution.Minute));

            Assert.IsFalse(subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Trade));
            Assert.IsFalse(subscriptionManager.IsSubscribed(Symbols.AAPL, TickType.Quote));
        }

        #region helper

        private SubscriptionDataConfig GetSubscriptionDataConfig(Type T, Symbol symbol, Resolution resolution, TickType? tickType = null)
        {
            return new SubscriptionDataConfig(
                T,
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false,
                tickType: tickType);
        }

        protected SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }

        #endregion
    }
}
