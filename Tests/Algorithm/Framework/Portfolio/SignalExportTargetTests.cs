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
using QuantConnect.Securities;
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

            var manager = new Collective2SignalExport("", 0);

            var message = manager.Send(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });

            var expectedMessage = @"{""positions"":[{""symbol"":""SPY R735QTJ8XC9X"",""typeofsymbol"":""stock"",""quant"":99},{""symbol"":""EURUSD 8G"",""typeofsymbol"":""forex"",""quant"":149},{""symbol"":""ES 1S1"",""typeofsymbol"":""future"",""quant"":99},{""symbol"":""SPY 2U|SPY R735QTJ8XC9X"",""typeofsymbol"":""option"",""quant"":149}],""systemid"":0,""apikey"":""""}";

            Assert.AreEqual(expectedMessage, message);
        }

        [Test]
        public void SendsTargetsToCrunchDAOAppropiately()
        {
            var symbols = new List<Symbol>()
            {
                Symbols.SPY,
                Symbols.SPX
            };

            var targetList = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SPY, (decimal)0.2),
                new PortfolioTarget(Symbols.SPX, (decimal)0.8)
            };

            var securityManager = CreateSecurityManager(symbols);
            var manager = new CrunchDAOSignalExport("", "");
            var algorithm = new QCAlgorithm();
            algorithm.Securities = securityManager;

            var message = manager.Send(new SignalExportTargetParameters { Targets = targetList, Algorithm = algorithm });
            var expectedMessage = "ticker,date,signal\nSPY R735QTJ8XC9X,2016-02-16,0.2\nSPX 31,2016-02-16,0.8\n";

            Assert.AreEqual(expectedMessage, message);
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

            var manager = new NumeraiSignalExport("", "", "");
            var algorithm = new QCAlgorithm();

            var message = manager.Send(new SignalExportTargetParameters { Targets = targets, Algorithm = algorithm});
            var expectedMessage = "numerai_ticker,signal\nSGX SP,0.05\nAAPL US,0.1\nMSFT US,0.1\nZNGA US,0.05\nFXE US,0.05\nLODE US,0.05\nIBM US,0.05\nGOOG US,0.1\nNFLX US,0.1\nCAT US,0.1\n";

            Assert.AreEqual(expectedMessage, message);
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
    }
}
