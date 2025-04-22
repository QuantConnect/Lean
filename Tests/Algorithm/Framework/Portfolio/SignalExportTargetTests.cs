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
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Tests.Engine.DataFeeds;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class SignalExportTargetTests
    {
        [TestCaseSource(nameof(SendsTargetsToCollective2AppropiatelyTestCases))]
        public void SendsTargetsToCollective2Appropiately(string currency, Symbol symbol, decimal quantity, string expectedMessage)
        {
            var targetList = new List<PortfolioTarget>() { new(symbol, quantity) };

            var algorithm = new AlgorithmStub();
            algorithm.SetDateTime(new DateTime(2016, 02, 16, 11, 53, 30));
            algorithm.Portfolio.SetAccountCurrency(currency);
            var security = algorithm.AddSecurity(symbol);
            security.SetMarketPrice(new Tick { Value = 100 });

            algorithm.Portfolio.SetCash(50000);

            using var manager = new Collective2SignalExportHandler("", 0);

            var message = manager.GetMessageSent(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });

            Assert.AreEqual(expectedMessage, message);
        }

        [Test]
        public void Collective2SignalExportManagerDoesNotLogMoreThanOnceWhenUnknownMarket()
        {
            var targetList = new List<PortfolioTarget>() { new(Symbols.EURUSD, 1), new PortfolioTarget(Symbols.GBPUSD, 1) };

            var algorithm = new AlgorithmStub();
            algorithm.SetDateTime(new DateTime(2016, 02, 16, 11, 53, 30));
            algorithm.Portfolio.SetAccountCurrency("USD");
            var security = algorithm.AddSecurity(Symbols.EURUSD);
            var security2 = algorithm.AddSecurity(Symbols.GBPUSD);
            security.SetMarketPrice(new Tick { Value = 100 });
            security2.SetMarketPrice(new Tick { Value = 100 });

            algorithm.Portfolio.SetCash(50000);

            using var manager = new Collective2SignalExportHandler("", 0);

            for (int count = 0; count < 100; count++)
            {
                manager.GetMessageSent(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });
            }

            Assert.AreEqual(2, algorithm.DebugMessages.Count);
            var debugMessages = algorithm.DebugMessages.ToList();
            Assert.AreEqual($"The market of the symbol {Symbols.EURUSD.Value} was unexpected: {Symbols.EURUSD.ID.Market}. Using 'DEFAULT' as market", debugMessages[0]);
            Assert.AreEqual($"The market of the symbol {Symbols.GBPUSD.Value} was unexpected: {Symbols.GBPUSD.ID.Market}. Using 'DEFAULT' as market", debugMessages[1]);
        }

        [Test]
        public void Collective2SignalExportManagerDoesNotLogMoreThanOnceWhenUnknownSecurityType()
        {
            var targetList = new List<PortfolioTarget>() { new(Symbols.BTCUSD, 1), new PortfolioTarget(Symbols.XAUJPY, 1) };

            var algorithm = new AlgorithmStub();
            algorithm.SetDateTime(new DateTime(2016, 02, 16, 11, 53, 30));
            algorithm.Portfolio.SetAccountCurrency("USD");
            var security = algorithm.AddSecurity(Symbols.BTCUSD);
            var security2 = algorithm.AddSecurity(Symbols.XAUJPY);
            security.SetMarketPrice(new Tick { Value = 100 });
            security2.SetMarketPrice(new Tick { Value = 100 });

            algorithm.Portfolio.SetCash(50000);

            using var manager = new Collective2SignalExportHandler("", 0);

            for (int count = 0; count < 100; count++)
            {
                manager.GetMessageSent(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });
            }

            Assert.AreEqual(2, algorithm.DebugMessages.Count);
            var debugMessages = algorithm.DebugMessages.ToList();
            Assert.AreEqual($"Unexpected security type found: {security.Symbol.SecurityType}. Collective2 just accepts: Equity, Future, Option, Index Option and Stock", debugMessages[0]);
            Assert.AreEqual($"Unexpected security type found: {security2.Symbol.SecurityType}. Collective2 just accepts: Equity, Future, Option, Index Option and Stock", debugMessages[1]);
        }

        [Test]
        public void Collective2ConvertsPercentageToQuantityAppropiately()
        {
            var symbols = new List<Symbol>()
            {
                Symbols.SPY,
                Symbols.EURUSD,
                Symbols.AAPL,
                Symbols.IBM,
                Symbols.GOOG,
                Symbols.MSFT,
                Symbols.CAT
            };

            var targetList = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SPY, (decimal)0.01),
                new PortfolioTarget(Symbols.EURUSD, (decimal)0.99),
                new PortfolioTarget(Symbols.AAPL, (decimal)(-0.01)),
                new PortfolioTarget(Symbols.IBM, (decimal)(-0.99)),
                new PortfolioTarget(Symbols.GOOG, (decimal)0.0),
                new PortfolioTarget(Symbols.MSFT, (decimal)1.0),
                new PortfolioTarget(Symbols.CAT, (decimal)(-1.0))
            };

            var algorithm = new AlgorithmStub();
            AddSymbols(symbols, algorithm);
            algorithm.Portfolio.SetCash(50000);
            algorithm.Settings.MinimumOrderMarginPortfolioPercentage = 0;
            algorithm.Settings.FreePortfolioValue = 250;

            using var manager = new Collective2SignalExportHandler("", 0);

            var expectedQuantities = new Dictionary<string, int>()
            {
                { "SPY", 4 },
                { "EURUSD", 492},
                { "AAPL", -4 },
                { "IBM", -492 },
                { "GOOG", 0 },
                { "MSFT", 497 },
                { "CAT", -497 }
            };

            foreach (var target in targetList)
            {
                var quantity = manager.ConvertPercentageToQuantity(algorithm, target);
                Assert.AreEqual(expectedQuantities[target.Symbol.Value], quantity);
            }
        }

        [Test]
        public void SendsTargetsToCrunchDAOAppropiately()
        {
            var symbols = new List<Symbol>()
            {
                Symbols.SPY,
                Symbols.AAPL,
                Symbols.CAT
            };

            var targetList = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SPY, (decimal)0.2),
                new PortfolioTarget(Symbols.AAPL, (decimal)0.2),
                new PortfolioTarget(Symbols.CAT, (decimal)0.2),
            };

            var algorithm = new AlgorithmStub();
            AddSymbols(symbols, algorithm);
            using var manager = new CrunchDAOSignalExportHandler("", "");

            var message = manager.GetMessageSent(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });
            var expectedMessage = "ticker,date,signal\nSPY,2016-02-16,0.2\nAAPL,2016-02-16,0.2\nCAT,2016-02-16,0.2\n";

            Assert.AreEqual(expectedMessage, message);
        }

        [Test]
        public void CrunchDAOSignalExportReturnsFalseWhenSymbolIsNotAllowed()
        {
            var symbols = new List<Symbol>()
            {
                Symbols.ES_Future_Chain,
                Symbols.SPY_Option_Chain,
                Symbols.EURUSD,
                Symbols.BTCUSD,
            };

            var targetList = new List<PortfolioTarget>();

            foreach (var symbol in symbols)
            {
                targetList.Add(new PortfolioTarget(symbol, (decimal)0.1));
            }

            var algorithm = new AlgorithmStub();
            AddSymbols(symbols, algorithm);
            algorithm.Portfolio.SetCash(50000);
            using var manager = new CrunchDAOSignalExport("", "");

            var result = manager.Send(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });
            Assert.IsFalse(result);
        }

        [Test]
        public void CrunchDAOSignalExportReturnsFalseWhenPortfolioTargetListIsEmpty()
        {
            var targetList = new List<PortfolioTarget>();
            using var manager = new CrunchDAOSignalExport("", "");
            var algorithm = new QCAlgorithm();

            var result = manager.Send(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });
            Assert.IsFalse(result);
        }

        [Test]
        public void SendsTargetsToNumeraiAppropiately()
        {
            var targets = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SGX, (decimal)0.05),
                new PortfolioTarget(Symbols.AAPL, (decimal)0.1),
                new PortfolioTarget(Symbols.MSFT, (decimal)0.1),
                new PortfolioTarget(Symbols.ZNGA, (decimal)0.05),
                new PortfolioTarget(Symbols.FXE, (decimal)0.05),
                new PortfolioTarget(Symbols.LODE, (decimal)0.05),
                new PortfolioTarget(Symbols.IBM, (decimal)0.05),
                new PortfolioTarget(Symbols.GOOG, (decimal)0.1),
                new PortfolioTarget(Symbols.NFLX, (decimal)0.1),
                new PortfolioTarget(Symbols.CAT, (decimal)0.1)
            };

            using var manager = new NumeraiSignalExportHandler("", "", "");
            var algorithm = new QCAlgorithm();
            algorithm.SetDateTime(new DateTime(2023, 03, 03));

            var message = manager.GetMessageSent(new SignalExportTargetParameters { Targets = targets, Algorithm = algorithm });
            var expectedMessage = "numerai_ticker,signal\nSGX SP,0.05\nAAPL US,0.1\nMSFT US,0.1\nZNGA US,0.05\nFXE US,0.05\nLODE US,0.05\nIBM US,0.05\nGOOG US,0.1\nNFLX US,0.1\nCAT US,0.1\n";

            Assert.AreEqual(expectedMessage, message);
        }

        [Test]
        public void NumeraiReturnsFalseWhenSymbolIsNotAllowed()
        {
            var targets = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SGX, (decimal)0.05),
                new PortfolioTarget(Symbols.AAPL, (decimal)0.1),
                new PortfolioTarget(Symbols.MSFT, (decimal)0.1),
                new PortfolioTarget(Symbols.ZNGA, (decimal)0.05),
                new PortfolioTarget(Symbols.FXE, (decimal)0.05),
                new PortfolioTarget(Symbols.LODE, (decimal)0.05),
                new PortfolioTarget(Symbols.IBM, (decimal)0.05),
                new PortfolioTarget(Symbols.GOOG, (decimal)0.1),
                new PortfolioTarget(Symbols.NFLX, (decimal)0.1),
                new PortfolioTarget(Symbols.EURUSD, (decimal)0.1)
            };

            using var manager = new NumeraiSignalExport("", "", "");
            var algorithm = new QCAlgorithm();
            var result = manager.Send(new SignalExportTargetParameters { Targets = targets, Algorithm = algorithm });
            Assert.IsFalse(result);
        }

        [Test]
        public void NumeraiReturnsFalseWhenNumberOfTargetsIsLessThanTen()
        {
            var targets = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SGX, (decimal)0.05),
                new PortfolioTarget(Symbols.AAPL, (decimal)0.1),
                new PortfolioTarget(Symbols.MSFT, (decimal)0.1),
                new PortfolioTarget(Symbols.ZNGA, (decimal)0.05),
                new PortfolioTarget(Symbols.FXE, (decimal)0.05),
                new PortfolioTarget(Symbols.LODE, (decimal)0.05),
                new PortfolioTarget(Symbols.IBM, (decimal)0.05),
                new PortfolioTarget(Symbols.GOOG, (decimal)0.1),
                new PortfolioTarget(Symbols.NFLX, (decimal)0.1),
            };

            using var manager = new NumeraiSignalExport("", "", "");
            var algorithm = new QCAlgorithm();
            var result = manager.Send(new SignalExportTargetParameters { Targets = targets, Algorithm = algorithm });
            Assert.IsFalse(result);
        }

        [TestCase(SecurityType.Equity, "SPY", 68)]
        [TestCase(SecurityType.Equity, "AAPL", -68)]
        [TestCase(SecurityType.Equity, "GOOG", 345)]
        [TestCase(SecurityType.Equity, "IBM", 0)]
        [TestCase(SecurityType.Forex, "EURUSD", 90)]
        [TestCase(SecurityType.Forex, "EURUSD", -90)]
        [TestCase(SecurityType.Future, "ES", 4)]
        [TestCase(SecurityType.Future, "ES", -4)]

        public void SignalExportManagerGetsCorrectPortfolioTargetArray(SecurityType securityType, string ticker, int quantity)
        {
            var algorithm = new AlgorithmStub(true);
            algorithm.SetFinishedWarmingUp();
            algorithm.SetCash(100000);

            var security = algorithm.AddSecurity(securityType, ticker);
            security.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), security.Symbol, 144.80m, 144.82m));
            security.Holdings.SetHoldings(144.81m, quantity);

            var signalExportManagerHandler = new SignalExportManagerHandler(algorithm);
            var result = signalExportManagerHandler.GetPortfolioTargets(out PortfolioTarget[] portfolioTargets);

            Assert.IsTrue(result);
            var target = portfolioTargets[0];
            var targetQuantity = (int)PortfolioTarget.Percent(algorithm, target.Symbol, target.Quantity).Quantity;
            // The quantites can differ by one because of the number of lots for certain securities
            Assert.AreEqual(quantity, targetQuantity, 1);
        }

        [Test]
        public void SignalExportManagerDoesNotThrowOnZeroPrice()
        {
            var algorithm = new AlgorithmStub(true);
            algorithm.SetDateTime(new DateTime(2024, 02, 16, 11, 53, 30));

            var security = algorithm.AddSecurity(Symbols.SPY);
            // Set the market price to 0 to simulate the edge case being tested
            security.SetMarketPrice(new Tick { Value = 0 });

            using var manager = new Collective2SignalExportHandler("", 0);
            // Ensure ConvertPercentageToQuantity does not throw when price is 0
            Assert.DoesNotThrow(() =>
            {
                var result = manager.ConvertPercentageToQuantity(algorithm, new PortfolioTarget(Symbols.SPY, 0));
                Assert.AreEqual(0, result);
            });
        }

        [Test]
        public void SignalExportManagerHandlesIndexOptions()
        {
            var algorithm = new AlgorithmStub(true);
            algorithm.SetFinishedWarmingUp();
            algorithm.SetCash(100000);

            int quantity = 123;
            var underlying = algorithm.AddIndex("SPX", Resolution.Minute).Symbol;

            // Create the option contract (IndexOption) with specific parameters
            var option = Symbol.CreateOption(
                underlying,
                "SPXW",
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                3800m,
                new DateTime(2021, 1, 04));

            var security = algorithm.AddIndexOptionContract(option, Resolution.Minute);
            security.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), security.Symbol, 144.80m, 144.82m));
            security.Holdings.SetHoldings(144.81m, quantity);

            // Initialize the SignalExportManagerHandler and get portfolio targets
            var signalExportManagerHandler = new SignalExportManagerHandler(algorithm);
            var result = signalExportManagerHandler.GetPortfolioTargets(out PortfolioTarget[] portfolioTargets);

            // Assert that the result is successful
            Assert.IsTrue(result);

            // Get the portfolio target and verify the quantity matches
            var target = portfolioTargets[0];
            var targetQuantity = (int)PortfolioTarget.Percent(algorithm, target.Symbol, target.Quantity).Quantity;
            Assert.AreEqual(quantity, targetQuantity);

            // Ensure the symbol is of type IndexOption
            Assert.IsTrue(target.Symbol.SecurityType == SecurityType.IndexOption);
        }

        [Test]
        public void SignalExportManagerIgnoresIndexSecurities()
        {
            var algorithm = new AlgorithmStub(true);
            algorithm.SetFinishedWarmingUp();
            algorithm.SetCash(100000);

            var security = algorithm.AddIndexOption("SPX", "SPXW");
            security.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), security.Symbol, 144.80m, 144.82m));
            security.Holdings.SetHoldings(144.81m, 10);

            var signalExportManagerHandler = new SignalExportManagerHandler(algorithm);
            var result = signalExportManagerHandler.GetPortfolioTargets(out PortfolioTarget[] portfolioTargets);

            Assert.IsTrue(result);
            Assert.IsFalse(portfolioTargets.Where(x => x.Symbol.SecurityType == SecurityType.Index).Any());
        }

        [TestCaseSource(nameof(SignalExportManagerSkipsNonTradeableFuturesTestCase))]
        public void SignalExportManagerSkipsNonTradeableFutures(IEnumerable<Symbol> symbols, int expectedNumberOfTargets)
        {
            var algorithm = new AlgorithmStub(true);
            algorithm.SetFinishedWarmingUp();
            algorithm.SetCash(100000);

            foreach (var symbol in symbols)
            {
                var security = algorithm.AddSecurity(symbol);
                security.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), security.Symbol, 144.80m, 144.82m));
                security.Holdings.SetHoldings(144.81m, 100);
            }

            var signalExportManagerHandler = new SignalExportManagerHandler(algorithm);
            var result = signalExportManagerHandler.GetPortfolioTargets(out PortfolioTarget[] portfolioTargets);

            Assert.IsTrue(result);
            Assert.AreEqual(expectedNumberOfTargets, portfolioTargets.Length);
        }

        [Test]
        public void SignalExportManagerReturnsFalseWhenNegativeTotalPortfolioValue()
        {
            var algorithm = new AlgorithmStub(true);
            algorithm.SetFinishedWarmingUp();
            algorithm.SetCash(-100000);

            var signalExportManagerHandler = new SignalExportManagerHandler(algorithm);
            Assert.IsFalse(signalExportManagerHandler.GetPortfolioTargets(out _));
        }

        [Test]
        public void EmptySignalExportList()
        {   
            var algorithm = new AlgorithmStub(true);
            algorithm.SetLiveMode(true);
            algorithm.SetFinishedWarmingUp();

            algorithm.SetCash(100000);

            var security = algorithm.AddSecurity(Symbols.SPY);
            security.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), security.Symbol, 144.80m, 144.82m));
            security.Holdings.SetHoldings(144.81m, 100);
           
            var utcTime = DateTime.UtcNow;
            var signalExportManagerHandler = new SignalExportManagerHandler(algorithm);
            signalExportManagerHandler.OnOrderEvent(new OrderEvent(0, security.Symbol, utcTime.AddMinutes(-1), OrderStatus.Filled, OrderDirection.Buy, 100, 100, new Orders.Fees.OrderFee(new Securities.CashAmount(1, "USD"))));

            Assert.DoesNotThrow(() => signalExportManagerHandler.SetTargetPortfolioFromPortfolio());
            Assert.DoesNotThrow(() => signalExportManagerHandler.Flush(utcTime));
        }

        private static object[] SendsTargetsToCollective2AppropiatelyTestCases =
        {
            new object[] { "USD", Symbols.SPY, 0.2m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""SPY"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""CS"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":99.0}]}" },
            new object[] { "USD", Symbols.EURUSD, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""EUR/USD"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbols.EURGBP, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""EUR/GBP"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbols.GBPJPY, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""GBP/JPY"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbols.GBPUSD, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""GBP/USD"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbols.USDJPY, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""USD/JPY"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 192m, new DateTime(2016, 02, 15)), 0.2m, @"{""StrategyId"":0,""Positions"":[]}" },
            new object[] { "USD", Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 192m, new DateTime(2056, 02, 19)), 0.2m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""SPY"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""OPT"",""maturityMonthYear"":""20560218"",""putOrCall"":1,""strikePrice"":192.0,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 192m, new DateTime(2056, 02, 19)), 0.2m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""SPY"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""OPT"",""maturityMonthYear"":""20560218"",""putOrCall"":0,""strikePrice"":192.0,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM), 0.2m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""EUR/USD"",""currency"":""USD"",""securityExchange"":""FXCM"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.Create("NQX", SecurityType.IndexOption, Market.USA), 0.2m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""NQX"",""currency"":""USD"",""securityExchange"":""DEFAULT"",""securityType"":""OPT"",""maturityMonthYear"":null,""putOrCall"":1,""strikePrice"":0.0,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("NIFTY", Market.India, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""NIFTY"",""currency"":""USD"",""securityExchange"":""XNSE"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("HSI", Market.HKFE, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""HSI"",""currency"":""USD"",""securityExchange"":""XHKF"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("ZG", Market.NYSELIFFE, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""ZG"",""currency"":""USD"",""securityExchange"":""XNLI"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("FESX", Market.EUREX, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""FESX"",""currency"":""USD"",""securityExchange"":""XEUR"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("KC", Market.ICE, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""KC"",""currency"":""USD"",""securityExchange"":""IEPA"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("VIX", Market.CFE, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""VIX"",""currency"":""USD"",""securityExchange"":""XCBF"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("ZC", Market.CBOT, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""ZC"",""currency"":""USD"",""securityExchange"":""XCBT"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("GC", Market.COMEX, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""GC"",""currency"":""USD"",""securityExchange"":""XCEC"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("CL", Market.NYMEX, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""CL"",""currency"":""USD"",""securityExchange"":""XNYM"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "USD", Symbol.CreateFuture("NK", Market.SGX, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""NK"",""currency"":""USD"",""securityExchange"":""XSES"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.SPY, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""SPY"",""currency"":""EUR"",""securityExchange"":""DEFAULT"",""securityType"":""CS"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.EURUSD, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""EUR/USD"",""currency"":""EUR"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.EURGBP, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""EUR/GBP"",""currency"":""EUR"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.GBPJPY, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""GBP/JPY"",""currency"":""EUR"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.GBPUSD, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""GBP/USD"",""currency"":""EUR"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.USDJPY, 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""USD/JPY"",""currency"":""EUR"",""securityExchange"":""DEFAULT"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 192m, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""SPY"",""currency"":""EUR"",""securityExchange"":""DEFAULT"",""securityType"":""OPT"",""maturityMonthYear"":""20560218"",""putOrCall"":1,""strikePrice"":192.0,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.CreateOptionSymbol("SPY", OptionRight.Put, 192m, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""SPY"",""currency"":""EUR"",""securityExchange"":""DEFAULT"",""securityType"":""OPT"",""maturityMonthYear"":""20560218"",""putOrCall"":0,""strikePrice"":192.0,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 192m, new DateTime(2016, 02, 15)), 0.2m, @"{""StrategyId"":0,""Positions"":[]}" },
            new object[] { "EUR", Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""EUR/USD"",""currency"":""EUR"",""securityExchange"":""FXCM"",""securityType"":""FOR"",""maturityMonthYear"":null,""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("NIFTY", Market.India, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""NIFTY"",""currency"":""EUR"",""securityExchange"":""XNSE"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("HSI", Market.HKFE, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""HSI"",""currency"":""EUR"",""securityExchange"":""XHKF"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("ZG", Market.NYSELIFFE, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""ZG"",""currency"":""EUR"",""securityExchange"":""XNLI"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("FESX", Market.EUREX, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""FESX"",""currency"":""EUR"",""securityExchange"":""XEUR"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("KC", Market.ICE, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""KC"",""currency"":""EUR"",""securityExchange"":""IEPA"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("VIX", Market.CFE, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""VIX"",""currency"":""EUR"",""securityExchange"":""XCBF"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("ZC", Market.CBOT, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""ZC"",""currency"":""EUR"",""securityExchange"":""XCBT"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("GC", Market.COMEX, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""GC"",""currency"":""EUR"",""securityExchange"":""XCEC"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("CL", Market.NYMEX, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""CL"",""currency"":""EUR"",""securityExchange"":""XNYM"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
            new object[] { "EUR", Symbol.CreateFuture("NK", Market.SGX, new DateTime(2056, 02, 19)), 0m, @"{""StrategyId"":0,""Positions"":[{""exchangeSymbol"":{""symbol"":""NK"",""currency"":""EUR"",""securityExchange"":""XSES"",""securityType"":""FUT"",""maturityMonthYear"":""20560219"",""putOrCall"":null,""strikePrice"":null,""priceMultiplier"":1.0},""quantity"":0.0}]}" },
        };

        private static void AddSymbols(List<Symbol> symbols, QCAlgorithm algorithm)
        {
            algorithm.SetDateTime(new DateTime(2016, 02, 16, 11, 53, 30));

            foreach (var symbol in symbols)
            {
                var security = algorithm.AddSecurity(symbol);
                security.SetMarketPrice(new Tick { Value = 100 });
            }
        }

        /// <summary>
        /// Creates a TradebarConfiguration for the given symbol
        /// </summary>
        /// <param name="symbol">Symbol for which we want to create a TradeBarConfiguration</param>
        /// <returns>A new TradebarConfiguration for the given symbol</returns>
        private static SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }

        /// <summary>
        /// Handler class to test how SignalExportManager obtains the portfolio targets from the algorithm's portfolio
        /// </summary>
        private class SignalExportManagerHandler : SignalExportManager
        {
            public SignalExportManagerHandler(IAlgorithm algorithm) : base(algorithm)
            {

            }

            /// <summary>
            /// Handler method to obtain portfolio targets from algorithm's portfolio
            /// </summary>
            /// <param name="targets">An array of portfolio targets from the algorithm's Portfolio</param>
            /// <returns>True if TotalPortfolioValue was bigger than zero</returns>
            public bool GetPortfolioTargets(out PortfolioTarget[] targets)
            {
                return base.GetPortfolioTargets(out targets);
            }
        }

        /// <summary>
        /// Handler class to test how Collective2SignalExport converts target percentage to number of shares. Additionally,
        /// to test the message sent to Collective2 API
        /// </summary>
        private class Collective2SignalExportHandler : Collective2SignalExport
        {
            public Collective2SignalExportHandler(string apiKey, int systemId) : base(apiKey, systemId)
            {
            }

            /// <summary>
            /// Handler method to test how Collective2SignalExport converts target percentage to number of shares
            /// </summary>
            /// <param name="algorithm">Algorithm being ran</param>
            /// <param name="target">Target with quantity as percentage</param>
            /// <returns>Number of shares for the given target percentage</returns>
            public int ConvertPercentageToQuantity(IAlgorithm algorithm, PortfolioTarget target)
            {
                return base.ConvertPercentageToQuantity(algorithm, target);
            }

            /// <summary>
            /// Handler method to test the message sent to Collective2 API
            /// </summary>
            /// <param name="parameters">A list of holdings from the portfolio 
            /// expected to be sent to Collective2 API and the algorithm being ran</param>
            /// <returns>Message sent to Collective2 API</returns>
            public string GetMessageSent(SignalExportTargetParameters parameters)
            {
                ConvertHoldingsToCollective2(parameters, out List<Collective2Position> positions);
                var message = CreateMessage(positions);

                return message;
            }
        }

        /// <summary>
        /// Handler class to test the message sent to CrunchDAO API
        /// </summary>
        private class CrunchDAOSignalExportHandler : CrunchDAOSignalExport
        {
            public CrunchDAOSignalExportHandler(string apiKey, string model, string submissionName = "", string comment = "") : base(apiKey, model, submissionName, comment)
            {
            }

            /// <summary>
            /// Handler method to test the message sent to CrunchDAO API
            /// </summary>
            /// <param name="parameters">A list of holdings from the portfolio 
            /// expected to be sent to CrunchDAO2 API and the algorithm being ran</param>
            /// <returns>Message sent to CrunchDAO API</returns>
            public string GetMessageSent(SignalExportTargetParameters parameters)
            {
                ConvertToCSVFormat(parameters, out string message);
                return message;
            }
        }

        /// <summary>
        /// Handler class to test message sent to Numerai API
        /// </summary>
        private class NumeraiSignalExportHandler : NumeraiSignalExport
        {
            public NumeraiSignalExportHandler(string publicId, string secretId, string modelId, string fileName = "predictions.csv") : base(publicId, secretId, modelId, fileName)
            {
            }

            /// <summary>
            /// Handler method to test the message sent to Numerai API
            /// </summary>
            /// <param name="parameters">A list of holdings from the portfolio 
            /// expected to be sent to Numerai API and the algorithm being ran</param>
            /// <returns>Message sent to Numerai API</returns>
            public string GetMessageSent(SignalExportTargetParameters parameters)
            {
                ConvertTargetsToNumerai(parameters, out string message);
                return message;
            }
        }

        private static object[] SignalExportManagerSkipsNonTradeableFuturesTestCase =
        {
            new object[] { new List<Symbol>() { Symbols.AAPL, Symbols.SPY, Symbols.SPX }, 2 },
            new object[] { new List<Symbol>() { Symbols.AAPL, Symbols.SPY, Symbols.NFLX }, 3 },
        };
    }
}
