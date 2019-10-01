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
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.TimeInForces
{
    [TestFixture]
    public class TimeInForceTests
    {
        [Test]
        public void GtcTimeInForceOrderDoesNotExpire()
        {
            var security = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            var timeInForce = new GoodTilCanceledTimeInForce();
            var order = new LimitOrder(Symbols.SPY, 10, 100, DateTime.UtcNow);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, DateTime.UtcNow, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, DateTime.UtcNow, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void DayTimeInForceEquityOrderExpiresAtMarketClose()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.Day;
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.SPY, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
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
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    Symbols.EURUSD,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.Day;
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.EURUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
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
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    Symbols.EURUSD,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.Day;
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.EURUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
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
        public void DayTimeInForceCryptoOrderExpiresAtMidnightUtc()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0);

            var security = new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    Symbols.BTCUSD,
                    Resolution.Minute,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    true,
                    true,
                    true
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.Utc);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.Day;
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.BTCUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            localTimeKeeper.UpdateTime(utcTime.AddHours(14).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            localTimeKeeper.UpdateTime(utcTime.AddHours(14));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void GtdTimeInForceEquityOrderExpiresAtMarketCloseOnExpiryDate()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.GoodTilDate(new DateTime(2018, 5, 1));
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.SPY, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            // April 27th before market close
            localTimeKeeper.UpdateTime(utcTime.AddHours(6).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // April 27th at market close
            localTimeKeeper.UpdateTime(utcTime.AddHours(6));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 1st at 10 AM
            localTimeKeeper.UpdateTime(utcTime.AddDays(4));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 1st before market close
            localTimeKeeper.UpdateTime(utcTime.AddDays(4).AddHours(6).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 1st at market close
            localTimeKeeper.UpdateTime(utcTime.AddDays(4).AddHours(6));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void GtdTimeInForceForexOrderBeforeExpiresAt5PMOnExpiryDate()
        {
            // set time to 10:00:00 AM (NY time)
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Forex(
                SecurityExchangeHoursTests.CreateForexSecurityExchangeHours(),
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    Symbols.EURUSD,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.GoodTilDate(new DateTime(2018, 5, 1));
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.EURUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            // April 27th 4:59:59 PM (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddHours(7).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // April 27th 5:00:00 PM (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddHours(7));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 1st at 10 AM
            localTimeKeeper.UpdateTime(utcTime.AddDays(4));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 1st 4:59:59 PM (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddDays(4).AddHours(7).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 1st 5:00:00 PM (NY time)
            localTimeKeeper.UpdateTime(utcTime.AddDays(4).AddHours(7));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void GtdTimeInForceCryptoOrderExpiresAtMidnightUtcAfterExpiryDate()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0);

            var security = new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    Symbols.BTCUSD,
                    Resolution.Minute,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    true,
                    true,
                    true
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.Utc);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.GoodTilDate(new DateTime(2018, 5, 1));
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.BTCUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            // April 27th before midnight
            localTimeKeeper.UpdateTime(utcTime.AddHours(14).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // April 28th at midnight
            localTimeKeeper.UpdateTime(utcTime.AddHours(14));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 1st at 10 AM
            localTimeKeeper.UpdateTime(utcTime.AddDays(4));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 1st before midnight
            localTimeKeeper.UpdateTime(utcTime.AddDays(4).AddHours(14).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            // May 2nd at midnight
            localTimeKeeper.UpdateTime(utcTime.AddDays(4).AddHours(14));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void GtdSameDayTimeInForceEquityOrderExpiresAtMarketClose()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.GoodTilDate(new DateTime(2018, 4, 27));
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.SPY, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));

            localTimeKeeper.UpdateTime(utcTime.AddHours(6).AddSeconds(-1));
            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            localTimeKeeper.UpdateTime(utcTime.AddHours(6));
            Assert.IsTrue(timeInForce.IsOrderExpired(security, order));

            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill2));
        }

        [Test]
        public void GtdSameDayTimeInForceForexOrderBefore5PMExpiresAt5PM()
        {
            // set time to 10:00:00 AM (NY time)
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Forex(
                SecurityExchangeHoursTests.CreateForexSecurityExchangeHours(),
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    Symbols.EURUSD,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.GoodTilDate(new DateTime(2018, 4, 27));
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.EURUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
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
        public void GtdSameDayTimeInForceForexOrderAfter5PMExpiresAt5PMNextDay()
        {
            // set time to 6:00:00 PM (NY time)
            var utcTime = new DateTime(2018, 4, 27, 18, 0, 0).ConvertToUtc(TimeZones.NewYork);

            var security = new Forex(
                SecurityExchangeHoursTests.CreateForexSecurityExchangeHours(),
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    Symbols.EURUSD,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    true
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.NewYork);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.GoodTilDate(new DateTime(2018, 4, 27));
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.EURUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
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
        public void GtdSameDayTimeInForceCryptoOrderExpiresAtMidnightUtc()
        {
            var utcTime = new DateTime(2018, 4, 27, 10, 0, 0);

            var security = new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 1m),
                new SubscriptionDataConfig(
                    typeof(QuoteBar),
                    Symbols.BTCUSD,
                    Resolution.Minute,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    true,
                    true,
                    true
                ),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            var localTimeKeeper = new LocalTimeKeeper(utcTime, TimeZones.Utc);
            security.SetLocalTimeKeeper(localTimeKeeper);

            var timeInForce = TimeInForce.GoodTilDate(new DateTime(2018, 4, 27));
            var orderProperties = new OrderProperties { TimeInForce = timeInForce };
            var order = new LimitOrder(Symbols.BTCUSD, 10, 100, utcTime, "", orderProperties);

            Assert.IsFalse(timeInForce.IsOrderExpired(security, order));

            var fill1 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.PartiallyFilled, OrderDirection.Buy, order.LimitPrice, 3, OrderFee.Zero);
            Assert.IsTrue(timeInForce.IsFillValid(security, order, fill1));

            var fill2 = new OrderEvent(order.Id, order.Symbol, utcTime, OrderStatus.Filled, OrderDirection.Buy, order.LimitPrice, 7, OrderFee.Zero);
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
