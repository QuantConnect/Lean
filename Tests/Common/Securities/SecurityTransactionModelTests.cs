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
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityTransactionModelTests
    {
        private static readonly DateTime Noon = new DateTime(2014, 6, 24, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });

        [Test]
        public void PerformsMarketFillBuy()
        {
            var model = new SecurityTransactionModel();
            var order = new MarketOrder(Symbols.SPY, 100, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            var fill = model.MarketFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }
        [Test]
        public void PerformsMarketFillSell()
        {
            var model = new SecurityTransactionModel();
            var order = new MarketOrder(Symbols.SPY, -100, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            var fill = model.MarketFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsLimitFillBuy()
        {
            var model = new SecurityTransactionModel();
            var order = new LimitOrder(Symbols.SPY, 100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.LimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Min(order.LimitPrice, security.High), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsLimitFillSell()
        {
            var model = new SecurityTransactionModel();
            var order = new LimitOrder(Symbols.SPY, -100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            var fill = model.LimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Max(order.LimitPrice, security.Low), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsStopLimitFillBuy()
        {
            var model = new SecurityTransactionModel();
            var order = new StopLimitOrder(Symbols.SPY, 100, 101.5m, 101.75m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 100m));

            var fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.66m));

            fill = model.StopLimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsStopLimitFillSell()
        {
            var model = new SecurityTransactionModel();
            var order = new StopLimitOrder(Symbols.SPY, -100, 101.75m, 101.50m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.66m));

            fill = model.StopLimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsStopMarketFillBuy()
        {
            var model = new SecurityTransactionModel();
            var order = new StopMarketOrder(Symbols.SPY, 100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            var fill = model.StopMarketFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102.5m));

            fill = model.StopMarketFill(security, order);

            // this fills worst case scenario, so it's min of asset/stop price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Max(security.Price, order.StopPrice), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsStopMarketFillSell()
        {
            var model = new SecurityTransactionModel();
            var order = new StopMarketOrder(Symbols.SPY, -100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.StopMarketFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            fill = model.StopMarketFill(security, order);

            // this fills worst case scenario, so it's min of asset/stop price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Min(security.Price, order.StopPrice), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsMarketOnOpenUsingOpenPrice()
        {
            var reference = new DateTime(2015, 06, 05, 9, 0, 0); // before market open
            var model = new SecurityTransactionModel();
            var order = new MarketOnOpenOrder(Symbols.SPY, 100, reference);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100));

            var fill = model.MarketOnOpenFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // market opens after 30min, so this is just before market open
            time = reference.AddMinutes(29);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1.33m, 2.75m, 1.15m, 1.45m, 100));

            fill = model.MarketOnOpenFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // market opens after 30min
            time = reference.AddMinutes(30);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1.45m, 2.0m, 1.1m, 1.40m, 100));

            fill = model.MarketOnOpenFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Open, fill.FillPrice);
        }

        [Test]
        public void PerformsMarketOnCloseUsingClosingPrice()
        {
            var reference = new DateTime(2015, 06, 05, 15, 0, 0); // before market close
            var model = new SecurityTransactionModel();
            var order = new MarketOnCloseOrder(Symbols.SPY, 100, reference);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100, config.Increment));

            var fill = model.MarketOnCloseFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes after 60min, so this is just before market Close
            time = reference.AddMinutes(59);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.33m, 2.75m, 1.15m, 1.45m, 100, config.Increment));

            fill = model.MarketOnCloseFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes
            time = reference.AddMinutes(60);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.45m, 2.0m, 1.1m, 1.40m, 100, config.Increment));

            fill = model.MarketOnCloseFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Close, fill.FillPrice);
        }

        private SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
