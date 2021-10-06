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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using System;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class FTXFeeTests
    {
        private Crypto _xrpusdt;
        private Crypto _ethusd;
        private readonly IFeeModel _feeModel = new FTXFeeModel();

        [SetUp]
        public void Initialize()
        {
            var spdb = SymbolPropertiesDatabase.FromDataFolder();
            var tz = TimeZones.Utc;
            _xrpusdt = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USDT", 0, 1),
                new SubscriptionDataConfig(typeof(TradeBar), Symbol.Create("XRP/USDT", SecurityType.Crypto, Market.FTX), Resolution.Minute, tz, tz, true, false, false),
                spdb.GetSymbolProperties(Market.FTX, Symbol.Create("XRP/USDT", SecurityType.Crypto, Market.FTX), SecurityType.Crypto, "USDT"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            _xrpusdt.SetMarketPrice(new Tick(DateTime.UtcNow, _xrpusdt.Symbol, 100, 100));

            _ethusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash(Currencies.USD, 0, 10),
                new SubscriptionDataConfig(typeof(TradeBar), Symbol.Create("ETH/USD", SecurityType.Crypto, Market.FTX), Resolution.Minute, tz, tz, true, false, false),
                spdb.GetSymbolProperties(Market.FTX, Symbol.Create("ETH/USD", SecurityType.Crypto, Market.FTX), SecurityType.Crypto, Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            _ethusd.SetMarketPrice(new Tick(DateTime.UtcNow, _ethusd.Symbol, 100, 100));
        }

        [Test]
        public void ReturnsTakerFeeInQuoteCurrencyInAccountCurrency()
        {
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _ethusd,
                    new MarketOrder(_ethusd.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(_ethusd.QuoteCurrency.Symbol, fee.Value.Currency);
            // 100 (price) * 0.0007 (taker fee, in quote currency)
            Assert.AreEqual(0.07m, fee.Value.Amount);
        }

        [Test]
        public void ReturnsMakerFeeInQuoteCurrencyInAccountCurrency()
        {
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _ethusd,
                    new LimitOrder(_ethusd.Symbol, -1, 100, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(_ethusd.BaseCurrencySymbol, fee.Value.Currency);
            // 0.0002 (maker fee, in base currency)
            Assert.AreEqual(0.0002, fee.Value.Amount);
        }

        [Test]
        public void ReturnsFeeInQuoteCurrencyInOtherCurrency()
        {
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _xrpusdt,
                    new MarketOrder(_xrpusdt.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual("USDT", fee.Value.Currency);
            // 100 (price) * 0.0007 (taker fee)
            Assert.AreEqual(0.07m, fee.Value.Amount);
        }
    }
}
