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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public class FutureFillModelTests
    {
        private static readonly DateTime Noon = new DateTime(2014, 6, 24, 12, 0, 0);

        private static TimeKeeper TimeKeeper;

        [SetUp]
        public void Setup()
        {
            TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
        }

        [Test]
        public void PerformsMarketFill([Values] bool isInternal,
            [Values] bool extendedMarketHours,
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection)
        {
            var model = new FutureFillModel();
            var quantity = orderDirection == OrderDirection.Buy ? 100 : -100;
            var time = extendedMarketHours ? Noon.AddHours(-12) : Noon; // Midgnight (extended hours) or Noon (regular hours)
            var order = new MarketOrder(Symbols.ES_Future_Chain, quantity, time);
            var config = CreateTradeBarConfig(Symbols.ES_Future_Chain, isInternal, extendedMarketHours);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.ES_Future_Chain, time, 101.123m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsStopMarketFill(
            [Values] bool extendedMarketHours,
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection)
        {
            var symbol = Symbols.ES_Future_Chain;
            var model = new FutureFillModel();
            var quantity = orderDirection == OrderDirection.Buy ? 100 : -100;
            var marketPrice = orderDirection == OrderDirection.Buy ? 102m : 101m;

            var time = Noon.AddHours(-12);

            var order = new StopMarketOrder(symbol, quantity, 101.124m, time);
            var config = CreateTradeBarConfig(symbol, extendedMarketHours: extendedMarketHours);
            var security = GetSecurity(config);
            TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork).UpdateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(symbol, time, marketPrice));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(config);
            if (extendedMarketHours)
            {
                Assert.AreEqual(order.Quantity, fill.FillQuantity);
                Assert.AreEqual(security.Price, fill.FillPrice);
                Assert.AreEqual(OrderStatus.Filled, fill.Status);
                Assert.IsTrue(exchangeHours.IsOpen(fill.UtcTime, extendedMarketHours));
            }
            else
            {
                Assert.AreEqual(0m, fill.FillQuantity);
                Assert.AreEqual(0m, fill.FillPrice);
                Assert.AreNotEqual(OrderStatus.Filled, fill.Status);
                Assert.AreNotEqual(OrderStatus.PartiallyFilled, fill.Status);
                Assert.IsFalse(exchangeHours.IsOpen(fill.UtcTime, extendedMarketHours));
            }

        }

        private SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol, bool isInternal = false, bool extendedMarketHours = true)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, extendedMarketHours, isInternal);
        }

        private Security GetSecurity(SubscriptionDataConfig config)
        {
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(config.Symbol.ID.Market, config.Symbol, config.SecurityType);
            var security = new Security(
                entry.ExchangeHours,
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            return security;
        }
    }
}
