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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.TimeInForces
{
    [TestFixture]
    public class TimeInForceHandlerTests
    {
        [Test]
        public void GtcTimeInForceOrderDoesNotExpire()
        {
            var handler = new GoodTilCanceledTimeInForceHandler();

            var security = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            var order = new LimitOrder(Symbols.SPY, 10, 100, DateTime.UtcNow);

            Assert.IsFalse(handler.HasOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, DateTime.UtcNow, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(handler.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, DateTime.UtcNow, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(handler.IsFillValid(security, order, fill2));
        }

        [Test]
        public void DayTimeInForceEquityOrderExpiresAtMarketClose()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);
            var handler = new DayTimeInForceHandler();

            var security = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var orderProperties = new OrderProperties { TimeInForce = TimeInForce.Day };
            var order = new LimitOrder(Symbols.SPY, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(handler.HasOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(handler.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(handler.IsFillValid(security, order, fill2));

            localTimeKeeper.UpdateTime(utcTime.AddHours(6).AddSeconds(-1));
            Assert.IsFalse(handler.HasOrderExpired(security, order));

            localTimeKeeper.UpdateTime(utcTime.AddHours(6));
            Assert.IsTrue(handler.HasOrderExpired(security, order));

            Assert.IsTrue(handler.IsFillValid(security, order, fill1));
            Assert.IsTrue(handler.IsFillValid(security, order, fill2));
        }

        [Test]
        public void DayTimeInForceForexOrderExpiresAt5PM()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);
            var handler = new DayTimeInForceHandler();

            var security = new Forex(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                new SubscriptionDataConfig(typeof(QuoteBar), Symbols.EURUSD, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var orderProperties = new OrderProperties { TimeInForce = TimeInForce.Day };
            var order = new LimitOrder(Symbols.EURUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(handler.HasOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(handler.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(handler.IsFillValid(security, order, fill2));

            localTimeKeeper.UpdateTime(utcTime.AddHours(7).AddSeconds(-1));
            Assert.IsFalse(handler.HasOrderExpired(security, order));

            localTimeKeeper.UpdateTime(utcTime.AddHours(7));
            Assert.IsTrue(handler.HasOrderExpired(security, order));

            Assert.IsTrue(handler.IsFillValid(security, order, fill1));
            Assert.IsTrue(handler.IsFillValid(security, order, fill2));
        }

        [Test]
        public void DayTimeInForceCryptoOrderExpiresAtMidnight()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0);
            var handler = new DayTimeInForceHandler();

            var security = new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                new SubscriptionDataConfig(typeof(QuoteBar), Symbols.BTCUSD, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, true, true, true),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.Utc);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var orderProperties = new OrderProperties { TimeInForce = TimeInForce.Day };
            var order = new LimitOrder(Symbols.BTCUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(handler.HasOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(handler.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(handler.IsFillValid(security, order, fill2));

            localTimeKeeper.UpdateTime(utcTime.AddHours(14).AddSeconds(-1));
            Assert.IsFalse(handler.HasOrderExpired(security, order));

            localTimeKeeper.UpdateTime(utcTime.AddHours(14));
            Assert.IsTrue(handler.HasOrderExpired(security, order));

            Assert.IsTrue(handler.IsFillValid(security, order, fill1));
            Assert.IsTrue(handler.IsFillValid(security, order, fill2));
        }
    }
}
