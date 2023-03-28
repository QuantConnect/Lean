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
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class SignalExportTargetTests
    {
        [Test]
        public void SendsTargetsToCollective2Appropiately()
        {
            var symbols = new List<Symbol>()
            {
                Symbols.SPY,
                Symbols.EURUSD,
                Symbols.ES_Future_Chain,
                Symbols.SPY_Option_Chain
            };

            var targetList = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SPY, (decimal)0.2),
                new PortfolioTarget(Symbols.EURUSD, (decimal)0.3),
                new PortfolioTarget(Symbols.ES_Future_Chain, (decimal)0.2),
                new PortfolioTarget(Symbols.SPY_Option_Chain, (decimal)0.3)
            };

            var securityManager = CreateSecurityManager(symbols);
            var transactionManager = new SecurityTransactionManager(null, securityManager);
            var portfolio = new SecurityPortfolioManager(securityManager, transactionManager);
            portfolio.SetCash(50000);
            var algorithm = new QCAlgorithm
            {
                Portfolio = portfolio,
                Securities = securityManager
            };

            var manager = new Collective2SignalExportHandler("", 0);

            var message = manager.GetMessageSent(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm});

            var expectedMessage = @"{""positions"":[{""symbol"":""SPY"",""typeofsymbol"":""stock"",""quant"":99},{""symbol"":""EURUSD"",""typeofsymbol"":""forex"",""quant"":149},{""symbol"":""ES 1S1"",""typeofsymbol"":""future"",""quant"":99},{""symbol"":""SPY 2U|SPY R735QTJ8XC9X"",""typeofsymbol"":""option"",""quant"":149}],""systemid"":0,""apikey"":""""}";

            Assert.AreEqual(expectedMessage, message);
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

            var securityManager = CreateSecurityManager(symbols);
            var transactionManager = new SecurityTransactionManager(null, securityManager);
            var portfolio = new SecurityPortfolioManager(securityManager, transactionManager);
            portfolio.SetCash(50000);
            var algorithm = new QCAlgorithm
            {
                Portfolio = portfolio,
                Securities = securityManager
            };

            var manager = new Collective2SignalExportHandler("", 0);

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
                Symbols.SPX,
                Symbols.AAPL,
                Symbols.CAT
            };

            var targetList = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SPY, (decimal)0.2),
                new PortfolioTarget(Symbols.SPX, (decimal)0.8)
            };

            var securityManager = CreateSecurityManager(symbols);
            var manager = new CrunchDAOSignalExportHandler("", "");
            var algorithm = new QCAlgorithm();
            algorithm.Securities = securityManager;

            var message = manager.GetMessageSent(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });
            var expectedMessage = "ticker,date,signal\nSPY,2016-02-16,0.2\nSPX 31,2016-02-16,0.8\n";

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

            var securityManager = CreateSecurityManager(symbols);
            var manager = new CrunchDAOSignalExport("", "");
            var algorithm = new QCAlgorithm();
            algorithm.Securities = securityManager;

            var result = manager.Send(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });
            Assert.IsFalse(result);
        }

        [Test]
        public void CrunchDAOSignalExportReturnsFalseWhenPortfolioTargetListIsEmpty()
        {
            var targetList = new List<PortfolioTarget>();
            var manager = new CrunchDAOSignalExport("", "");
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

            var manager = new NumeraiSignalExportHandler("", "", "");
            var algorithm = new QCAlgorithm();

            var message = manager.GetMessageSent(new SignalExportTargetParameters { Targets = targets, Algorithm = algorithm});
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

            var manager = new NumeraiSignalExport("", "", "");
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

            var manager = new NumeraiSignalExport("", "", "");
            var algorithm = new QCAlgorithm();
            var result = manager.Send(new SignalExportTargetParameters { Targets = targets, Algorithm = algorithm });
            Assert.IsFalse(result);
        }

        [Test]
        public void SignalExportManagerGetsCorrectPortfolioTargetArray()
        {
            var symbols = new List<Symbol>()
            {
                Symbols.SPY,
                Symbols.AAPL
            };

            var algorithm = new AlgorithmStub(true);
            algorithm.SetFinishedWarmingUp();
            algorithm.SetCash(100000);

            var expectedPortfolioTargets = new Dictionary<string, string>();

            var spy = algorithm.AddSecurity(SecurityType.Equity, symbols[0].Value);
            spy.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), spy.Symbol, 10.0001m, 10.0001m));
            spy.Holdings.SetHoldings(10.00000000m, 99900);
            expectedPortfolioTargets.Add("SPY", "0.4540913218970736629667003027");

            var aapl = algorithm.AddSecurity(SecurityType.Equity, symbols[1].Value);
            aapl.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), aapl.Symbol, 10.0001m, 10.0001m));
            aapl.Holdings.SetHoldings(10.00000000m, 100);
            expectedPortfolioTargets.Add("AAPL", "0.0004545458677648385014681685");


            var signalExportManagerHandler = new SignalExportManagerHandler();
            var result = signalExportManagerHandler.GetPortfolioTargets(algorithm, out PortfolioTarget[] portfolioTargets);

            Assert.IsTrue(result);

            foreach (var target in portfolioTargets)
            {
                Assert.AreEqual(expectedPortfolioTargets[target.Symbol.Value], target.Quantity.ToString());
            }
        }

        [Test]
        public void SignalExportManagerReturnsFalseWhenNegativeTotalPortfolioValue()
        {
            var algorithm = new AlgorithmStub(true);
            algorithm.SetFinishedWarmingUp();
            algorithm.SetCash(-100000);

            var signalExportManagerHandler = new SignalExportManagerHandler();
            Assert.IsFalse(signalExportManagerHandler.GetPortfolioTargets(algorithm, out _));
        }

        private static SecurityManager CreateSecurityManager(List<Symbol> symbols)
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            var timeKeeper = new TimeKeeper(reference);
            var securityManager = new SecurityManager(timeKeeper);

            foreach (var symbol in symbols)
            {
                var security = CreateSecurity(symbol);
                securityManager.Add(security);
            }

            return securityManager;
        }

        private static Security CreateSecurity(Symbol symbol)
        {
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                CreateTradeBarConfig(symbol),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            security.SetMarketPrice(new Tick { Value = 100 });
            return security;
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
        private class SignalExportManagerHandler: SignalExportManager
        {
            /// <summary>
            /// Handler method to obtain portfolio targets from the given algorithm's portfolio
            /// </summary>
            /// <param name="algorithm">Algorithm being ran</param>
            /// <param name="targets">An array of portfolio targets from the algorithm's Portfolio</param>
            /// <returns>True if TotalPortfolioValue was bigger than zero</returns>
            public bool GetPortfolioTargets(IAlgorithm algorithm, out PortfolioTarget[] targets)
            {
                return base.GetPortfolioTargets(algorithm, out targets);
            }
        }

        /// <summary>
        /// Handler class to test how Collective2SignalExport converts target percentage to number of shares. Additionally,
        /// to test the message sent to Collective2 API
        /// </summary>
        private class Collective2SignalExportHandler : Collective2SignalExport
        {
            public Collective2SignalExportHandler(string apiKey, int systemId, string platformId = null) : base(apiKey, systemId, platformId)
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
                VerifyTargets(parameters.Targets, DefaultAllowedSecurityTypes);
                var message = ConvertToCSVFormat(parameters);
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
                VerifyTargets(parameters.Targets, DefaultAllowedSecurityTypes);
                ConvertTargetsToNumerai(parameters.Targets, out string message);
                return message;
            }
        }
    }
}
