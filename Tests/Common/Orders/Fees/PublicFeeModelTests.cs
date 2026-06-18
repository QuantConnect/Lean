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

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class PublicFeeModelTests
    {
        private readonly PublicFeeModel _feeModel = new();

        [Test]
        public void GetOrderFeeEquityRegularHoursReturnsZero()
        {
            var security = CreateEquity();
            // 2024-01-03 15:00 UTC == 10:00 ET (regular session).
            var order = new MarketOrder(security.Symbol, 10m, new DateTime(2024, 1, 3, 15, 0, 0));

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0m));
        }

        [Test]
        public void GetOrderFeeEquityExtendedHoursReturnsFlatFee()
        {
            var security = CreateEquity();
            // 2024-01-03 12:00 UTC == 07:00 ET (pre-market, extended session).
            var order = new MarketOrder(security.Symbol, 10m, new DateTime(2024, 1, 3, 12, 0, 0));

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(2.99m));
        }

        [Test]
        public void GetOrderFeeEquityOptionReturnsZero()
        {
            var security = CreateOption(SecurityType.Option, 5m, "AAPL");
            var order = new MarketOrder(security.Symbol, 3m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0m));
        }

        [TestCase(1, 0.50)]
        [TestCase(3, 1.50)]
        public void GetOrderFeeIndexOptionReturnsPerContractFee(decimal quantity, decimal expectedFee)
        {
            var security = CreateOption(SecurityType.IndexOption, 12m, "SPX");
            var order = new MarketOrder(security.Symbol, quantity, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(expectedFee));
        }

        // Crypto fee is tiered by the order amount (price * quantity) in USD.
        [TestCase(5, 1, 0.49, Description = "amount <= $10")]
        [TestCase(10, 1, 0.49, Description = "amount == $10 boundary")]
        [TestCase(20, 1, 0.69, Description = "$10 < amount <= $25")]
        [TestCase(40, 1, 1.19, Description = "$25 < amount <= $50")]
        [TestCase(80, 1, 1.69, Description = "$50 < amount <= $100")]
        [TestCase(200, 1, 3.29, Description = "$100 < amount <= $250")]
        [TestCase(400, 1, 6.29, Description = "$250 < amount <= $500")]
        [TestCase(500, 1, 6.29, Description = "amount == $500 boundary")]
        [TestCase(600, 1, 7.50, Description = "amount > $500 -> 1.25%")]
        [TestCase(100, 8, 10.00, Description = "amount $800 > $500 -> 1.25%")]
        public void GetOrderFeeCryptoReturnsTieredFee(decimal price, decimal quantity, decimal expectedFee)
        {
            var security = CreateCrypto(price);
            var order = new MarketOrder(security.Symbol, quantity, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(expectedFee));
        }

        private static Security CreateEquity()
        {
            var symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, symbol, SecurityType.Equity);
            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute,
                exchangeHours.TimeZone, exchangeHours.TimeZone, true, true, false);

            return new Security(
                exchangeHours,
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
        }

        private static Security CreateCrypto(decimal price)
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase);
            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute,
                TimeZones.Utc, TimeZones.Utc, false, true, false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            security.SetMarketPrice(new Tick(DateTime.UtcNow, symbol, price, price));
            return security;
        }

        private static Security CreateOption(SecurityType securityType, decimal price, string ticker)
        {
            var isIndex = securityType == SecurityType.IndexOption;
            var underlying = Symbol.Create(ticker, isIndex ? SecurityType.Index : SecurityType.Equity, Market.USA);
            var symbol = Symbol.CreateOption(
                underlying, Market.USA,
                isIndex ? OptionStyle.European : OptionStyle.American,
                OptionRight.Call,
                isIndex ? 1000m : 150m,
                new DateTime(2026, 6, 20));

            var config = new SubscriptionDataConfig(
                typeof(TradeBar), symbol, Resolution.Minute,
                TimeZones.Utc, TimeZones.Utc, false, true, false);

            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            security.SetMarketPrice(new Tick(DateTime.UtcNow, symbol, price, price));
            return security;
        }
    }
}
