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
        public void ConvertsPortfolioTargetsToCollective2Appropiately()
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

            var manager = new Collective2SignalExportTestHandler("", 0, portfolio);
            var transformedList = manager.ConvertHoldingsToCollective2TestHandler(targetList);
            var expectedTransformedList = new List<Collective2SignalExport.Collective2Position>()
            {
                new Collective2SignalExport.Collective2Position
                {
                    symbol = "SPY R735QTJ8XC9X",
                    typeofsymbol = "stock",
                    quant = 1000000
                },

                new Collective2SignalExport.Collective2Position
                {
                    symbol = "EURUSD 8G",
                    typeofsymbol = "forex",
                    quant = 1500000
                },

                new Collective2SignalExport.Collective2Position
                {
                    symbol = "ES 1S1",
                    typeofsymbol = "future",
                    quant = 1000000
                },

                new Collective2SignalExport.Collective2Position
                {
                    symbol = "SPY 2U|SPY R735QTJ8XC9X",
                    typeofsymbol = "option",
                    quant = 1500000
                }
            };

            for (var index = 0; index < expectedTransformedList.Count; index++)
            {
                Assert.AreEqual(expectedTransformedList[index].symbol, transformedList[index].symbol);
                Assert.AreEqual(expectedTransformedList[index].typeofsymbol, transformedList[index].typeofsymbol);
                Assert.AreEqual(expectedTransformedList[index].quant, transformedList[index].quant);
            }
        }

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

            var manager = new Collective2SignalExport("", 0, portfolio);

            var message = manager.Send(targetList);

            var expectedMessage = @"{""positions"":[{""symbol"":""SPY R735QTJ8XC9X"",""typeofsymbol"":""stock"",""quant"":1000000},{""symbol"":""EURUSD 8G"",""typeofsymbol"":""forex"",""quant"":1500000},{""symbol"":""ES 1S1"",""typeofsymbol"":""future"",""quant"":1000000},{""symbol"":""SPY 2U|SPY R735QTJ8XC9X"",""typeofsymbol"":""option"",""quant"":1500000}],""systemid"":0,""apikey"":""""}";

            Assert.AreEqual(expectedMessage, message);
        }

        [Test]
        public void ConvertsPortfolioTargetsToCrunchDAOAppropiately()
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
            var manager = new CrunchDAOSignalExport("", "", securityManager);

            var message = manager.Send(targetList);
            var expectedMessage = "ticker,date,signal\nSPY R735QTJ8XC9X,2016-02-16,0.2\nSPX 31,2016-02-16,0.8\n";

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

        /// <summary>
        /// Handler class to test Collective2SignalExport
        /// </summary>
        private class Collective2SignalExportTestHandler : Collective2SignalExport
        {
            public Collective2SignalExportTestHandler(string apiKey, int systemId, SecurityPortfolioManager portfolio, string platformId = null) : base(apiKey, systemId, portfolio, platformId)
            {
            }

            public List<Collective2Position> ConvertHoldingsToCollective2TestHandler(List<PortfolioTarget> holdings)
            {
                return base.ConvertHoldingsToCollective2(holdings);
            }
        }

        /// <summary>
        /// Handler class to test CrunchDAOSignalExport
        /// </summary>
        private class CrunchDAOSignalExportTestHandler : CrunchDAOSignalExport
        {
            public CrunchDAOSignalExportTestHandler(string apiKey, string model, SecurityManager securities, string submissionName = "", string comment = "") : base(apiKey, model, securities, submissionName, comment)
            {
            }

            public string ConvertToCSVFormatTestHandler(List<PortfolioTarget> holdings)
            {
                return base.ConvertToCSVFormat(holdings);
            }
        }
    }
}
