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
using System.Linq;

using NUnit.Framework;

using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class BybitFuturesFeeModelTests
    {
        [Test]
        public void GetFeeModelTest()
        {
            var model = new BybitBrokerageModel(AccountType.Margin);
            Assert.IsInstanceOf<BybitFuturesFeeModel>(model.GetFeeModel(security));
        }

        private static void TestFeeModel(
            BybitFuturesFeeModel feeModel,
            OrderTestParameters parameters,
            bool shortOrder,
            decimal expectedFeeFactor
            )
        {
            var order = shortOrder ? parameters.CreateShortOrder(Quantity) : parameters.CreateLongOrder(Quantity);
            var securit = security;
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            var expectedFee = expectedFeeFactor * Math.Abs(Quantity) * security.SymbolProperties.ContractMultiplier *
                security.Price;
            Assert.AreEqual(expectedFee, fee.Value.Amount);
            Assert.AreEqual(security.QuoteCurrency.Symbol, fee.Value.Currency);
        }



        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnShortOrderMakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BybitFuturesFeeModel();

            TestFeeModel(feeModel, parameters, true, BybitFuturesFeeModel.MakerNonVIPFee);
        }

        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnShortOrderTakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BybitFuturesFeeModel();


            TestFeeModel(feeModel, parameters, true, BybitFuturesFeeModel.TakerNonVIPFee);
        }

        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnLongOrderMakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BybitFuturesFeeModel();


            TestFeeModel(feeModel, parameters, false, BybitFuturesFeeModel.MakerNonVIPFee);
        }

        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnLongOrderTakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BybitFuturesFeeModel();

            TestFeeModel(feeModel, parameters, false, BybitFuturesFeeModel.TakerNonVIPFee);
        }

        [TestCaseSource(nameof(CustomMakerOrders))]
        public void ReturnShortOrderCustomMakerFees(
            decimal makerUsdtFee,
            decimal takerUsdtFee,
            OrderTestParameters parameters
            )
        {
            var feeModel = new BybitFuturesFeeModel(makerUsdtFee, takerUsdtFee);

            TestFeeModel(feeModel, parameters, true, makerUsdtFee);
        }

        [TestCaseSource(nameof(CustomTakerOrders))]
        public void ReturnShortOrderCustomTakerFees(
            decimal makerUsdtFee,
            decimal takerUsdtFee,
            OrderTestParameters parameters
            )
        {
            var feeModel = new BybitFuturesFeeModel(makerUsdtFee, takerUsdtFee);

            TestFeeModel(feeModel, parameters, true, takerUsdtFee);
        }

        [TestCaseSource(nameof(CustomMakerOrders))]
        public void ReturnLongOrderCustomMakerFees(
            decimal makerUsdtFee,
            decimal takerUsdtFee,
            OrderTestParameters parameters
            )
        {
            var feeModel = new BybitFuturesFeeModel(makerUsdtFee, takerUsdtFee);


            TestFeeModel(feeModel, parameters, false, makerUsdtFee);
        }

        [TestCaseSource(nameof(CustomTakerOrders))]
        public void ReturnLongOrderCustomTakerFees(
            decimal makerUsdtFee,
            decimal takerUsdtFee,
            OrderTestParameters parameters
            )
        {
            var feeModel = new BybitFuturesFeeModel(makerUsdtFee, takerUsdtFee);

            TestFeeModel(feeModel, parameters, false, takerUsdtFee);
        }

        private static readonly Symbol Symbol = Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Bybit);

        private static CryptoFuture security
        {
            get
            {
                CurrencyPairUtil.DecomposeCurrencyPair(Symbol, out var baseCurrency, out var quoteCurrency);
                var security = new CryptoFuture(
                    Symbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    new Cash(quoteCurrency, 0, 1m),
                    new Cash(baseCurrency, 0, 1m),
                    SymbolProperties.GetDefault(quoteCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
                security.SetMarketPrice(new Tick(DateTime.UtcNow, Symbol, LowPrice, HighPrice));

                return security;
            }
        }

        private static readonly OrderSubmissionData OrderSubmissionData = new OrderSubmissionData(security.BidPrice, security.AskPrice, (security.BidPrice + security.AskPrice) / 2);

        private static decimal HighPrice => 1000m;
        private static decimal LowPrice => 100m;
        private static decimal Quantity => 1m;

        private static TestCaseData[] MakerOrders => new[]
        {
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, null,
                OrderSubmissionData)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BybitOrderProperties())),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice,
                new BybitOrderProperties() { PostOnly = true }, OrderSubmissionData)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice,
                new BybitOrderProperties() { PostOnly = true }))
        };

        private static TestCaseData[] TakerOrders => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)),
            new TestCaseData(new MarketOrderTestParameters(Symbol, new BybitOrderProperties() { PostOnly = true })),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, null,
                OrderSubmissionData))
        };

        private static TestCaseData[] CustomMakerOrders => new[]
        {
            new TestCaseData(0.0002m, 0.0004m,
                new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(0.00016m, 0.0004m,
                new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, null, OrderSubmissionData)),
            new TestCaseData(0.00014m, 0.00035m,
                new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new BybitOrderProperties())),
            new TestCaseData(0.00012m, 0.00032m,
                new LimitOrderTestParameters(Symbol, LowPrice, HighPrice,
                    new BybitOrderProperties() { PostOnly = true },
                    OrderSubmissionData)),
            new TestCaseData(0.0001m, 0.0003m,
                new LimitOrderTestParameters(Symbol, HighPrice, LowPrice,
                    new BybitOrderProperties() { PostOnly = true }))
        };

        private static TestCaseData[] CustomTakerOrders => new[]
        {
            new TestCaseData(0.00016m, 0.0004m,
                new MarketOrderTestParameters(Symbol)),
            new TestCaseData(0.00014m, 0.00035m,
                new MarketOrderTestParameters(Symbol, new BybitOrderProperties { PostOnly = true })),
            new TestCaseData(0.00012m, 0.00032m,
                new LimitOrderTestParameters(Symbol, LowPrice, HighPrice, null, OrderSubmissionData))
        };
    }
}
