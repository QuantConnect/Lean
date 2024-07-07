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
    public class FTXFeeTests
    {
        private Crypto _xrpusdt;
        private Crypto _ethusd;
        private IFeeModel _feeModel;

        protected decimal TakerFee { get; set; }
        protected decimal MakerFee { get; set; }

        [SetUp]
        public void Initialize()
        {
            _feeModel = GetFeeModel();
            SetBrokerageFees();
            var spdb = SymbolPropertiesDatabase.FromDataFolder();
            var tz = TimeZones.Utc;
            _xrpusdt = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USDT", 0, 1),
                new Cash("XRP", 0, 0),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbol.Create("XRPUSDT", SecurityType.Crypto, Market.FTX),
                    Resolution.Minute,
                    tz,
                    tz,
                    true,
                    false,
                    false
                ),
                spdb.GetSymbolProperties(
                    Market.FTX,
                    Symbol.Create("XRPUSDT", SecurityType.Crypto, Market.FTX),
                    SecurityType.Crypto,
                    "USDT"
                ),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            _xrpusdt.SetMarketPrice(new Tick(DateTime.UtcNow, _xrpusdt.Symbol, 100, 100));

            _ethusd = new Crypto(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash(Currencies.USD, 0, 10),
                new Cash("ETH", 0, 0),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbol.Create("ETHUSD", SecurityType.Crypto, Market.FTX),
                    Resolution.Minute,
                    tz,
                    tz,
                    true,
                    false,
                    false
                ),
                spdb.GetSymbolProperties(
                    Market.FTX,
                    Symbol.Create("ETHUSD", SecurityType.Crypto, Market.FTX),
                    SecurityType.Crypto,
                    Currencies.USD
                ),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            _ethusd.SetMarketPrice(new Tick(DateTime.UtcNow, _ethusd.Symbol, 100, 100));
        }

        protected virtual void SetBrokerageFees()
        {
            MakerFee = 0.02m;
            TakerFee = 0.07m;
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void ReturnsTakerFeeInQuoteCurrency(decimal quantity)
        {
            //{
            //    "channel": "fills",
            //    "type": "update",
            //    "data": {
            //        "id": 4199228419,
            //        "market": "XRP/USDT",
            //        "future": null,
            //        "baseCurrency": "XRP",
            //        "quoteCurrency": "USDT",
            //        "type": "order",
            //        "side": "sell",
            //        "price": 1.07225,
            //        "size": 10,
            //        "orderId": 85922585621,
            //        "time": "2021-10-07T19:25:45.411201+00:00",
            //        "tradeId": 2084391649,
            //        "feeRate": 0.0007,
            //        "fee": 0.00750575,
            //        "feeCurrency": "USDT",
            //        "liquidity": "taker"
            //    }
            //}

            //{
            //    "channel": "fills",
            //    "type": "update",
            //    "data": {
            //        "id": 4199574804,
            //        "market": "XRP/USDT",
            //        "future": null,
            //        "baseCurrency": "XRP",
            //        "quoteCurrency": "USDT",
            //        "type": "order",
            //        "side": "buy",
            //        "price": 1.075725,
            //        "size": 1,
            //        "orderId": 85928918834,
            //        "time": "2021-10-07T20:02:07.116972+00:00",
            //        "tradeId": 2084563562,
            //        "feeRate": 0.0007,
            //        "fee": 0.0007530075,
            //        "feeCurrency": "USDT",
            //        "liquidity": "taker"
            //    }
            //}

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _ethusd,
                    new MarketOrder(_ethusd.Symbol, quantity, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(_ethusd.QuoteCurrency.Symbol, fee.Value.Currency);
            // 100 (price) * 0.0007 (taker fee, in quote currency)
            Assert.AreEqual(TakerFee, fee.Value.Amount);
        }

        [Test]
        public void ReturnsMakerFeeInQuoteCurrency()
        {
            //{
            //    "channel": "fills",
            //    "type": "update",
            //    "data": {
            //        "id": 4199162157,
            //        "market": "XRP/USDT",
            //        "future": null,
            //        "baseCurrency": "XRP",
            //        "quoteCurrency": "USDT",
            //        "type": "order",
            //        "side": "sell",
            //        "price": 1.074,
            //        "size": 1,
            //        "orderId": 85920785762,
            //        "time": "2021-10-07T19:19:27.092534+00:00",
            //        "tradeId": 2084358777,
            //        "feeRate": 0.0002,
            //        "fee": 0.0002148,
            //        "feeCurrency": "USDT",
            //        "liquidity": "maker"
            //    }
            //}

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _ethusd,
                    new LimitOrder(_ethusd.Symbol, -1, 100, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(_ethusd.QuoteCurrency.Symbol, fee.Value.Currency);
            // 0.0002 (maker fee, in quote currency)
            Assert.AreEqual(MakerFee, fee.Value.Amount);
        }

        [Test]
        public void ReturnsMakerFeeInBaseCurrency()
        {
            //{
            //    "channel": "fills",
            //    "type": "update",
            //    "data": {
            //        "id": 4199609111,
            //        "market": "XRP/USDT",
            //        "future": null,
            //        "baseCurrency": "XRP",
            //        "quoteCurrency": "USDT",
            //        "type": "order",
            //        "side": "buy",
            //        "price": 1.077,
            //        "size": 1,
            //        "orderId": 85929414038,
            //        "time": "2021-10-07T20:05:40.241875+00:00",
            //        "tradeId": 2084580551,
            //        "feeRate": 0.0002,
            //        "fee": 0.0002,
            //        "feeCurrency": "XRP",
            //        "liquidity": "maker"
            //    }
            //}

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _ethusd,
                    new LimitOrder(_ethusd.Symbol, 1, 100, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(_ethusd.BaseCurrency.Symbol, fee.Value.Currency);
            // 0.0002 (maker fee, in base currency)
            Assert.AreEqual(MakerFee / 100, fee.Value.Amount);
        }

        [Test]
        public void ReturnsFeeInQuoteCurrencyInNonAccountCurrency()
        {
            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    _xrpusdt,
                    new MarketOrder(_xrpusdt.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual("USDT", fee.Value.Currency);
            // 100 (price) * 0.0007 (taker fee)
            Assert.AreEqual(TakerFee, fee.Value.Amount);
        }

        protected virtual FTXFeeModel GetFeeModel() => new();
    }
}
