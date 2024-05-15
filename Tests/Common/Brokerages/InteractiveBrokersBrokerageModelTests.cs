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

using Moq;
using NUnit.Framework;

using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Data;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Forex;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Securities.Cfd;

namespace QuantConnect.Tests.Common.Brokerages
{

    [TestFixture, Parallelizable(ParallelScope.All)]
    public class InteractiveBrokersBrokerageModelTests
    {
        private readonly InteractiveBrokersBrokerageModel _interactiveBrokersBrokerageModel = new InteractiveBrokersBrokerageModel();

        [TestCaseSource(nameof(GetUnsupportedOptions))]
        public void CannotSubmitOrder_IndexOptionExercise(Security security)
        {
            var order = new Mock<OptionExerciseOrder>();
            order.Setup(x => x.Type).Returns(OrderType.OptionExercise);

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order.Object, out var message);

            Assert.IsFalse(canSubmit, message.Message);
            Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
            Assert.AreEqual("NotSupported", message.Code);
            StringAssert.Contains("exercises for index and cash-settled options", message.Message);
        }

        [TestCaseSource(nameof(GetForexOrderTestCases))]
        public void CanSubmitOrder_ForexWithinAllowableOrderSize(Forex security, decimal quantity, bool shouldSubmit)
        {
            var order = new MarketOrder(security.Symbol, quantity, new DateTime(2023, 1, 20));

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.AreEqual(shouldSubmit, canSubmit);

            if (shouldSubmit)
            {
                Assert.IsNull(message);
            }
            else
            {
                Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
                Assert.AreEqual("OrderSizeLimit", message.Code);
                StringAssert.Contains("minimum and maximum limits for the allowable order size are", message.Message);
            }
        }

