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
 *
*/

using System;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities.Crypto;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class AlpacaFeeModelTests
    {
        private readonly IFeeModel _feeModel = new AlpacaFeeModel();

        [Test]
        public void ZeroFee()
        {
            var security = SecurityTests.GetSecurity();
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0m, fee.Value.Amount);
        }

        [Test]
        public void CryptoTakerFee()
        {
            var btcusd = GetCryptoSecurity();
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    btcusd,
                    new MarketOrder(btcusd.Symbol, 2, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(100m * 2, fee.Value.Amount);
        }

        [Test]
        public void CryptoMakerFee()
        {
            var btcusd = GetCryptoSecurity();
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    btcusd,
                    new LimitOrder(btcusd.Symbol, 2, 50000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(120m, fee.Value.Amount);
        }

        private static Security GetCryptoSecurity()
        {
            var btcusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 1),
                new Cash("BTC", 0, 40000),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCUSD, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, true, false, false),
                new SymbolProperties("BTCUSD", Currencies.USD, 1, 0.01m, 0.00000001m, string.Empty),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            btcusd.SetMarketPrice(new Tick(DateTime.UtcNow, btcusd.Symbol, 40000, 40000));
            return btcusd;
        }
    }
}
