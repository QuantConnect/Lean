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

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class TimeInForceTests
    {
        [Test]
        public void GoodTilCanceledTimeInForceCompareProperly()
        {
            var tif1 = new GoodTilCanceledTimeInForce();
            var tif2 = new GoodTilCanceledTimeInForce();

            Assert.IsTrue(tif1 == tif2);
            Assert.IsFalse(tif1 != tif2);

            Assert.IsTrue(tif1 == TimeInForce.GoodTilCanceled);
            Assert.IsFalse(tif1 != TimeInForce.GoodTilCanceled);

            Assert.AreEqual(tif1, tif2);
            Assert.AreEqual(tif1.Type, tif2.Type);

            Assert.AreEqual(tif1, TimeInForceType.GoodTilCanceled);
        }

        [Test]
        public void DayTimeInForceCompareProperly()
        {
            var tif1 = new DayTimeInForce();
            var tif2 = new DayTimeInForce();

            Assert.IsTrue(tif1 == tif2);
            Assert.IsFalse(tif1 != tif2);

            Assert.IsTrue(tif1 == TimeInForce.Day);
            Assert.IsFalse(tif1 != TimeInForce.Day);

            Assert.AreEqual(tif1, tif2);
            Assert.AreEqual(tif1.Type, tif2.Type);

            Assert.AreEqual(tif1, TimeInForceType.Day);
        }

        [Test]
        public void GtdTimeInForceCompareProperly()
        {
            var tif1 = new GoodTilDateTimeInForce(new DateTime(2018, 5, 21));
            var tif2 = new GoodTilDateTimeInForce(new DateTime(2018, 5, 21));
            var tif3 = new GoodTilDateTimeInForce(new DateTime(2018, 6, 6));

            Assert.IsTrue(tif1 == tif2);
            Assert.IsFalse(tif1 != tif2);

            Assert.IsTrue(tif1 != tif3);
            Assert.IsFalse(tif1 == tif3);

            Assert.AreEqual(tif1, tif2);
            Assert.AreEqual(tif1.Type, tif2.Type);

            Assert.AreNotEqual(tif1, tif3);
            Assert.AreEqual(tif1.Type, tif3.Type);

            Assert.AreEqual(tif1, TimeInForceType.GoodTilDate);
        }

        [Test]
        public void GtcTimeInForceOrderDoesNotExpire()
        {
            var security = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));

            var timeInForce = new GoodTilCanceledTimeInForce();
            var order = new LimitOrder(Symbols.SPY, 10, 100, DateTime.UtcNow);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, DateTime.UtcNow, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, DateTime.UtcNow, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void DayTimeInForceEquityOrderExpiresAtMarketClose()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.Day;
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.SPY, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            localTimeKeeper.UpdateTime(utcTime.AddHours(6).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            localTimeKeeper.UpdateTime(utcTime.AddHours(6));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void DayTimeInForceForexOrderBefore5PMExpiresAt5PM()
        {
            // set time to 10:00:00 AM (NY time)
            var utcTime = new DateTime(2018, 4, 25, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Forex(
                SecurityExchangeHoursTests.CreateForexSecurityExchangeHours(),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                new SubscriptionDataConfig(typeof(QuoteBar), Symbols.EURUSD, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.Day;
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.EURUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            // set time to 4:59:59 PM (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddHours(7).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // set time to 5:00:00 PM (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddHours(7));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void DayTimeInForceForexOrderAfter5PMExpiresAt5PMNextDay()
        {
            // set time to 6:00:00 PM (NY time)
            var utcTime = new DateTime(2018, 4, 25, 18, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Forex(
                SecurityExchangeHoursTests.CreateForexSecurityExchangeHours(),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                new SubscriptionDataConfig(typeof(QuoteBar), Symbols.EURUSD, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.Day;
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.EURUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            // set time to midnight (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddHours(6));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // set time to 4:59:59 PM next day (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddHours(23).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // set time to 5:00:00 PM next day (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddHours(23));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void DayTimeInForceCryptoOrderExpiresAtMidnight()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0);

            var security = new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                new SubscriptionDataConfig(typeof(QuoteBar), Symbols.BTCUSD, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, true, true, true),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.Utc);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.Day;
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.BTCUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, 0);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            localTimeKeeper.UpdateTime(utcTime.AddHours(14).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            localTimeKeeper.UpdateTime(utcTime.AddHours(14));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }
    }
}
