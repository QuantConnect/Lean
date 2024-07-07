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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;

namespace QuantConnect.Tests.Brokerages.Kraken
{
    [TestFixture]
    public class KrakenFeeModelTests
    {
        private static Symbol Symbol => Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Kraken);
        private static Symbol FiatSymbol =>
            Symbol.Create("EURUSD", SecurityType.Crypto, Market.Kraken);
        private static Security Security
        {
            get
            {
                var security = new Security(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new SubscriptionDataConfig(
                        typeof(TradeBar),
                        Symbol,
                        Resolution.Minute,
                        TimeZones.NewYork,
                        TimeZones.NewYork,
                        false,
                        false,
                        false
                    ),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
                security.SetMarketPrice(new Tick(DateTime.UtcNow, Symbol, LowPrice, HighPrice));

                return security;
            }
        }

        private static Security FiatSecurity
        {
            get
            {
                var security = new Security(
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new SubscriptionDataConfig(
                        typeof(TradeBar),
                        FiatSymbol,
                        Resolution.Minute,
                        TimeZones.NewYork,
                        TimeZones.NewYork,
                        false,
                        false,
                        false
                    ),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
                security.SetMarketPrice(new Tick(DateTime.UtcNow, Symbol, LowPrice, HighPrice));

                return security;
            }
        }

        private static OrderSubmissionData OrderSubmissionData =>
            new OrderSubmissionData(
                Security.BidPrice,
                Security.AskPrice,
                (Security.BidPrice + Security.AskPrice) / 2
            );
        private static decimal HighPrice = 1000m;
        private static decimal LowPrice = 100m;
        private static decimal Quantity = 1m;

        private static TestCaseData[] MakerOrders =>
            new[]
            {
                new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
                new TestCaseData(
                    new LimitOrderTestParameters(
                        Symbol,
                        HighPrice,
                        LowPrice,
                        new KrakenOrderProperties()
                    )
                ),
                new TestCaseData(
                    new LimitOrderTestParameters(
                        Symbol,
                        HighPrice,
                        LowPrice,
                        new KrakenOrderProperties() { PostOnly = true }
                    )
                )
            };

        private static TestCaseData[] TakerOrders =>
            new[]
            {
                new TestCaseData(new MarketOrderTestParameters(Symbol)),
                new TestCaseData(
                    new MarketOrderTestParameters(
                        Symbol,
                        new KrakenOrderProperties() { PostOnly = true }
                    )
                ),
                new TestCaseData(
                    new LimitOrderTestParameters(
                        Symbol,
                        LowPrice,
                        HighPrice,
                        new KrakenOrderProperties()
                    )
                    {
                        OrderSubmissionData = OrderSubmissionData
                    }
                ),
            };

        private static TestCaseData[] FiatsOrders =>
            new[]
            {
                new TestCaseData(new MarketOrderTestParameters(FiatSymbol)),
                new TestCaseData(
                    new MarketOrderTestParameters(
                        FiatSymbol,
                        new KrakenOrderProperties() { PostOnly = true }
                    )
                ),
                new TestCaseData(
                    new LimitOrderTestParameters(
                        FiatSymbol,
                        LowPrice,
                        HighPrice,
                        new KrakenOrderProperties()
                    )
                    {
                        OrderSubmissionData = OrderSubmissionData
                    }
                ),
            };

        [Test]
        public void GetFeeModelTest()
        {
            KrakenBrokerageModel model = new KrakenBrokerageModel();
            Assert.IsInstanceOf<KrakenFeeModel>(model.GetFeeModel(Security));
        }

        [Test]
        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnShortOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new KrakenFeeModel();

            Order order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                KrakenFeeModel.MakerTier1CryptoFee * 1 * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual("ETH", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnShortOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new KrakenFeeModel();

            Order order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                KrakenFeeModel.TakerTier1CryptoFee * 1 * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual("ETH", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnLongOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new KrakenFeeModel();

            Order order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                KrakenFeeModel.MakerTier1CryptoFee * price * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnLongOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new KrakenFeeModel();

            Order order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                KrakenFeeModel.TakerTier1CryptoFee * price * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(FiatsOrders))]
        public void ReturnLongFiatCoinFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new KrakenFeeModel();

            Order order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(FiatSecurity, order));

            Assert.AreEqual(
                KrakenFeeModel.Tier1FxFee * price * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }
    }
}
