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

using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Tests.Engine.DataFeeds;
using static QuantConnect.Tests.Algorithm.Framework.Portfolio.PortfolioConstructionModelTests;
using NodaTime;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using System;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class Collective2SignalExportTests
    {
        [Test]
        public void SendsTargetsToCollective2Appropiately()
        {
            var reference = new DateTime(2016, 02, 16, 11, 53, 30);
            var symbols = new List<Symbol>()
            {
                Symbols.SPY,
                Symbols.EURUSD,
                Symbols.ES_Future_Chain,
                Symbols.SPY_Option_Chain
            };

            var securities = new List<Security>();
            var timeKeeper = new TimeKeeper(reference);
            var securityManager = new SecurityManager(timeKeeper);

            foreach (var symbol in symbols)
            {
                var security = CreateSecurity(reference, symbol);
                securities.Add(security);
                securityManager.Add(security);
            }

            var transactionManager = new SecurityTransactionManager(null, securityManager);
            var portfolio = new SecurityPortfolioManager(securityManager, transactionManager);
            portfolio.SetCash(50000);

            var targetList = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SPY, (decimal)0.2),
                new PortfolioTarget(Symbols.EURUSD, (decimal)0.3),
                new PortfolioTarget(Symbols.ES_Future_Chain, (decimal)0.2),
                new PortfolioTarget(Symbols.SPY_Option_Chain, (decimal)0.3)
            };

            Collective2SignalExport manager = new Collective2SignalExport("fnmzppYk0HO8YTrMRCPA2MBa3mLna6frsMjAJab1SyA5lpfbhY", 143679411, portfolio);

            var message = manager.Send(targetList);

            var expectedMessage = @"{""positions"":[{""symbol"":""SPY R735QTJ8XC9X"",""typeofsymbol"":""stock"",""quant"":1000000},{""symbol"":""EURUSD 8G"",""typeofsymbol"":""forex"",""quant"":1500000},{""symbol"":""ES 1S1"",""typeofsymbol"":""future"",""quant"":1000000},{""symbol"":""SPY 2U|SPY R735QTJ8XC9X"",""typeofsymbol"":""option"",""quant"":1500000}],""systemid"":143679411,""apikey"":""fnmzppYk0HO8YTrMRCPA2MBa3mLna6frsMjAJab1SyA5lpfbhY""}";

            Assert.AreEqual(expectedMessage, message);
        }

        private Security CreateSecurity(DateTime reference,
            Symbol symbol,
            string accountCurrency = "USD")
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

        private static SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
