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
    public class SubscriptionDataTests
    {
        [Test]
        public void CreatedSubscriptionRoundsTimeDownForDataWithPeriod()
        {
            var tb = new TradeBar
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Period = TimeSpan.FromHours(1),
                Symbol = Symbols.SPY
            };

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.Utc);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.Utc, new DateTime(2020, 5, 21), new DateTime(2020, 5, 22));

            var subscription = SubscriptionData.Create(false, config, exchangeHours, offsetProvider, tb, config.DataNormalizationMode);

            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 0, 0), subscription.Data.Time);
            Assert.AreEqual(new DateTime(2020, 5, 21, 9, 0, 0), subscription.Data.EndTime);
        }

        [Test]
        public void CreatedSubscriptionDoesNotRoundDownForPeriodLessData()
        {
            var data = new MyCustomData
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Symbol = Symbols.SPY
            };

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.Utc);
            var offsetProvider = new TimeZoneOffsetProvider(TimeZones.Utc, new DateTime(2020, 5, 21), new DateTime(2020, 5, 22));

            var subscription = SubscriptionData.Create(false, config, exchangeHours, offsetProvider, data, config.DataNormalizationMode);

            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 9, 0), subscription.Data.Time);
            Assert.AreEqual(new DateTime(2020, 5, 21, 8, 9, 0), subscription.Data.EndTime);
        }

        [TestCase(1, 0)]
        [TestCase(null, 0)]
        [TestCase(null, 1000)]
        public void CreateDefaults(decimal? scale, decimal dividends)
        {
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            config.SumOfDividends = dividends;

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

            var data = SubscriptionData.Create(false,
                config,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new TimeZoneOffsetProvider(TimeZones.NewYork, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1)),
                tb,
                config.DataNormalizationMode,
                scale);

            Assert.True(data.GetType() == typeof(SubscriptionData));

            Assert.AreEqual(tb.Open, (data.Data as TradeBar).Open);
            Assert.AreEqual(tb.High, (data.Data as TradeBar).High);
            Assert.AreEqual(tb.Low, (data.Data as TradeBar).Low);
            Assert.AreEqual(tb.Close, (data.Data as TradeBar).Close);
        }

        [TestCase(typeof(SubscriptionData), 1)]
        [TestCase(typeof(PrecalculatedSubscriptionData), 2)]
        [TestCase(typeof(PrecalculatedSubscriptionData), 0.5)]
        public void CreateZeroDividends(Type type, decimal? scale)
        {
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            config.SumOfDividends = 0;

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

            var data = SubscriptionData.Create(false,
                config,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new TimeZoneOffsetProvider(TimeZones.NewYork, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1)),
                tb,
                config.DataNormalizationMode,
                scale);

            Assert.True(data.GetType() == type);

            Assert.AreEqual(tb.Open * scale, (data.Data as TradeBar).Open);
            Assert.AreEqual(tb.High * scale, (data.Data as TradeBar).High);
            Assert.AreEqual(tb.Low * scale, (data.Data as TradeBar).Low);
            Assert.AreEqual(tb.Close * scale, (data.Data as TradeBar).Close);
        }

        [TestCase(typeof(PrecalculatedSubscriptionData), 1)]
        [TestCase(typeof(PrecalculatedSubscriptionData), 2)]
        [TestCase(typeof(PrecalculatedSubscriptionData), 0.5)]
        public void CreateAdjustedNotZeroDividends(Type type, decimal? scale)
        {
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            config.SumOfDividends = 100;

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

            var data = SubscriptionData.Create(false,
                config,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new TimeZoneOffsetProvider(TimeZones.NewYork, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1)),
                tb,
                config.DataNormalizationMode,
                scale);

            Assert.True(data.GetType() == type);

            Assert.AreEqual(tb.Open * scale, (data.Data as TradeBar).Open);
            Assert.AreEqual(tb.High * scale, (data.Data as TradeBar).High);
            Assert.AreEqual(tb.Low * scale, (data.Data as TradeBar).Low);
            Assert.AreEqual(tb.Close * scale, (data.Data as TradeBar).Close);
        }

        [TestCase(typeof(PrecalculatedSubscriptionData), 1)]
        [TestCase(typeof(PrecalculatedSubscriptionData), 2)]
        [TestCase(typeof(PrecalculatedSubscriptionData), 0.5)]
        public void CreateTotalNotZeroDividends(Type type, decimal? scale)
        {
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            config.SumOfDividends = 100;
            config.DataNormalizationMode = DataNormalizationMode.TotalReturn;

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

            var data = SubscriptionData.Create(false,
                config,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new TimeZoneOffsetProvider(TimeZones.NewYork, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1)),
                tb,
                config.DataNormalizationMode,
                scale);

            Assert.True(data.GetType() == type);

            Assert.AreEqual(tb.Open * scale + config.SumOfDividends, (data.Data as TradeBar).Open);
            Assert.AreEqual(tb.High * scale + config.SumOfDividends, (data.Data as TradeBar).High);
            Assert.AreEqual(tb.Low * scale + config.SumOfDividends, (data.Data as TradeBar).Low);
            Assert.AreEqual(tb.Close * scale + config.SumOfDividends, (data.Data as TradeBar).Close);
        }

        [TestCase(true, typeof(TradeBar))]
        [TestCase(false, typeof(TradeBar))]
        [TestCase(true, typeof(QuoteBar))]
        [TestCase(false, typeof(QuoteBar))]
        [TestCase(true, typeof(Tick))]
        [TestCase(false, typeof(Tick))]
        public void FillForwardFlagIsCorrectlySet(bool isFillForward, Type type)
        {
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                Symbols.SPY,
                Resolution.Hour,
                TimeZones.Utc,
                TimeZones.Utc,
                false,
                false,
                false
            );

            var scale = 0.5m;
            config.DataNormalizationMode = DataNormalizationMode.Adjusted;

            var data = (BaseData)Activator.CreateInstance(type);
            if (isFillForward)
            {
                data = data.Clone(isFillForward);
            }

            var subscriptionData = (PrecalculatedSubscriptionData) SubscriptionData.Create(false, config,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new TimeZoneOffsetProvider(TimeZones.NewYork, new DateTime(2015, 1, 1), new DateTime(2016, 1, 1)),
                data,
                config.DataNormalizationMode,
                scale);

            config.DataNormalizationMode = DataNormalizationMode.Raw;
            Assert.AreEqual(isFillForward, subscriptionData.Data.IsFillForward);

            config.DataNormalizationMode = DataNormalizationMode.Adjusted;
            Assert.AreEqual(isFillForward, subscriptionData.Data.IsFillForward);
        }

        internal class MyCustomData : BaseData
        {
        }
    }
}
