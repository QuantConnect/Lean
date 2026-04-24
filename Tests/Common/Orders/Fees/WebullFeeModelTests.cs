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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class WebullFeeModelTests
    {
        private readonly WebullFeeModel _feeModel = new WebullFeeModel();

        private static IEnumerable<Security> ZeroFeeSecurities()
        {
            var equity = SecurityTests.GetSecurity();
            equity.SetMarketPrice(new Tick(DateTime.UtcNow, equity.Symbol, 100m, 100m));
            yield return equity;
            yield return CreateSecurity(SecurityType.Option, 5m, "AAPL");
        }

        /// <summary>
        /// Equity and non-index options are commission-free on Webull.
        /// </summary>
        [TestCaseSource(nameof(ZeroFeeSecurities))]
        public void GetOrderFeeReturnsZeroForFreeAssets(Security security)
        {
            var order = new MarketOrder(security.Symbol, 10m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0m));
        }

        /// <summary>
        /// SPX/SPXW exchange fee tiers (per contract) + Webull $0.50/contract:
        ///   SPX  price < $1 -> $0.57 + $0.50 = $1.07
        ///   SPX  price >= $1 -> $0.66 + $0.50 = $1.16
        ///   SPXW price < $1 -> $0.50 + $0.50 = $1.00
        ///   SPXW price >= $1 -> $0.59 + $0.50 = $1.09
        /// </summary>
        [TestCase("SPX", 0.50, 2, 2.14, Description = "SPX price < $1 -> $1.07/contract")]
        [TestCase("SPX", 1.50, 3, 3.48, Description = "SPX price >= $1 -> $1.16/contract")]
        [TestCase("SPXW", 0.80, 1, 1.00, Description = "SPXW price < $1 -> $1.00/contract")]
        [TestCase("SPXW", 2.00, 4, 4.36, Description = "SPXW price >= $1 -> $1.09/contract")]
        public void GetOrderFeeSpxPriceTierReturnsCorrectFee(string ticker, decimal price, decimal quantity, decimal expectedFee)
        {
            var security = CreateSecurity(SecurityType.IndexOption, price, ticker);
            var order = new MarketOrder(security.Symbol, quantity, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(expectedFee));
        }

        /// <summary>
        /// VIX exchange fee tiers (per contract) + Webull $0.50/contract:
        ///   Tier 1: price ≤ $0.10 -> $0.10 + $0.50 = $0.60
        ///   Tier 2: price $0.11–$0.99 -> $0.25 + $0.50 = $0.75
        ///   Tier 3: price $1.00–$1.99 -> $0.40 + $0.50 = $0.90
        ///   Tier 4: price >= $2.00 -> $0.45 + $0.50 = $0.95
        /// </summary>
        [TestCase(0.05, 4, 2.40, Description = "Tier 1: price ≤ $0.10 -> $0.60/contract")]
        [TestCase(0.50, 2, 1.50, Description = "Tier 2: price $0.11–$0.99 -> $0.75/contract")]
        [TestCase(1.50, 1, 0.90, Description = "Tier 3: price $1.00–$1.99 -> $0.90/contract")]
        [TestCase(3.00, 5, 4.75, Description = "Tier 4: price >= $2.00 -> $0.95/contract")]
        public void GetOrderFeeVixPriceTierReturnsCorrectFee(decimal price, decimal quantity, decimal expectedFee)
        {
            var security = CreateSecurity(SecurityType.IndexOption, price, "VIX");
            var order = new MarketOrder(security.Symbol, quantity, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(expectedFee));
        }

        /// <summary>
        /// VIXW uses identical tier schedule to VIX; verify tier 2.
        /// </summary>
        [Test]
        public void GetOrderFeeVixwPriceTier2MatchesVixFee()
        {
            var vix = CreateSecurity(SecurityType.IndexOption, 0.50m, "VIX");
            var vixw = CreateSecurity(SecurityType.IndexOption, 0.50m, "VIXW");
            var order = new MarketOrder(vix.Symbol, 2m, DateTime.UtcNow);

            var vixFee = _feeModel.GetOrderFee(new OrderFeeParameters(vix, order));
            var vixwFee = _feeModel.GetOrderFee(new OrderFeeParameters(vixw, new MarketOrder(vixw.Symbol, 2m, DateTime.UtcNow)));

            Assert.That(vixFee.Value.Amount, Is.EqualTo(vixwFee.Value.Amount));
        }

        /// <summary>
        /// Index option fee schedule (per contract) + Webull $0.50/contract:
        ///   XSP  qty < 10  -> $0.00 + $0.50 = $0.50
        ///   XSP  qty >= 10  -> $0.07 + $0.50 = $0.57
        ///   DJX  flat      -> $0.18 + $0.50 = $0.68
        ///   NDX  price < $25 -> $0.50 + $0.50 = $1.00  (NDXP shares the same schedule)
        ///   NDX  price >= $25 -> $0.75 + $0.50 = $1.25  (NDXP shares the same schedule)
        /// </summary>
        [TestCase("XSP", 1.00, 5, 2.50, Description = "XSP qty < 10 -> $0.50/contract")]
        [TestCase("XSP", 1.00, 10, 5.70, Description = "XSP qty >= 10 -> $0.57/contract")]
        [TestCase("DJX", 2.00, 2, 1.36, Description = "DJX flat -> $0.68/contract")]
        [TestCase("NDX", 10.00, 3, 3.00, Description = "NDX price < $25 -> $1.00/contract")]
        [TestCase("NDX", 50.00, 2, 2.50, Description = "NDX price >= $25 -> $1.25/contract")]
        [TestCase("NDXP", 10.00, 3, 3.00, Description = "NDXP price < $25 matches NDX schedule")]
        [TestCase("NDXP", 50.00, 2, 2.50, Description = "NDXP price >= $25 matches NDX schedule")]
        public void GetOrderFeeIndexOptionReturnsCorrectFee(string ticker, decimal price, decimal quantity, decimal expectedFee)
        {
            var security = CreateSecurity(SecurityType.IndexOption, price, ticker);
            var order = new MarketOrder(security.Symbol, quantity, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(expectedFee));
        }

        /// <summary>
        /// Crypto fee = 0.6% of notional (quantity × price).
        /// 2 BTC × $50,000 = $100,000 notional -> fee = $600.
        /// </summary>
        [Test]
        public void GetOrderFeeCryptoReturnsPointSixPercentOfNotional()
        {
            var btcusd = CreateSecurity(SecurityType.Crypto, 50_000m);
            var order = new MarketOrder(btcusd.Symbol, 2m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(btcusd, order));

            // 2 * 50000 * 0.006 = 600
            Assert.That(fee.Value.Amount, Is.EqualTo(600m));
            Assert.That(fee.Value.Currency, Is.EqualTo(Currencies.USD));
        }

        /// <summary>
        /// Creates a test security of the requested <paramref name="securityType"/> priced at <paramref name="price"/>.
        /// Supported types: <see cref="SecurityType.IndexOption"/>, <see cref="SecurityType.Option"/>, <see cref="SecurityType.Crypto"/>.
        /// For option types <paramref name="ticker"/> identifies the underlying symbol;
        /// it is ignored for <see cref="SecurityType.Crypto"/> (BTC/USD is always used).
        /// </summary>
        private static Security CreateSecurity(SecurityType securityType, decimal price, string ticker = "AAPL")
        {
            if (securityType == SecurityType.Crypto)
            {
                var btcusd = new Crypto(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    new Cash(Currencies.USD, 0, 1m),
                    new Cash("BTC", 0, price),
                    new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCUSD, Resolution.Minute,
                        TimeZones.Utc, TimeZones.Utc, true, false, false),
                    new SymbolProperties("BTCUSD", Currencies.USD, 1, 0.01m, 0.00000001m, string.Empty),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null);
                btcusd.SetMarketPrice(new Tick(DateTime.UtcNow, btcusd.Symbol, price, price));
                return btcusd;
            }

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
