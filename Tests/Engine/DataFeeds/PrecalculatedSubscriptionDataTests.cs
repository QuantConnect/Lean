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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class PrecalculatedSubscriptionDataTests
    {
        private SubscriptionDataConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
        }

        [Test]
        public void ChangeDataNormalizationMode()
        {
            var tb = new TradeBar
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Period = TimeSpan.FromHours(1),
                Symbol = Symbols.SPY,
                Open = 100,
                High = 200,
                Low = 300,
                Close = 400
            };

            var factor = 0.5m;
            var sumOfDividends = 100m;
            var adjustedTb = tb.Clone(tb.IsFillForward).Adjust(factor);

            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.Utc);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.Utc, new DateTime(2020, 5, 21), new DateTime(2020, 5, 22));

            var emitTimeUtc = offsetProvider.ConvertToUtc(tb.EndTime);
            _config.SumOfDividends = sumOfDividends;

            var subscriptionData = new PrecalculatedSubscriptionData(
                _config,
                tb,
                adjustedTb,
                DataNormalizationMode.Adjusted,
                emitTimeUtc);

            _config.DataNormalizationMode = DataNormalizationMode.Raw;
            Assert.AreEqual(tb.Open, (subscriptionData.Data as TradeBar).Open);
            Assert.AreEqual(tb.High, (subscriptionData.Data as TradeBar).High);
            Assert.AreEqual(tb.Low, (subscriptionData.Data as TradeBar).Low);
            Assert.AreEqual(tb.Close, (subscriptionData.Data as TradeBar).Close);

            _config.DataNormalizationMode = DataNormalizationMode.Adjusted;
            Assert.AreEqual(tb.Open * factor, (subscriptionData.Data as TradeBar).Open);
            Assert.AreEqual(tb.High * factor, (subscriptionData.Data as TradeBar).High);
            Assert.AreEqual(tb.Low * factor, (subscriptionData.Data as TradeBar).Low);
            Assert.AreEqual(tb.Close * factor, (subscriptionData.Data as TradeBar).Close);

            _config.DataNormalizationMode = DataNormalizationMode.TotalReturn;
            Assert.Throws<ArgumentException>(() =>
                {
                    var data = subscriptionData.Data;
                }
            );

            _config.DataNormalizationMode = DataNormalizationMode.SplitAdjusted;
            Assert.Throws<ArgumentException>(() =>
                {
                    var data = subscriptionData.Data;
                }
            );
        }
    }
}
