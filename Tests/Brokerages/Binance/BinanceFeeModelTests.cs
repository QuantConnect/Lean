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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture]
    public class BinanceFeeModelTests
    {
        [Test]
        public static void GetFeeModelTest()
        {
            var model = new BinanceBrokerageModel();
            Assert.IsInstanceOf<BinanceFeeModel>(model.GetFeeModel(Security));
        }

        [Test]
        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnShortOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BinanceFeeModel();

            var order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                BinanceFeeModel.MakerTier1Fee * price * Math.Abs(Quantity),
                fee.Value.Amount);
            Assert.AreEqual("USDT", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnShortOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BinanceFeeModel();

            var order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                BinanceFeeModel.TakerTier1Fee * price * Math.Abs(Quantity),
                fee.Value.Amount);
            Assert.AreEqual("USDT", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnLongOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BinanceFeeModel();

            var order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                BinanceFeeModel.MakerTier1Fee * price * Math.Abs(Quantity),
                fee.Value.Amount);
            Assert.AreEqual("USDT", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnLongOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BinanceFeeModel();

            var order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                BinanceFeeModel.TakerTier1Fee * price * Math.Abs(Quantity),
                fee.Value.Amount);
            Assert.AreEqual("USDT", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(CustomMakerOrders))]
        public void ReturnShortOrderCustomMakerFees(decimal mFee, decimal tFee, OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BinanceFeeModel(mFee, tFee);

            var order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                mFee * price * Math.Abs(Quantity),
                fee.Value.Amount);
            Assert.AreEqual("USDT", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(CustomTakerOrders))]
        public void ReturnShortOrderCustomTakerFees(decimal mFee, decimal tFee, OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BinanceFeeModel(mFee, tFee);

            var order = parameters.CreateShortOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : LowPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                tFee * price * Math.Abs(Quantity),
                fee.Value.Amount);
            Assert.AreEqual("USDT", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(CustomMakerOrders))]
        public void ReturnLongOrderCustomMakerFees(decimal mFee, decimal tFee, OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BinanceFeeModel(mFee, tFee);

            var order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                mFee * price * Math.Abs(Quantity),
                fee.Value.Amount);
            Assert.AreEqual("USDT", fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(CustomTakerOrders))]
        public void ReturnLongOrderCustomTakerFees(decimal mFee, decimal tFee, OrderTestParameters parameters)
        {
            IFeeModel feeModel = new BinanceFeeModel(mFee, tFee);

            var order = parameters.CreateLongOrder(Quantity);
            var price = order.Type == OrderType.Limit ? ((LimitOrder)order).LimitPrice : HighPrice;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                tFee * price * Math.Abs(Quantity),
                fee.Value.Amount);
            Assert.AreEqual("USDT", fee.Value.Currency);
        }

        private static Symbol Symbol => Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance);

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
                    new Cash("USDT", 0, 1m),
                    SymbolProperties.GetDefault("USDT"),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
                security.SetMarketPrice(new Tick(DateTime.UtcNow, Symbol, LowPrice, HighPrice));

                return security;
            }
        }

        private static OrderSubmissionData OrderSubmissionData => new OrderSubmissionData(Security.BidPrice, Security.AskPrice, (Security.BidPrice + Security.AskPrice) / 2);

        private static decimal HighPrice => 1000m;
        private static decimal LowPrice => 100m;

        private static decimal Quantity => 1m;

        private static TestCaseData[] MakerOrders => new[]
        {
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice) { OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BinanceOrderProperties())),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, new BinanceOrderProperties() { PostOnly = true }){ OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BinanceOrderProperties() { PostOnly = true }))
        };

        private static TestCaseData[] TakerOrders => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)),
            new TestCaseData(new MarketOrderTestParameters(Symbol, new BinanceOrderProperties() { PostOnly = true })),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice) { OrderSubmissionData = OrderSubmissionData})
        };

        private static TestCaseData[] CustomMakerOrders => new[]
        {
            new TestCaseData(0.001m, 0.001m, new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(0.0009m, 0.001m, new LimitOrderTestParameters(Symbol, HighPrice, LowPrice) { OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(0.0008m, 0.001m, new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BinanceOrderProperties())),
            new TestCaseData(0.0007m, 0.0009m, new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, new BinanceOrderProperties() { PostOnly = true }){ OrderSubmissionData = OrderSubmissionData}),
            new TestCaseData(0.0006m, 0.0008m, new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BinanceOrderProperties() { PostOnly = true }))
        };

        private static TestCaseData[] CustomTakerOrders => new[]
        {
            new TestCaseData(0.0007m, 0.0009m, new MarketOrderTestParameters(Symbol)),
            new TestCaseData(0.0006m, 0.0008m, new MarketOrderTestParameters(Symbol, new BinanceOrderProperties { PostOnly = true })),
            new TestCaseData(0.0005m, 0.0006m, new LimitOrderTestParameters(Symbol, LowPrice, HighPrice) { OrderSubmissionData = OrderSubmissionData})
        };
    }
}
