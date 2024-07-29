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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class OrderTests
    {
        [Test, TestCaseSource(nameof(GetValueTestParameters))]
        public void GetValueTest(ValueTestParameters parameters)
        {
            // By default the price for option exercise orders is 0, so we need to set it to the strike price
            if (parameters.Order.Type == OrderType.OptionExercise)
            {
                parameters.Order.Price = parameters.Order.Symbol.ID.StrikePrice;
            }

            var value = parameters.Order.GetValue(parameters.Security);
            Assert.AreEqual(parameters.ExpectedValue, value);
        }

        [TestCase(OrderDirection.Sell, 300, 0.1, true, 270)]
        [TestCase(OrderDirection.Sell, 300, 30, false, 270)]
        [TestCase(OrderDirection.Buy, 300, 0.1, true, 330)]
        [TestCase(OrderDirection.Buy, 300, 30, false, 330)]
        public void TrailingStopOrder_CalculatesStopPrice(OrderDirection direction, decimal marketPrice, decimal trailingAmount,
            bool trailingAsPercentage, decimal expectedStopPrice)
        {
            var stopPrice = TrailingStopOrder.CalculateStopPrice(marketPrice, trailingAmount, trailingAsPercentage, direction);
            Assert.AreEqual(expectedStopPrice, stopPrice);
        }

        [TestCase(OrderDirection.Sell, 269, 300, 0.1, true, 270)]
        [TestCase(OrderDirection.Sell, 270, 300, 0.1, true, null)]
        [TestCase(OrderDirection.Sell, 269, 300, 30, false, 270)]
        [TestCase(OrderDirection.Sell, 270, 300, 30, false, null)]
        [TestCase(OrderDirection.Buy, 331, 300, 0.1, true, 330)]
        [TestCase(OrderDirection.Buy, 330, 300, 0.1, true, null)]
        [TestCase(OrderDirection.Buy, 331, 300, 30, false, 330)]
        [TestCase(OrderDirection.Buy, 330, 300, 30, false, null)]
        public void TrailingStopOrder_UpdatesStopPriceIfNecessary(OrderDirection direction, decimal currentStopPrice, decimal marketPrice,
            decimal trailingAmount, bool trailingAsPercentage, decimal? expectedStopPrice)
        {
            var updated = TrailingStopOrder.TryUpdateStopPrice(marketPrice, currentStopPrice, trailingAmount, trailingAsPercentage, direction,
                out var updatedStopPrice);

            if (expectedStopPrice.HasValue)
            {
                Assert.IsTrue(updated);
                Assert.AreEqual(expectedStopPrice.Value, updatedStopPrice);
            }
            else
            {
                Assert.IsFalse(updated);
            }
        }

        [TestCase(OrderDirection.Sell, 300, 0.1, true, 270)]
        [TestCase(OrderDirection.Sell, 300, 30, false, 270)]
        [TestCase(OrderDirection.Buy, 300, 0.1, true, 330)]
        [TestCase(OrderDirection.Buy, 300, 30, false, 330)]
        public void TrailingStopLimitorder_CalculatesStopPrice(OrderDirection direction, decimal marketPrice, decimal trailingAmount,
            bool trailingAsPercentage, decimal expectedStopPrice)
        {
            var stopPrice = TrailingStopLimitOrder.CalculateStopPrice(marketPrice, trailingAmount, trailingAsPercentage, direction);
            Assert.AreEqual(expectedStopPrice, stopPrice);
        }

        [TestCase(OrderDirection.Sell, 300, 10, 290)]
        [TestCase(OrderDirection.Buy, 300, 10, 310)]
        public void TrailingStopLimitOrder_CalculatesLimitPrice(OrderDirection direction, decimal stopPrice, decimal limitOffset,
            decimal expectedLimitPrice)
        {
            var limitPrice = TrailingStopLimitOrder.CalculateLimitPrice(stopPrice, limitOffset, direction);
            Assert.AreEqual(expectedLimitPrice, limitPrice);
        }

        [TestCase(OrderDirection.Sell, 269, 300, 0.1, true, 10, 270, 260)]
        [TestCase(OrderDirection.Sell, 270, 300, 0.1, true, 10, null, null)]
        [TestCase(OrderDirection.Sell, 269, 300, 30, false, 10, 270, 260)]
        [TestCase(OrderDirection.Sell, 270, 300, 30, false, 10, null, null)]
        [TestCase(OrderDirection.Buy, 331, 300, 0.1, true, 10, 330, 340)]
        [TestCase(OrderDirection.Buy, 330, 300, 0.1, true, 10, null, null)]
        [TestCase(OrderDirection.Buy, 331, 300, 30, false, 10, 330, 340)]
        [TestCase(OrderDirection.Buy, 330, 300, 30, false, 10, null, null)]
        public void TrailingStopLimitOrder_UpdatesStopAndLimitPricesIfNecessary(OrderDirection direction, decimal currentStopPrice, decimal marketPrice,
            decimal trailingAmount, bool trailingAsPercentage, decimal limitOffset, decimal? expectedStopPrice, decimal? expectedLimitPrice)
        {
            var updated = TrailingStopLimitOrder.TryUpdateStopAndLimitPrices(marketPrice, currentStopPrice, trailingAmount, trailingAsPercentage,
                limitOffset, direction, out var updatedStopPrice, out var updatedLimitPrice);

            if (expectedStopPrice.HasValue)
            {
                Assert.IsTrue(updated);
                Assert.AreEqual(expectedStopPrice, updatedStopPrice);
                Assert.AreEqual(expectedLimitPrice, updatedLimitPrice);
            }
            else
            {
                Assert.IsFalse(updated);
            }
        }

        private static TestCaseData[] GetValueTestParameters()
        {
            const decimal delta = 1m;
            const decimal price = 1.2345m;
            const int quantity = 100;
            const decimal pricePlusDelta = price + delta;
            const decimal priceMinusDelta = price - delta;
            var tz = TimeZones.NewYork;

            var time = new DateTime(2016, 2, 4, 16, 0, 0).ConvertToUtc(tz);

            var equity = new Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            equity.SetMarketPrice(new Tick {Value = price});

            var gbpCash = new Cash("GBP", 0, 1.46m);
            var properties = SymbolProperties.GetDefault(gbpCash.Symbol);
            var forex = new Forex(
                SecurityExchangeHours.AlwaysOpen(tz),
                gbpCash,
                new Cash("EUR", 0, 0),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURGBP, Resolution.Minute, tz, tz, true, false, false),
                properties,
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            forex.SetMarketPrice(new Tick {Value= price});

            var eurCash = new Cash("EUR", 0, 1.12m);
            properties = new SymbolProperties("Euro-Bund", eurCash.Symbol, 10, 0.1m, 1, string.Empty);
            var cfd = new Cfd(
                SecurityExchangeHours.AlwaysOpen(tz),
                eurCash,
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.DE10YBEUR, Resolution.Minute, tz, tz, true, false, false),
                properties,
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            cfd.SetMarketPrice(new Tick { Value = price });
            var multiplierTimesConversionRate = properties.ContractMultiplier*eurCash.ConversionRate;

            var option = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY_P_192_Feb19_2016,
                    Resolution.Minute,
                    tz,
                    tz,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            option.SetMarketPrice(new Tick { Value = price });

            return new List<ValueTestParameters>
            {
                // equity orders
                new ValueTestParameters("EquityLongMarketOrder", equity, new MarketOrder(Symbols.SPY, quantity, time), quantity*price),
                new ValueTestParameters("EquityShortMarketOrder", equity, new MarketOrder(Symbols.SPY, -quantity, time), -quantity*price),
                new ValueTestParameters("EquityLongLimitOrder", equity, new LimitOrder(Symbols.SPY, quantity, priceMinusDelta, time), quantity*priceMinusDelta),
                new ValueTestParameters("EquityShortLimit Order", equity, new LimitOrder(Symbols.SPY, -quantity, pricePlusDelta, time), -quantity*pricePlusDelta),
                new ValueTestParameters("EquityLongStopLimitOrder", equity, new StopLimitOrder(Symbols.SPY, quantity,.5m*priceMinusDelta, priceMinusDelta, time), quantity*priceMinusDelta),
                new ValueTestParameters("EquityShortStopLimitOrder", equity, new StopLimitOrder(Symbols.SPY, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, time), -quantity*pricePlusDelta),
                new ValueTestParameters("EquityLongStopMarketOrder", equity, new StopMarketOrder(Symbols.SPY, quantity, priceMinusDelta, time), quantity*priceMinusDelta),
                new ValueTestParameters("EquityLongStopMarketOrder", equity, new StopMarketOrder(Symbols.SPY, quantity, pricePlusDelta, time), quantity*price),
                new ValueTestParameters("EquityShortStopMarketOrder", equity, new StopMarketOrder(Symbols.SPY, -quantity, pricePlusDelta, time), -quantity*pricePlusDelta),
                new ValueTestParameters("EquityShortStopMarketOrder", equity, new StopMarketOrder(Symbols.SPY, -quantity, priceMinusDelta, time), -quantity*price),
                new ValueTestParameters("EquityLongTrailingStopOrderPriceMinusDelta", equity, new TrailingStopOrder(Symbols.SPY, quantity, priceMinusDelta, 0.1m, true, time), quantity*priceMinusDelta),
                new ValueTestParameters("EquityLongTrailingStopOrderPricePlusDelta", equity, new TrailingStopOrder(Symbols.SPY, quantity, pricePlusDelta, 0.1m, true, time), quantity*price),
                new ValueTestParameters("EquityShortTrailingStopOrderPricePlusDelta", equity, new TrailingStopOrder(Symbols.SPY, -quantity, pricePlusDelta, 0.1m, true, time), -quantity*pricePlusDelta),
                new ValueTestParameters("EquityShortTrailingStopOrderPriceMinusDelta", equity, new TrailingStopOrder(Symbols.SPY, -quantity, priceMinusDelta, 0.1m, true, time), -quantity*price),
                new ValueTestParameters("EquityLongLimitIfTouchedOrder", equity, new LimitIfTouchedOrder(Symbols.SPY, quantity, 1.5m*pricePlusDelta, priceMinusDelta, time), quantity*priceMinusDelta),
                new ValueTestParameters("EquityShortLimitIfTouchedOrder", equity, new LimitIfTouchedOrder(Symbols.SPY, -quantity, .5m*priceMinusDelta, pricePlusDelta, time), -quantity*pricePlusDelta),
                new ValueTestParameters("EquityLongTrailingStopLimitOrderPriceMinusDelta", equity, new TrailingStopLimitOrder(Symbols.SPY, quantity, .5m*priceMinusDelta, priceMinusDelta, 0.1m, true, 0.1m, time), quantity*priceMinusDelta),
                new ValueTestParameters("EquityLongTrailingStopLimitOrderPricePlusDelta", equity, new TrailingStopLimitOrder(Symbols.SPY, quantity, .5m*pricePlusDelta, pricePlusDelta, 0.1m, true, 0.1m, time), quantity*price),
                new ValueTestParameters("EquityShortTrailingStopLimitOrderPricePlusDelta", equity, new TrailingStopLimitOrder(Symbols.SPY, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, 0.1m, true, 0.1m, time), -quantity*pricePlusDelta),
                new ValueTestParameters("EquityShortTrailingStopLimitOrderPriceMinusDelta", equity, new TrailingStopLimitOrder(Symbols.SPY, -quantity, 1.5m*priceMinusDelta, priceMinusDelta, 0.1m, true, 0.1m, time), -quantity*price),

                // forex orders
                new ValueTestParameters("ForexLongMarketOrder", forex, new MarketOrder(Symbols.EURGBP, quantity, time), quantity*price*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortMarketOrder", forex, new MarketOrder(Symbols.EURGBP, -quantity, time), -quantity*price*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongLimitOrder", forex, new LimitOrder(Symbols.EURGBP, quantity, priceMinusDelta, time), quantity*priceMinusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortLimit Order", forex, new LimitOrder(Symbols.EURGBP, -quantity, pricePlusDelta, time), -quantity*pricePlusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongStopLimitOrder", forex, new StopLimitOrder(Symbols.EURGBP, quantity,.5m*priceMinusDelta, priceMinusDelta, time), quantity*priceMinusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortStopLimitOrder", forex, new StopLimitOrder(Symbols.EURGBP, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, time), -quantity*pricePlusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongStopMarketOrder", forex, new StopMarketOrder(Symbols.EURGBP, quantity, priceMinusDelta, time), quantity*priceMinusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongStopMarketOrder", forex, new StopMarketOrder(Symbols.EURGBP, quantity, pricePlusDelta, time), quantity*price*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortStopMarketOrder", forex, new StopMarketOrder(Symbols.EURGBP, -quantity, pricePlusDelta, time), -quantity*pricePlusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortStopMarketOrder", forex, new StopMarketOrder(Symbols.EURGBP, -quantity, priceMinusDelta, time), -quantity*price*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongTrailingStopOrderPriceMinusDelta", forex, new TrailingStopOrder(Symbols.EURGBP, quantity, priceMinusDelta, 0.1m, true, time), quantity*priceMinusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongTrailingStopOrderPricePlusDelta", forex, new TrailingStopOrder(Symbols.EURGBP, quantity, pricePlusDelta, 0.1m, true, time), quantity*price*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortTrailingStopOrderPricePlusDelta", forex, new TrailingStopOrder(Symbols.EURGBP, -quantity, pricePlusDelta, 0.1m, true, time), -quantity*pricePlusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortTrailingStopOrderPriceMinusDelta", forex, new TrailingStopOrder(Symbols.EURGBP, -quantity, priceMinusDelta, 0.1m, true, time), -quantity*price*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongLimitIfTouchedOrder", forex, new LimitIfTouchedOrder(Symbols.EURGBP, quantity,1.5m*priceMinusDelta, priceMinusDelta, time), quantity*priceMinusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortLimitIfTouchedOrder", forex, new LimitIfTouchedOrder(Symbols.EURGBP, -quantity, .5m*pricePlusDelta, pricePlusDelta, time), -quantity*pricePlusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongTrailingStopLimitOrderPriceMinusDelta", forex, new TrailingStopLimitOrder(Symbols.EURGBP, quantity, .5m*priceMinusDelta, priceMinusDelta, 0.1m, true, 0.1m, time), quantity*priceMinusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexLongTrailingStopLimitOrderPricePlusDelta", forex, new TrailingStopLimitOrder(Symbols.EURGBP, quantity, .5m*pricePlusDelta, pricePlusDelta, 0.1m, true, 0.1m, time), quantity*price*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortTrailingStopLimitOrderPricePlusDelta", forex, new TrailingStopLimitOrder(Symbols.EURGBP, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, 0.1m, true, 0.1m, time), -quantity*pricePlusDelta*forex.QuoteCurrency.ConversionRate),
                new ValueTestParameters("ForexShortTrailingStopLimitOrderPriceMinusDelta", forex, new TrailingStopLimitOrder(Symbols.EURGBP, -quantity, 1.5m*priceMinusDelta, priceMinusDelta, 0.1m, true, 0.1m, time), -quantity*price*forex.QuoteCurrency.ConversionRate),

                // cfd orders
                new ValueTestParameters("CfdLongMarketOrder", cfd, new MarketOrder(Symbols.DE10YBEUR, quantity, time), quantity*price*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortMarketOrder", cfd, new MarketOrder(Symbols.DE10YBEUR, -quantity, time), -quantity*price*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongLimitOrder", cfd, new LimitOrder(Symbols.DE10YBEUR, quantity, priceMinusDelta, time), quantity*priceMinusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortLimit Order", cfd, new LimitOrder(Symbols.DE10YBEUR, -quantity, pricePlusDelta, time), -quantity*pricePlusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongStopLimitOrder", cfd, new StopLimitOrder(Symbols.DE10YBEUR, quantity,.5m*priceMinusDelta, priceMinusDelta, time), quantity*priceMinusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortStopLimitOrder", cfd, new StopLimitOrder(Symbols.DE10YBEUR, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, time), -quantity*pricePlusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongStopMarketOrder", cfd, new StopMarketOrder(Symbols.DE10YBEUR, quantity, priceMinusDelta, time), quantity*priceMinusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongStopMarketOrder", cfd, new StopMarketOrder(Symbols.DE10YBEUR, quantity, pricePlusDelta, time), quantity*price*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortStopMarketOrder", cfd, new StopMarketOrder(Symbols.DE10YBEUR, -quantity, pricePlusDelta, time), -quantity*pricePlusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortStopMarketOrder", cfd, new StopMarketOrder(Symbols.DE10YBEUR, -quantity, priceMinusDelta, time), -quantity*price*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongTrailingStopOrderPriceMinusDelta", cfd, new TrailingStopOrder(Symbols.DE10YBEUR, quantity, priceMinusDelta, 0.1m, true, time), quantity*priceMinusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongTrailingStopOrderPricePlusDelta", cfd, new TrailingStopOrder(Symbols.DE10YBEUR, quantity, pricePlusDelta, 0.1m, true, time), quantity*price*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortTrailingStopOrderPricePlusDelta", cfd, new TrailingStopOrder(Symbols.DE10YBEUR, -quantity, pricePlusDelta, 0.1m, true, time), -quantity*pricePlusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortTrailingStopOrderPriceMinusDelta", cfd, new TrailingStopOrder(Symbols.DE10YBEUR, -quantity, priceMinusDelta, 0.1m, true, time), -quantity*price*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortLimitIfTouchedOrder", cfd, new LimitIfTouchedOrder(Symbols.DE10YBEUR, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, time), -quantity*pricePlusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongLimitIfTouchedOrder", cfd, new LimitIfTouchedOrder(Symbols.DE10YBEUR, quantity,.5m*priceMinusDelta, priceMinusDelta, time), quantity*priceMinusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongTrailingStopLimitOrderPriceMinusDelta", cfd, new TrailingStopLimitOrder(Symbols.SPY, quantity, .5m*priceMinusDelta, priceMinusDelta, 0.1m, true, 0.1m, time), quantity*priceMinusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdLongTrailingStopLimitOrderPricePlusDelta", cfd, new TrailingStopLimitOrder(Symbols.SPY, quantity, .5m*pricePlusDelta, pricePlusDelta, 0.1m, true, 0.1m, time), quantity*price*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortTrailingStopLimitOrderPricePlusDelta", cfd, new TrailingStopLimitOrder(Symbols.SPY, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, 0.1m, true, 0.1m, time), -quantity*pricePlusDelta*multiplierTimesConversionRate),
                new ValueTestParameters("CfdShortTrailingStopLimitOrderPriceMinusDelta", cfd, new TrailingStopLimitOrder(Symbols.SPY, -quantity, 1.5m*priceMinusDelta, priceMinusDelta, 0.1m, true, 0.1m, time), -quantity*price*multiplierTimesConversionRate),

                // equity/index option orders
                new ValueTestParameters("OptionLongMarketOrder", option, new MarketOrder(Symbols.SPY_P_192_Feb19_2016, quantity, time), quantity*price),
                new ValueTestParameters("OptionShortMarketOrder", option, new MarketOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, time), -quantity*price),
                new ValueTestParameters("OptionLongLimitOrder", option, new LimitOrder(Symbols.SPY_P_192_Feb19_2016, quantity, priceMinusDelta, time), quantity*priceMinusDelta),
                new ValueTestParameters("OptionShortLimit Order", option, new LimitOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, pricePlusDelta, time), -quantity*pricePlusDelta),
                new ValueTestParameters("OptionLongStopLimitOrder", option, new StopLimitOrder(Symbols.SPY_P_192_Feb19_2016, quantity,.5m*priceMinusDelta, priceMinusDelta, time), quantity*priceMinusDelta),
                new ValueTestParameters("OptionShortStopLimitOrder", option, new StopLimitOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, time),  -quantity*pricePlusDelta),
                new ValueTestParameters("OptionLongStopMarketOrder", option, new StopMarketOrder(Symbols.SPY_P_192_Feb19_2016, quantity, priceMinusDelta, time), quantity*priceMinusDelta),
                new ValueTestParameters("OptionLongStopMarketOrder", option, new StopMarketOrder(Symbols.SPY_P_192_Feb19_2016, quantity, pricePlusDelta, time), quantity*price),
                new ValueTestParameters("OptionShortStopMarketOrder", option, new StopMarketOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, pricePlusDelta, time), -quantity*pricePlusDelta),
                new ValueTestParameters("OptionShortStopMarketOrder", option, new StopMarketOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, priceMinusDelta, time), -quantity*price),
                new ValueTestParameters("OptionLongTrailingStopOrdePriceMinusDeltar", option, new TrailingStopOrder(Symbols.SPY_P_192_Feb19_2016, quantity, priceMinusDelta, 0.1m, true, time), quantity*priceMinusDelta),
                new ValueTestParameters("OptionLongTrailingStopOrderPricePlusDelta", option, new TrailingStopOrder(Symbols.SPY_P_192_Feb19_2016, quantity, pricePlusDelta, 0.1m, true, time), quantity*price),
                new ValueTestParameters("OptionShortTrailingStopOrderPricePlusDelta", option, new TrailingStopOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, pricePlusDelta, 0.1m, true, time), -quantity*pricePlusDelta),
                new ValueTestParameters("OptionShortTrailingStopOrderPriceMinusDelta", option, new TrailingStopOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, priceMinusDelta, 0.1m, true, time), -quantity*price),
                new ValueTestParameters("OptionShortLimitIfTouchedOrder", option, new LimitIfTouchedOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, time),  -quantity*pricePlusDelta),
                new ValueTestParameters("OptionLongLimitIfTouchedOrder", option, new LimitIfTouchedOrder(Symbols.SPY_P_192_Feb19_2016, quantity,.5m*priceMinusDelta, priceMinusDelta, time), quantity*priceMinusDelta),
                new ValueTestParameters("OptionLongTrailingStopLimitOrderPriceMinusDelta", option, new TrailingStopLimitOrder(Symbols.SPY_P_192_Feb19_2016, quantity, .5m*priceMinusDelta, priceMinusDelta, 0.1m, true, 0.1m, time), quantity*priceMinusDelta),
                new ValueTestParameters("OptionLongTrailingStopLimitOrderPricePlusDelta", option, new TrailingStopLimitOrder(Symbols.SPY_P_192_Feb19_2016, quantity, .5m*pricePlusDelta, pricePlusDelta, 0.1m, true, 0.1m, time), quantity*price),
                new ValueTestParameters("OptionShortTrailingStopLimitOrderPricePlusDelta", option, new TrailingStopLimitOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, 1.5m*pricePlusDelta, pricePlusDelta, 0.1m, true, 0.1m, time), -quantity*pricePlusDelta),
                new ValueTestParameters("OptionShortTrailingStopLimitOrderPriceMinusDelta", option, new TrailingStopLimitOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, 1.5m*priceMinusDelta, priceMinusDelta, 0.1m, true, 0.1m, time), -quantity*price),

                new ValueTestParameters("OptionExerciseOrderPut", option, new OptionExerciseOrder(Symbols.SPY_P_192_Feb19_2016, quantity, time), quantity*option.Symbol.ID.StrikePrice),
                new ValueTestParameters("OptionAssignmentOrderPut", option, new OptionExerciseOrder(Symbols.SPY_P_192_Feb19_2016, -quantity, time), -quantity*option.Symbol.ID.StrikePrice),
                new ValueTestParameters("OptionExerciseOrderCall", option, new OptionExerciseOrder(Symbols.SPY_C_192_Feb19_2016, quantity, time), quantity*option.Symbol.ID.StrikePrice),
                new ValueTestParameters("OptionAssignmentOrderCall", option, new OptionExerciseOrder(Symbols.SPY_C_192_Feb19_2016, -quantity, time), -quantity*option.Symbol.ID.StrikePrice),


            }.Select(x => new TestCaseData(x).SetName(x.Name)).ToArray();
        }

        public class ValueTestParameters
        {
            public string Name { get; init; }
            public Security Security { get; init; }
            public Order Order { get; init; }
            public decimal ExpectedValue { get; init; }

            public ValueTestParameters(string name, Security security, Order order, decimal expectedValue)
            {
                Name = name;
                Security = security;
                Order = order;
                ExpectedValue = expectedValue;
            }
        }
    }
}
