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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class BinanceCoinFuturesFeeModelTests
    {
        [Test]
        public void GetFeeModelTest()
        {
            var model = new BinanceCoinFuturesBrokerageModel(AccountType.Margin);
            Assert.IsInstanceOf<BinanceCoinFuturesFeeModel>(model.GetFeeModel(Security));
        }

        private static void TestFeeModel(BinanceCoinFuturesFeeModel feeModel, OrderTestParameters parameters, bool shortOrder, decimal expectedFeeFactor)
        {
            var order = shortOrder ? parameters.CreateShortOrder(Quantity) : parameters.CreateLongOrder(Quantity);
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(expectedFeeFactor * Math.Abs(Quantity) * Security.SymbolProperties.ContractMultiplier / Security.Price, fee.Value.Amount);
            Assert.AreEqual(Security.BaseCurrency.Symbol, fee.Value.Currency);
        }

        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnShortOrderMakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BinanceCoinFuturesFeeModel();
            TestFeeModel(feeModel, parameters, true, BinanceCoinFuturesFeeModel.MakerTier1Fee);
        }

        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnShortOrderTakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BinanceCoinFuturesFeeModel();
            TestFeeModel(feeModel, parameters, true, BinanceCoinFuturesFeeModel.TakerTier1Fee);
        }

        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnLongOrderMakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BinanceCoinFuturesFeeModel();
            TestFeeModel(feeModel, parameters, false, BinanceCoinFuturesFeeModel.MakerTier1Fee);
        }

        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnLongOrderTakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BinanceCoinFuturesFeeModel();
            TestFeeModel(feeModel, parameters, false, BinanceCoinFuturesFeeModel.TakerTier1Fee);
        }

        [TestCaseSource(nameof(CustomMakerOrders))]
        public void ReturnShortOrderCustomMakerFees(decimal makerFee, decimal takerFee, OrderTestParameters parameters)
        {
            var feeModel = new BinanceCoinFuturesFeeModel(makerFee, takerFee);
            TestFeeModel(feeModel, parameters, true, makerFee);
        }

        [TestCaseSource(nameof(CustomTakerOrders))]
        public void ReturnShortOrderCustomTakerFees(decimal makerFee, decimal takerFee, OrderTestParameters parameters)
        {
            var feeModel = new BinanceCoinFuturesFeeModel(makerFee, takerFee);
            TestFeeModel(feeModel, parameters, true, takerFee);
        }

        [TestCaseSource(nameof(CustomMakerOrders))]
        public void ReturnLongOrderCustomMakerFees(decimal makerFee, decimal takerFee, OrderTestParameters parameters)
        {
            var feeModel = new BinanceCoinFuturesFeeModel(makerFee, takerFee);
            TestFeeModel(feeModel, parameters, false, makerFee);
        }

        [TestCaseSource(nameof(CustomTakerOrders))]
        public void ReturnLongOrderCustomTakerFees(decimal makerFee, decimal takerFee, OrderTestParameters parameters)
        {
            var feeModel = new BinanceCoinFuturesFeeModel(makerFee, takerFee);
            TestFeeModel(feeModel, parameters, false, takerFee);
        }

        private static Symbol Symbol => Symbol.Create("ETHUSD", SecurityType.CryptoFuture, Market.Binance);

        private static CryptoFuture Security
        {
            get
            {
                var security = new CryptoFuture(
                    Symbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new Cash("USD", 0, 1m),
                    new Cash("ETH", 0, 1m),
                    SymbolProperties.GetDefault("USD"),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
                security.SetMarketPrice(new Tick(DateTime.UtcNow, Symbol, LowPrice, HighPrice));

                return security;
            }
        }

        private static OrderSubmissionData OrderSubmissionData => new(Security.BidPrice, Security.AskPrice, (Security.BidPrice + Security.AskPrice) / 2);

        private static decimal HighPrice => 1000m;
        private static decimal LowPrice => 100m;

        private static decimal Quantity => 1m;

        private static TestCaseData[] MakerOrders => new[]
        {
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, null, OrderSubmissionData)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BinanceOrderProperties())),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, new BinanceOrderProperties() { PostOnly = true }, OrderSubmissionData)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BinanceOrderProperties() { PostOnly = true }))
        };

        private static TestCaseData[] TakerOrders => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)),
            new TestCaseData(new MarketOrderTestParameters(Symbol, new BinanceOrderProperties() { PostOnly = true })),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, null, OrderSubmissionData))
        };

        private static TestCaseData[] CustomMakerOrders => new[]
        {
            new TestCaseData(0.001m, 0.001m, new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(0.00008m, 0.00045m, new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, null, OrderSubmissionData)),
            new TestCaseData(0.00005m, 0.0004m, new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BinanceOrderProperties())),
            new TestCaseData(0.00003m, 0.0003m, new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, new BinanceOrderProperties() { PostOnly = true }, OrderSubmissionData)),
            new TestCaseData(0.0m, 0.00025m, new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BinanceOrderProperties() { PostOnly = true }))
        };

        private static TestCaseData[] CustomTakerOrders => new[]
        {
            new TestCaseData(0.00008m, 0.00045m, new MarketOrderTestParameters(Symbol)),
            new TestCaseData(0.00005m, 0.0004m, new MarketOrderTestParameters(Symbol, new BinanceOrderProperties { PostOnly = true })),
            new TestCaseData(0.0m, 0.00025m, new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, null, OrderSubmissionData))
        };
    }
}
