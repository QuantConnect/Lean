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
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Tests.Brokerages;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class BinanceFuturesFeeModelTests
    {
        [Test]
        public void GetFeeModelTest()
        {
            var model = new BinanceFuturesBrokerageModel(AccountType.Margin);
            Assert.IsInstanceOf<BinanceFuturesFeeModel>(model.GetFeeModel(Securities[0]));
        }

        private static void TestFeeModel(
            BinanceFuturesFeeModel feeModel,
            OrderTestParameters parameters,
            bool shortOrder,
            decimal expectedFeeFactor
        )
        {
            var order = shortOrder
                ? parameters.CreateShortOrder(Quantity)
                : parameters.CreateLongOrder(Quantity);
            var security = Securities.First(x => x.Symbol == order.Symbol);
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            var expectedFee =
                expectedFeeFactor
                * Math.Abs(Quantity)
                * security.SymbolProperties.ContractMultiplier
                * security.Price;
            Assert.AreEqual(expectedFee, fee.Value.Amount);
            Assert.AreEqual(security.QuoteCurrency.Symbol, fee.Value.Currency);
        }

        private static decimal GetExpectedFee(Symbol symbol, decimal usdtFee, decimal busdFee)
        {
            var security = Securities.First(x => x.Symbol == symbol);
            return security.QuoteCurrency.Symbol == "USDT" ? usdtFee : busdFee;
        }

        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnShortOrderMakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BinanceFuturesFeeModel();
            var expectedMakerFee = GetExpectedFee(
                parameters.Symbol,
                BinanceFuturesFeeModel.MakerTier1USDTFee,
                BinanceFuturesFeeModel.MakerTier1BUSDFee
            );

            TestFeeModel(feeModel, parameters, true, expectedMakerFee);
        }

        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnShortOrderTakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BinanceFuturesFeeModel();
            var expectedTakerFee = GetExpectedFee(
                parameters.Symbol,
                BinanceFuturesFeeModel.TakerTier1USDTFee,
                BinanceFuturesFeeModel.TakerTier1BUSDFee
            );

            TestFeeModel(feeModel, parameters, true, expectedTakerFee);
        }

        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnLongOrderMakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BinanceFuturesFeeModel();
            var expectedMakerFee = GetExpectedFee(
                parameters.Symbol,
                BinanceFuturesFeeModel.MakerTier1USDTFee,
                BinanceFuturesFeeModel.MakerTier1BUSDFee
            );

            TestFeeModel(feeModel, parameters, false, expectedMakerFee);
        }

        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnLongOrderTakerFees(OrderTestParameters parameters)
        {
            var feeModel = new BinanceFuturesFeeModel();
            var expectedTakerFee = GetExpectedFee(
                parameters.Symbol,
                BinanceFuturesFeeModel.TakerTier1USDTFee,
                BinanceFuturesFeeModel.TakerTier1BUSDFee
            );

            TestFeeModel(feeModel, parameters, false, expectedTakerFee);
        }

        [TestCaseSource(nameof(CustomMakerOrders))]
        public void ReturnShortOrderCustomMakerFees(
            decimal makerUsdtFee,
            decimal takerUsdtFee,
            decimal makerBusdFee,
            decimal takerBusdFee,
            OrderTestParameters parameters
        )
        {
            var feeModel = new BinanceFuturesFeeModel(
                makerUsdtFee,
                takerUsdtFee,
                makerBusdFee,
                takerBusdFee
            );
            var expectedMakerFee = GetExpectedFee(parameters.Symbol, makerUsdtFee, makerBusdFee);

            TestFeeModel(feeModel, parameters, true, expectedMakerFee);
        }

        [TestCaseSource(nameof(CustomTakerOrders))]
        public void ReturnShortOrderCustomTakerFees(
            decimal makerUsdtFee,
            decimal takerUsdtFee,
            decimal makerBusdFee,
            decimal takerBusdFee,
            OrderTestParameters parameters
        )
        {
            var feeModel = new BinanceFuturesFeeModel(
                makerUsdtFee,
                takerUsdtFee,
                makerBusdFee,
                takerBusdFee
            );
            var expectedTakerFee = GetExpectedFee(parameters.Symbol, takerUsdtFee, takerBusdFee);

            TestFeeModel(feeModel, parameters, true, expectedTakerFee);
        }

        [TestCaseSource(nameof(CustomMakerOrders))]
        public void ReturnLongOrderCustomMakerFees(
            decimal makerUsdtFee,
            decimal takerUsdtFee,
            decimal makerBusdFee,
            decimal takerBusdFee,
            OrderTestParameters parameters
        )
        {
            var feeModel = new BinanceFuturesFeeModel(
                makerUsdtFee,
                takerUsdtFee,
                makerBusdFee,
                takerBusdFee
            );
            var expectedMakerFee = GetExpectedFee(parameters.Symbol, makerUsdtFee, makerBusdFee);

            TestFeeModel(feeModel, parameters, false, expectedMakerFee);
        }

        [TestCaseSource(nameof(CustomTakerOrders))]
        public void ReturnLongOrderCustomTakerFees(
            decimal makerUsdtFee,
            decimal takerUsdtFee,
            decimal makerBusdFee,
            decimal takerBusdFee,
            OrderTestParameters parameters
        )
        {
            var feeModel = new BinanceFuturesFeeModel(
                makerUsdtFee,
                takerUsdtFee,
                makerBusdFee,
                takerBusdFee
            );
            var expectedTakerFee = GetExpectedFee(parameters.Symbol, takerUsdtFee, takerBusdFee);

            TestFeeModel(feeModel, parameters, false, expectedTakerFee);
        }

        private static readonly List<Symbol> Symbols = new List<Symbol>
        {
            Symbol.Create("ETHUSDT", SecurityType.CryptoFuture, Market.Binance),
            Symbol.Create("ETHBUSD", SecurityType.CryptoFuture, Market.Binance)
        };

        private static readonly List<CryptoFuture> Securities = Symbols
            .Select(symbol =>
            {
                CurrencyPairUtil.DecomposeCurrencyPair(
                    symbol,
                    out var baseCurrency,
                    out var quoteCurrency
                );
                var security = new CryptoFuture(
                    symbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    new Cash(quoteCurrency, 0, 1m),
                    new Cash(baseCurrency, 0, 1m),
                    SymbolProperties.GetDefault(quoteCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
                security.SetMarketPrice(new Tick(DateTime.UtcNow, symbol, LowPrice, HighPrice));

                return security;
            })
            .ToList();

        private static readonly Dictionary<Symbol, OrderSubmissionData> OrderSubmissionData =
            Securities.ToDictionary(
                security => security.Symbol,
                security => new OrderSubmissionData(
                    security.BidPrice,
                    security.AskPrice,
                    (security.BidPrice + security.AskPrice) / 2
                )
            );

        private static decimal HighPrice => 1000m;
        private static decimal LowPrice => 100m;
        private static decimal Quantity => 1m;

        private static TestCaseData[] MakerOrders =>
            Symbols
                .Select(symbol =>
                    new[]
                    {
                        new TestCaseData(new LimitOrderTestParameters(symbol, HighPrice, LowPrice)),
                        new TestCaseData(
                            new LimitOrderTestParameters(
                                symbol,
                                HighPrice,
                                LowPrice,
                                null,
                                OrderSubmissionData[symbol]
                            )
                        ),
                        new TestCaseData(
                            new LimitOrderTestParameters(
                                symbol,
                                HighPrice,
                                LowPrice,
                                new BinanceOrderProperties()
                            )
                        ),
                        new TestCaseData(
                            new LimitOrderTestParameters(
                                symbol,
                                LowPrice,
                                HighPrice,
                                new BinanceOrderProperties() { PostOnly = true },
                                OrderSubmissionData[symbol]
                            )
                        ),
                        new TestCaseData(
                            new LimitOrderTestParameters(
                                symbol,
                                HighPrice,
                                LowPrice,
                                new BinanceOrderProperties() { PostOnly = true }
                            )
                        )
                    }
                )
                .SelectMany(x => x)
                .ToArray();

        private static TestCaseData[] TakerOrders =>
            Symbols
                .Select(symbol =>
                    new[]
                    {
                        new TestCaseData(new MarketOrderTestParameters(symbol)),
                        new TestCaseData(
                            new MarketOrderTestParameters(
                                symbol,
                                new BinanceOrderProperties() { PostOnly = true }
                            )
                        ),
                        new TestCaseData(
                            new LimitOrderTestParameters(
                                symbol,
                                LowPrice,
                                HighPrice,
                                null,
                                OrderSubmissionData[symbol]
                            )
                        )
                    }
                )
                .SelectMany(x => x)
                .ToArray();

        private static TestCaseData[] CustomMakerOrders =>
            Symbols
                .Select(symbol =>
                    new[]
                    {
                        new TestCaseData(
                            0.0002m,
                            0.0004m,
                            0.00012m,
                            0.0003m,
                            new LimitOrderTestParameters(symbol, HighPrice, LowPrice)
                        ),
                        new TestCaseData(
                            0.00016m,
                            0.0004m,
                            0.00012m,
                            0.0003m,
                            new LimitOrderTestParameters(
                                symbol,
                                HighPrice,
                                LowPrice,
                                null,
                                OrderSubmissionData[symbol]
                            )
                        ),
                        new TestCaseData(
                            0.00014m,
                            0.00035m,
                            0.00012m,
                            0.0003m,
                            new LimitOrderTestParameters(
                                symbol,
                                HighPrice,
                                LowPrice,
                                new BinanceOrderProperties()
                            )
                        ),
                        new TestCaseData(
                            0.00012m,
                            0.00032m,
                            0.00012m,
                            0.0003m,
                            new LimitOrderTestParameters(
                                symbol,
                                LowPrice,
                                HighPrice,
                                new BinanceOrderProperties() { PostOnly = true },
                                OrderSubmissionData[symbol]
                            )
                        ),
                        new TestCaseData(
                            0.0001m,
                            0.0003m,
                            0.0001m,
                            0.0003m,
                            new LimitOrderTestParameters(
                                symbol,
                                HighPrice,
                                LowPrice,
                                new BinanceOrderProperties() { PostOnly = true }
                            )
                        )
                    }
                )
                .SelectMany(x => x)
                .ToArray();

        private static TestCaseData[] CustomTakerOrders =>
            Symbols
                .Select(symbol =>
                    new[]
                    {
                        new TestCaseData(
                            0.00016m,
                            0.0004m,
                            0.00012m,
                            0.0003m,
                            new MarketOrderTestParameters(symbol)
                        ),
                        new TestCaseData(
                            0.00014m,
                            0.00035m,
                            0.00012m,
                            0.0003m,
                            new MarketOrderTestParameters(
                                symbol,
                                new BinanceOrderProperties { PostOnly = true }
                            )
                        ),
                        new TestCaseData(
                            0.00012m,
                            0.00032m,
                            0.00012m,
                            0.0003m,
                            new LimitOrderTestParameters(
                                symbol,
                                LowPrice,
                                HighPrice,
                                null,
                                OrderSubmissionData[symbol]
                            )
                        )
                    }
                )
                .SelectMany(x => x)
                .ToArray();
    }
}
