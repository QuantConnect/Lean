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
using QuantConnect.Securities.Forex;
using QuantConnect.Tests.Common.Orders.Fees;
using System;

namespace QuantConnect.Tests.Brokerages.Exante
{
    [TestFixture]
    public class ExanteFeeModelTests
    {
        private static decimal HighPrice => 1000m;
        private static decimal LowPrice => 100m;

        private static decimal Quantity => 1m;
        private static Symbol Symbol => Symbols.SPY;

        private static OrderSubmissionData OrderSubmissionData =>
            new(Security.BidPrice, Security.AskPrice, (Security.BidPrice + Security.AskPrice) / 2);

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
                    new Cash("USD", 0, 1m),
                    SymbolProperties.GetDefault("USD"),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
                security.SetMarketPrice(new Tick(DateTime.UtcNow, Symbol, LowPrice, HighPrice));
                return security;
            }
        }

        [Test]
        public static void GetFeeModelTest()
        {
            var model = new ExanteBrokerageModel();
            Assert.IsInstanceOf<ExanteFeeModel>(model.GetFeeModel(Security));
        }

        private static TestCaseData[] MakerOrders => new[]
        {
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice)
                { OrderSubmissionData = OrderSubmissionData }),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new OrderProperties())),
            new TestCaseData(
                new LimitOrderTestParameters(Symbol, LowPrice, HighPrice,
                    new OrderProperties()) { OrderSubmissionData = OrderSubmissionData }),
            new TestCaseData(new LimitOrderTestParameters(Symbol, HighPrice, LowPrice, new OrderProperties()))
        };

        private static TestCaseData[] TakerOrders => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol)),
            new TestCaseData(new MarketOrderTestParameters(Symbol, new OrderProperties())),
            new TestCaseData(new LimitOrderTestParameters(Symbol, LowPrice, HighPrice)
                { OrderSubmissionData = OrderSubmissionData })
        };

        [Test]
        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnShortOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new ExanteFeeModel();

            var order = parameters.CreateShortOrder(Quantity);
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                ExanteFeeModel.MarketUsaRate * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnShortOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new ExanteFeeModel();

            var order = parameters.CreateShortOrder(Quantity);
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                ExanteFeeModel.MarketUsaRate * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(MakerOrders))]
        public void ReturnLongOrderMakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new ExanteFeeModel();

            var order = parameters.CreateLongOrder(Quantity);
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                ExanteFeeModel.MarketUsaRate * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [Test]
        [TestCaseSource(nameof(TakerOrders))]
        public void ReturnLongOrderTakerFees(OrderTestParameters parameters)
        {
            IFeeModel feeModel = new ExanteFeeModel();

            var order = parameters.CreateLongOrder(Quantity);
            var fee = feeModel.GetOrderFee(new OrderFeeParameters(Security, order));

            Assert.AreEqual(
                ExanteFeeModel.MarketUsaRate * Math.Abs(Quantity),
                fee.Value.Amount
            );
            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
        }

        [TestCase("EURUSD", 1000000, 1100000)]
        [TestCase("USDJPY", -1000000, 1000000)]
        [TestCase("EURGBP", 1000000, 1100000)]
        public void ForexFeeInUSDDoesNotDependOnTheAccountCurrency(string ticker, decimal quantity, decimal valueInUSD)
        {
            IFeeModel feeModel = new ExanteFeeModel();

            foreach (var accountCurrency in new[] { Currencies.USD, Currencies.EUR, Currencies.GBP, Currencies.JPY })
            {
                var security = InteractiveBrokersFeeModelTests.GetForex(ticker, accountCurrency);

                var fee = feeModel.GetOrderFee(
                    new OrderFeeParameters(security, new MarketOrder(security.Symbol, quantity, DateTime.UtcNow)));

                Assert.AreEqual(Currencies.USD, fee.Value.Currency, accountCurrency);
                Assert.That(fee.Value.Amount, Is.EqualTo(0.25m * valueInUSD).Within(0.0000001m), accountCurrency);
            }
        }

        [Test]
        public void ZeroRateForexFeeDoesNotNeedUSDInTheCashBook()
        {
            var cashBook = new CashBook { AccountCurrency = Currencies.EUR };
            cashBook.Add(Currencies.GBP, 0, 1.14m);
            var tz = TimeZones.NewYork;
            var security = new Forex(
                SecurityExchangeHours.AlwaysOpen(tz),
                cashBook[Currencies.GBP],
                cashBook[Currencies.EUR],
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURGBP, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("EURGBP", Currencies.GBP, 1, 0.00001m, 1, string.Empty),
                cashBook,
                RegisteredSecurityDataTypesProvider.Null
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 0.877m, 0.877m));

            var fee = new ExanteFeeModel(0).GetOrderFee(
                new OrderFeeParameters(security, new MarketOrder(security.Symbol, 25000, DateTime.UtcNow)));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0m, fee.Value.Amount);
            Assert.AreEqual(0m, cashBook.ConvertToAccountCurrency(fee.Value).Amount);
        }
    }
}
