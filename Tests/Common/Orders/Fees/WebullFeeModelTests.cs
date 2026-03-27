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
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class WebullFeeModelTests
    {
        private readonly WebullFeeModel _feeModel = new WebullFeeModel();

        // ── Equity / Option — zero commission ────────────────────────────────────

        [Test]
        public void GetOrderFee_Equity_ReturnsZero()
        {
            var security = SecurityTests.GetSecurity();
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100m, 100m));
            var order = new MarketOrder(security.Symbol, 10m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0m));
        }

        [Test]
        public void GetOrderFee_Option_ReturnsZero()
        {
            var security = CreateOptionSecurity("AAPL", 5m);
            var order = new MarketOrder(security.Symbol, 3m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0m));
        }

        // ── IndexOption — SPX ─────────────────────────────────────────────────────

        /// <summary>
        /// SPX, price &lt; $1 → exchange $0.57 + Webull $0.50 = $1.07/contract.
        /// 2 contracts → $2.14.
        /// </summary>
        [Test]
        public void GetOrderFee_SpxPriceBelow1_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("SPX", price: 0.50m);
            var order = new MarketOrder(security.Symbol, 2m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(2.14m));
            Assert.That(fee.Value.Currency, Is.EqualTo(Currencies.USD));
        }

        /// <summary>
        /// SPX, price ≥ $1 → exchange $0.66 + Webull $0.50 = $1.16/contract.
        /// 3 contracts → $3.48.
        /// </summary>
        [Test]
        public void GetOrderFee_SpxPriceAbove1_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("SPX", price: 1.50m);
            var order = new MarketOrder(security.Symbol, 3m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(3.48m));
        }

        // ── IndexOption — SPXW ────────────────────────────────────────────────────

        /// <summary>
        /// SPXW, price &lt; $1 → exchange $0.50 + Webull $0.50 = $1.00/contract.
        /// </summary>
        [Test]
        public void GetOrderFee_SpxwPriceBelow1_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("SPXW", price: 0.80m);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(1.00m));
        }

        /// <summary>
        /// SPXW, price ≥ $1 → exchange $0.59 + Webull $0.50 = $1.09/contract.
        /// 4 contracts → $4.36.
        /// </summary>
        [Test]
        public void GetOrderFee_SpxwPriceAbove1_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("SPXW", price: 2.00m);
            var order = new MarketOrder(security.Symbol, 4m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(4.36m));
        }

        // ── IndexOption — VIX ─────────────────────────────────────────────────────

        /// <summary>
        /// VIX, price ≤ $0.10 → exchange $0.10 + Webull $0.50 = $0.60/contract.
        /// 4 contracts → $2.40.
        /// </summary>
        [Test]
        public void GetOrderFee_VixPriceTier1_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("VIX", price: 0.05m);
            var order = new MarketOrder(security.Symbol, 4m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(2.40m));
        }

        /// <summary>
        /// VIX, price $0.11–$0.99 → exchange $0.25 + Webull $0.50 = $0.75/contract.
        /// 2 contracts → $1.50.
        /// </summary>
        [Test]
        public void GetOrderFee_VixPriceTier2_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("VIX", price: 0.50m);
            var order = new MarketOrder(security.Symbol, 2m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(1.50m));
        }

        /// <summary>
        /// VIX, price $1.00–$1.99 → exchange $0.40 + Webull $0.50 = $0.90/contract.
        /// </summary>
        [Test]
        public void GetOrderFee_VixPriceTier3_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("VIX", price: 1.50m);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(0.90m));
        }

        /// <summary>
        /// VIX, price ≥ $2.00 → exchange $0.45 + Webull $0.50 = $0.95/contract.
        /// 5 contracts → $4.75.
        /// </summary>
        [Test]
        public void GetOrderFee_VixPriceTier4_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("VIX", price: 3.00m);
            var order = new MarketOrder(security.Symbol, 5m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(4.75m));
        }

        // ── IndexOption — VIXW ────────────────────────────────────────────────────

        /// <summary>
        /// VIXW uses identical tier schedule to VIX; verify tier 2.
        /// </summary>
        [Test]
        public void GetOrderFee_VixwPriceTier2_MatchesVixFee()
        {
            var vix = CreateIndexOptionSecurity("VIX", price: 0.50m);
            var vixw = CreateIndexOptionSecurity("VIXW", price: 0.50m);
            var order = new MarketOrder(vix.Symbol, 2m, DateTime.UtcNow);

            var vixFee = _feeModel.GetOrderFee(new OrderFeeParameters(vix, order));
            var vixwFee = _feeModel.GetOrderFee(new OrderFeeParameters(vixw, new MarketOrder(vixw.Symbol, 2m, DateTime.UtcNow)));

            Assert.That(vixFee.Value.Amount, Is.EqualTo(vixwFee.Value.Amount));
        }

        // ── IndexOption — XSP ─────────────────────────────────────────────────────

        /// <summary>
        /// XSP, qty &lt; 10 → exchange $0.00 + Webull $0.50 = $0.50/contract.
        /// 5 contracts → $2.50.
        /// </summary>
        [Test]
        public void GetOrderFee_XspSmallQuantity_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("XSP", price: 1.00m);
            var order = new MarketOrder(security.Symbol, 5m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(2.50m));
        }

        /// <summary>
        /// XSP, qty ≥ 10 → exchange $0.07 + Webull $0.50 = $0.57/contract.
        /// 10 contracts → $5.70.
        /// </summary>
        [Test]
        public void GetOrderFee_XspLargeQuantity_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("XSP", price: 1.00m);
            var order = new MarketOrder(security.Symbol, 10m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(5.70m));
        }

        // ── IndexOption — DJX ─────────────────────────────────────────────────────

        /// <summary>
        /// DJX flat → exchange $0.18 + Webull $0.50 = $0.68/contract.
        /// 2 contracts → $1.36.
        /// </summary>
        [Test]
        public void GetOrderFee_Djx_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("DJX", price: 2.00m);
            var order = new MarketOrder(security.Symbol, 2m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(1.36m));
        }

        // ── IndexOption — NDX / NDXP ──────────────────────────────────────────────

        /// <summary>
        /// NDX, single-leg, premium &lt; $25 → exchange $0.50 + Webull $0.50 = $1.00/contract.
        /// 3 contracts → $3.00.
        /// </summary>
        [Test]
        public void GetOrderFee_NdxSingleLegPriceBelow25_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("NDX", price: 10.00m);
            var order = new MarketOrder(security.Symbol, 3m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(3.00m));
        }

        /// <summary>
        /// NDX, single-leg, premium ≥ $25 → exchange $0.75 + Webull $0.50 = $1.25/contract.
        /// 2 contracts → $2.50.
        /// </summary>
        [Test]
        public void GetOrderFee_NdxSingleLegPriceAbove25_ReturnsCorrectFee()
        {
            var security = CreateIndexOptionSecurity("NDX", price: 50.00m);
            var order = new MarketOrder(security.Symbol, 2m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.That(fee.Value.Amount, Is.EqualTo(2.50m));
        }

        /// <summary>
        /// NDXP uses the same fee schedule as NDX.
        /// </summary>
        [Test]
        public void GetOrderFee_NdxpSingleLegPriceBelow25_MatchesNdxFee()
        {
            var ndx = CreateIndexOptionSecurity("NDX", price: 10.00m);
            var ndxp = CreateIndexOptionSecurity("NDXP", price: 10.00m);
            var ndxOrder = new MarketOrder(ndx.Symbol, 1m, DateTime.UtcNow);
            var ndxpOrder = new MarketOrder(ndxp.Symbol, 1m, DateTime.UtcNow);

            var ndxFee = _feeModel.GetOrderFee(new OrderFeeParameters(ndx, ndxOrder));
            var ndxpFee = _feeModel.GetOrderFee(new OrderFeeParameters(ndxp, ndxpOrder));

            Assert.That(ndxFee.Value.Amount, Is.EqualTo(ndxpFee.Value.Amount));
        }

        // ── Crypto ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Crypto fee = 0.6% of notional (quantity × price).
        /// 2 BTC × $50,000 = $100,000 notional → fee = $600.
        /// </summary>
        [Test]
        public void GetOrderFee_Crypto_ReturnsPointSixPercentOfNotional()
        {
            var btcusd = CreateCryptoSecurity(price: 50_000m);
            var order = new MarketOrder(btcusd.Symbol, 2m, DateTime.UtcNow);

            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(btcusd, order));

            // 2 * 50000 * 0.006 = 600
            Assert.That(fee.Value.Amount, Is.EqualTo(600m));
            Assert.That(fee.Value.Currency, Is.EqualTo(Currencies.USD));
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an index option security with the given underlying ticker and option price.
        /// Uses <see cref="Symbol.CreateOption(Symbol,string,OptionStyle,OptionRight,decimal,DateTime)"/>
        /// with an Index underlying to produce a <see cref="SecurityType.IndexOption"/> symbol.
        /// </summary>
        private static Security CreateIndexOptionSecurity(string underlyingTicker, decimal price)
        {
            var underlying = Symbol.Create(underlyingTicker, SecurityType.Index, Market.USA);
            var symbol = Symbol.CreateOption(
                underlying, Market.USA, OptionStyle.European, OptionRight.Call, 1000m, new DateTime(2026, 6, 20));

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

        /// <summary>
        /// Creates a standard equity option security (SecurityType.Option) for zero-fee assertions.
        /// </summary>
        private static Security CreateOptionSecurity(string underlyingTicker, decimal price)
        {
            var underlying = Symbol.Create(underlyingTicker, SecurityType.Equity, Market.USA);
            var symbol = Symbol.CreateOption(
                underlying, Market.USA, OptionStyle.American, OptionRight.Call, 150m, new DateTime(2026, 6, 20));

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

        /// <summary>
        /// Creates a crypto security with the given USD price set via <see cref="Security.SetMarketPrice"/>.
        /// </summary>
        private static Crypto CreateCryptoSecurity(decimal price)
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
    }
}
