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
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class AlphaStreamsFeeModelTests
    {
        [Test]
        public void CalculateEquityMinimumFeeInUSD()
        {
            var feeModel = new AlphaStreamsFeeModel();
            var security = SecurityTests.GetSecurity();
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(1m, fee.Value.Amount);
        }

        [Test]
        public void CalculateEquityFeeInUSD()
        {
            var feeModel = new AlphaStreamsFeeModel();
            var security = SecurityTests.GetSecurity();
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(5m, fee.Value.Amount);
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void CalculateOrderFeeForLongOrShortFutures(int quantity)
        {
            var tz = TimeZones.NewYork;
            var security = new Future(Symbols.Fut_SPY_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var feeModel = new AlphaStreamsFeeModel();

            var parameters = new OrderFeeParameters(
                security,
                new MarketOrder(security.Symbol, quantity, DateTime.UtcNow)
            );

            var fee = feeModel.GetOrderFee(parameters);

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(Math.Abs(quantity) * 0.50m, fee.Value.Amount);
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void CalculateOrderFeeForLongOrShortOptions(int quantity)
        {
            var tz = TimeZones.NewYork;
            var security = new Option(Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var feeModel = new AlphaStreamsFeeModel();

            var parameters = new OrderFeeParameters(
                security,
                new MarketOrder(security.Symbol, quantity, DateTime.UtcNow)
            );

            var fee = feeModel.GetOrderFee(parameters);

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(Math.Abs(quantity) * 0.50m, fee.Value.Amount);
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void GetMinimumOrderFeeForLongOrShortOptions(int quantity)
        {
            var tz = TimeZones.NewYork;
            var security = new Option(Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var feeModel = new AlphaStreamsFeeModel();

            var parameters = new OrderFeeParameters(
                security,
                new MarketOrder(security.Symbol, quantity, DateTime.UtcNow)
            );

            var fee = feeModel.GetOrderFee(parameters);

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(Math.Abs(quantity) * 0.50m, fee.Value.Amount);
        }

        [TestCase(-1000)]
        [TestCase(1000)]
        public void CalculateOrderFeeForLongOrShortForex(int quantity)
        {
            var tz = TimeZones.NewYork;
            var security = new Forex(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 1),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURUSD, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("EURUSD", "USD", 1, 0.01m, 0.00000001m),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var feeModel = new AlphaStreamsFeeModel();

            var parameters = new OrderFeeParameters(
                security,
                new MarketOrder(security.Symbol, quantity, DateTime.UtcNow)
            );

            var fee = feeModel.GetOrderFee(parameters);

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0.000002m * security.Price * Math.Abs(quantity), fee.Value.Amount);
        }

        [TestCase(-1000000)]
        [TestCase(1000000)]
        public void CalculateOrderFeeForLongOrShortForexNonUsd(int quantity)
        {
            var conversionRate = 1.2m;
            var tz = TimeZones.NewYork;
            var security = new Forex(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("GBP", 0, conversionRate),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURGBP, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("EURGBP", "GBP", 1, 0.01m, 0.00000001m),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var feeModel = new AlphaStreamsFeeModel();

            var fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, quantity, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0.000002m * security.Price * Math.Abs(quantity) * conversionRate, fee.Value.Amount);
        }

        [Test]
        public void CalculateReturnsFeeInQuotecurrencyInAccountCurrency()
        {
            Crypto btcusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new Cash(Currencies.USD, 0, 1),
                    new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCUSD, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false),
                    new SymbolProperties("BTCUSD", Currencies.USD, 1, 0.01m, 0.00000001m),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                );
            btcusd.SetMarketPrice(new Tick(DateTime.UtcNow, btcusd.Symbol, 100, 100));

            var feeModel = new AlphaStreamsFeeModel();

            var fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    btcusd,
                    new MarketOrder(btcusd.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0.2m, fee.Value.Amount);
        }

        [Test]
        public void ReturnsFeeInQuoteCurrencyInOtherCurrency()
        {
            Crypto btceur = new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("EUR", 0, 10),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCEUR, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, false, false),
                new SymbolProperties("BTCEUR", "EUR", 1, 0.01m, 0.00000001m),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            btceur.SetMarketPrice(new Tick(DateTime.UtcNow, btceur.Symbol, 100, 100));

            var feeModel = new AlphaStreamsFeeModel();

            var fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    btceur,
                    new MarketOrder(btceur.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual("EUR", fee.Value.Currency);
            Assert.AreEqual(0.2m, fee.Value.Amount);
        }

        [Test]
        public void CalculateOrderFeeForCfd()
        {
            var tz = TimeZones.NewYork;
            var security = new Cfd(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("EUR", 0, 0),
                new SubscriptionDataConfig(typeof(QuoteBar), Symbols.DE30EUR, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("DE30EUR", "EUR", 1, 0.01m, 1m),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 12000, 12000));

            var feeModel = new AlphaStreamsFeeModel();

            var fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(0, fee.Value.Amount);
        }
    }
}