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
using QuantConnect.Orders.Fees;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexFeeModelTests
    {
        protected Symbol Symbol => Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Bitfinex);
        protected Security Security
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
        protected OrderSubmissionData OrderSubmissionData => new OrderSubmissionData(Security.BidPrice, Security.AskPrice, (Security.BidPrice + Security.AskPrice) / 2);

        protected decimal HighPrice => 1000m;
        protected decimal LowPrice => 100m;

        protected decimal Quantity => 1m;

        public TestCaseData[] MakerOrders => new[]
        {
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice) { OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BitfinexOrderProperties())),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, new BitfinexOrderProperties() { PostOnly = true }){ OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BitfinexOrderProperties() { PostOnly = true }))
        };

        public TestCaseData[] TakerOrders => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)),
            new TestCaseData(new MarketOrderTestParameters(Symbol, new BitfinexOrderProperties() { PostOnly = true })),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice) { OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BitfinexOrderProperties() { Hidden = true })),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BitfinexOrderProperties() { PostOnly = true, Hidden = true }))
        };

        [Test]
        public void GetFeeModelTest()
        {
            BitfinexBrokerageModel model = new BitfinexBrokerageModel();
            Assert.IsInstanceOf<BitfinexFeeModel>(model.GetFeeModel(Security));
        }

        [Test]
        [TestCaseSource("MakerOrders")]
        public void ReturnShortOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BitfinexFeeModel();

            Order order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                BitfinexFeeModel.MakerFee * price * Math.Abs(Quantity), fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        [TestCaseSource("TakerOrders")]
        public void ReturnShortOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BitfinexFeeModel();

            Order order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;
            var fee =
                feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                BitfinexFeeModel.TakerFee * price * Math.Abs(Quantity), fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        [TestCaseSource("MakerOrders")]
        public void ReturnLongOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BitfinexFeeModel();

            Order order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee =
                feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                BitfinexFeeModel.MakerFee * price * Math.Abs(Quantity), fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        [TestCaseSource("TakerOrders")]
        public void ReturnLongOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BitfinexFeeModel();

            Order order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee =
                feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                BitfinexFeeModel.TakerFee * price * Math.Abs(Quantity), fee.Value.Amount);
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }
    }
}
