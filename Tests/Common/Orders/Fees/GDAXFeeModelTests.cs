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

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    class GDAXFeeModelTests
    {
        private Crypto _btcusd;
        private Crypto _btceur;
        private readonly IFeeModel _feeModel = new GDAXFeeModel();

        [SetUp]
        public void Initialize()
        {
            var tz = TimeZones.NewYork;
            _btcusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash(Currencies.USD, 0, 1),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCUSD, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("BTCUSD", Currencies.USD, 1, 0.01m, 0.00000001m, string.Empty),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            _btcusd.SetMarketPrice(new Tick(DateTime.UtcNow, _btcusd.Symbol, 100, 100));

            _btceur = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("EUR", 0, 10),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.BTCEUR, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("BTCEUR", "EUR", 1, 0.01m, 0.00000001m, string.Empty),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            _btceur.SetMarketPrice(new Tick(DateTime.UtcNow, _btceur.Symbol, 100, 100));
        }

        [Test]
        public void ReturnsFeeInQuoteCurrencyInAccountCurrency()
        {
            var time = new DateTime(2019, 2, 1);
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _btcusd,
                    new MarketOrder(_btcusd.Symbol, 1, time)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            // 100 (price) * 0.003 (taker fee)
            Assert.AreEqual(0.3m, fee.Value.Amount);
        }

        [Test]
        public void ReturnsFeeInQuoteCurrencyInOtherCurrency()
        {
            var time = new DateTime(2019, 2, 1);
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _btceur,
                    new MarketOrder(_btceur.Symbol, 1, time)
                )
            );

            Assert.AreEqual("EUR", fee.Value.Currency);
            // 100 (price) * 0.003 (taker fee)
            Assert.AreEqual(0.3m, fee.Value.Amount);
        }

        [TestCase(2019, 2, 1, 0, 0, 0, 0.3)]
        [TestCase(2019, 3, 23, 1, 29, 59, 0.3)]
        [TestCase(2019, 3, 23, 1, 30, 0, 0.25)]
        [TestCase(2019, 4, 1, 0, 0, 0, 0.25)]
        public void FeeChangesOverTime(int year, int month, int day, int hour, int minute, int second, decimal expectedFee)
        {
            var time = new DateTime(year, month, day, hour, minute, second);
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _btcusd,
                    new MarketOrder(_btcusd.Symbol, 1, time)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            // 100 (price) * fee (taker fee)
            Assert.AreEqual(expectedFee, fee.Value.Amount);
        }
    }
}