        [TestCase("SPY", SecurityType.Option)]
        [TestCase("SPX", SecurityType.IndexOption)]
        [TestCase("ES", SecurityType.FutureOption)]
        public void CannotSubmitMOCOrdersForOptions(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);
            if (securityType == SecurityType.FutureOption)
            {
                var underlyingFuture = Symbol.CreateFuture(
                QuantConnect.Securities.Futures.Indices.SP500EMini,
                Market.CME,
                new DateTime(2021, 3, 19));

                var futureOption = Symbol.CreateOption(underlyingFuture,
                    Market.CME,
                    OptionStyle.American,
                    OptionRight.Call,
                    2550m,
                    new DateTime(2021, 3, 19));

                security = new QuantConnect.Securities.FutureOption.FutureOption(
                    futureOption,
                    MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.CME, futureOption, futureOption.SecurityType),
                    new Cash("USD", 100000m, 1m),
                    new OptionSymbolProperties(string.Empty, "USD", 1m, 0.01m, 1m),
                    new CashBook(),
                    new RegisteredSecurityDataTypesProvider(),
                    new SecurityCache(),
                    null);
            }

            var order = new MarketOnCloseOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsFalse(result);
            var expectedMessage = "InteractiveBrokers does not support Market-on-Close orders for other security types different than Future and Equity.";
            Assert.AreEqual(expectedMessage, message.Message);
        }

        [TestCase("EURGBP", SecurityType.Forex)]
        [TestCase("DE10YBEUR", SecurityType.Cfd)]
        public void CannotSubmitMOCOrdersForForexAndCfd(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);

            var order = new MarketOnCloseOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsFalse(result);
            var expectedMessage = "InteractiveBrokers does not support Market-on-Close orders for other security types different than Future and Equity.";
            Assert.AreEqual(expectedMessage, message.Message);
        }

        [TestCase("EURGBP", SecurityType.Forex)]
        [TestCase("DE10YBEUR", SecurityType.Cfd)]
        [TestCase("ES", SecurityType.Future)]
        public void CannotSubmitMOOOrdersForForexCfdAndFutureOrders(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);

            var order = new MarketOnOpenOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsFalse(result);
            var expectedMessage = "InteractiveBrokers does not support Market-on-Open orders for other security types different than Option and Equity.";
            Assert.AreEqual(expectedMessage, message.Message);
        }

        [TestCase("SPY", SecurityType.Option)]
        [TestCase("SPY", SecurityType.Equity)]
        public void CanSubmitMOOOrdersForOptionAndEquity(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);

            var order = new MarketOnOpenOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsTrue(result);
        }

        [TestCase("ES", SecurityType.Future)]
        [TestCase("SPY", SecurityType.Equity)]
        public void CanSubmitMOCOrdersForFutureAndEquity(string ticker, SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            var security = algo.AddSecurity(securityType, ticker);

            var order = new MarketOnCloseOrder(security.Symbol, 1, DateTime.UtcNow);
            var result = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);
            Assert.IsTrue(result);
        }

        [TestCase(AccountType.Cash, 1)]
        [TestCase(AccountType.Margin, 10)]
        public void GetsCorrectLeverageForCfds(AccountType accounType, decimal expectedLeverage)
        {
            var brokerageModel = new InteractiveBrokersBrokerageModel(accounType);
            var security = new Cfd(Symbols.DE10YBEUR,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("USD", 0, 0),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            Assert.AreEqual(expectedLeverage, brokerageModel.GetLeverage(security));
        }

        [Test]
        public void CanSubmitCfdOrder()
        {
            var security = new Cfd(Symbols.DE10YBEUR,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("USD", 0, 0),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            var order = new MarketOrder(security.Symbol, 1, new DateTime(2023, 1, 20));

            var canSubmit = _interactiveBrokersBrokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.IsTrue(canSubmit);
        }

        private static List<Security> GetUnsupportedOptions()
        {
            // Index option
            var spxSymbol = Symbol.Create("SPX", SecurityType.IndexOption, Market.USA);
            var spx = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new SubscriptionDataConfig(typeof(TradeBar), spxSymbol, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false),
                new Cash("USD", 1000, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            //Cash settled option
            var vixSymbol = Symbol.Create("VIX", SecurityType.Option, Market.USA);
            var vix = new Option(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new SubscriptionDataConfig(typeof(TradeBar), vixSymbol, Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false),
                new Cash("USD", 1000, 1),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null);
            vix.ExerciseSettlement = SettlementType.Cash;

            return new() {spx, vix};
        }

        private static TestCaseData[] GetForexOrderTestCases()
        {
            return new[]
            {
                Tuple.Create("USDCAD", 25000m, 7000000m),
                Tuple.Create("AUDUSD", 25000m, 6000000m),
                Tuple.Create("CADUSD", 25000m, 6000000m),
                Tuple.Create("CHFUSD", 25000m, 6000000m),
                Tuple.Create("CNHUSD", 150000m, 40000000m),
                Tuple.Create("CZKUSD", 0m, 0m), // need market price in USD or EUR -- do later when we support
                Tuple.Create("DKKUSD", 150000m, 35000000m),
                Tuple.Create("EURUSD", 20000m, 6000000m),
                Tuple.Create("GBPUSD", 20000m, 5000000m),
                Tuple.Create("HKDUSD", 200000m, 50000000m),
                Tuple.Create("HUFUSD", 0m, 0m), // need market price in USD or EUR -- do later when we support
                Tuple.Create("ILSUSD", 0m, 0m), // need market price in USD or EUR -- do later when we support
                Tuple.Create("KRWUSD", 0m, 200000000m),
                Tuple.Create("JPYUSD", 2500000m, 550000000m),
                Tuple.Create("MXNUSD", 300000m, 70000000m),
                Tuple.Create("NOKUSD", 150000m, 35000000m),
                Tuple.Create("NZDUSD", 35000m, 8000000m),
                Tuple.Create("PLNUSD", 0m, 0m), // need market price in USD or EUR -- do later when we support
                Tuple.Create("RUBUSD", 750000m, 30000000m),
                Tuple.Create("SEKUSD", 175000m, 40000000m),
                Tuple.Create("SGDUSD", 35000m, 8000000m),
                Tuple.Create("ZARUSD", 350000m, 100000000m),
                Tuple.Create("INRUSD", 0m, 0m) // not in the limits dictionary, should always return false
            }
            .Select(x =>
            {
                var currencyPair = x.Item1;
                Forex.DecomposeCurrencyPair(currencyPair, out var baseCurrency, out var quoteCurrency);
                var forexSymbol = Symbol.Create(currencyPair, SecurityType.Forex, Market.USA);
                var forex = new Forex(
                    forexSymbol,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    new Cash(quoteCurrency, 0, 0.7m),
                    new Cash(baseCurrency, 0, 1),
                    SymbolProperties.GetDefault(quoteCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new ForexCache());

                var min = x.Item2;
                var max = x.Item3;

                if (min != 0m || max != 0)
                {
                    if (min == 0m)
                    {
                        return new[]
                        {
                            // buy
                            new TestCaseData(forex, min, false),
                            new TestCaseData(forex, max * 1.001m, false),
                            new TestCaseData(forex, 0.001m, true),
                            new TestCaseData(forex, max, true),
                            new TestCaseData(forex, max / 2, true),
                            // sell
                            new TestCaseData(forex, -max * 1.001m, false),
                            new TestCaseData(forex, -0.001m, true),
                            new TestCaseData(forex, -max, true),
                            new TestCaseData(forex, -max / 2, true)
                        };
                    }

                    return new[]
                    {
                        // buy
                        new TestCaseData(forex, min * 0.999m, false),
                        new TestCaseData(forex, max * 1.001m, false),
                        new TestCaseData(forex, min, true),
                        new TestCaseData(forex, max, true),
                        new TestCaseData(forex, (min + max) / 2, true),
                        // sell
                        new TestCaseData(forex, -min * 0.999m, false),
                        new TestCaseData(forex, -max * 1.001m, false),
                        new TestCaseData(forex, -min, true),
                        new TestCaseData(forex, -max, true),
                        new TestCaseData(forex, -(min + max) / 2, true)
                    };
                }

                // min and max are 0, need market price in USD or EUR, we don't support yet
                return new[] { new TestCaseData(forex, 100000m, false) };
            })
            .SelectMany(x => x)
            .ToArray();
        }
    }
}
