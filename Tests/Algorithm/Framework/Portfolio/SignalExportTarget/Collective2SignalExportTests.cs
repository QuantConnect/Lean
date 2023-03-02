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
using QuantConnect.Securities;
using QuantConnect.Tests;
using QuantConnect.Tests.Engine;
using QuantConnect.Tests.Engine.DataFeeds;
using static QuantConnect.Tests.Algorithm.Framework.Portfolio.PortfolioConstructionModelTests;
using NodaTime;
using QuantConnect.Tests.Common.Data.UniverseSelection;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio.SignalExportTarget
{
    [TestFixture]
    public class Collective2SignalExportTests
    {
        [Test]
        public void SendsTargetsToCollective2Appropiately()
        {
            var portfolio = new TestPortfolioConstructionModel(time => time.AddDays(1));

            var spy = new Security(Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 1, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());

            portfolio.OnSecuritiesChanged(null, SecurityChangesTests.AddedNonInternal(spy));

            var eurusd = new Security(Symbols.EURUSD,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 1, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());

            portfolio.OnSecuritiesChanged(null, SecurityChangesTests.AddedNonInternal(eurusd));

            var es = new Security(Symbols.ES_Future_Chain,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 1, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());

            portfolio.OnSecuritiesChanged(null, SecurityChangesTests.AddedNonInternal(es));

            var spyOption = new Security(Symbols.SPY_Option_Chain,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 1, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache());

            portfolio.OnSecuritiesChanged(null, SecurityChangesTests.AddedNonInternal(spyOption));
            var holdings = new List<PortfolioTarget>()
            {
                new PortfolioTarget(Symbols.SPY, (decimal)0.2),
                new PortfolioTarget(Symbols.BTCEUR, (decimal)0.3),
                new PortfolioTarget(Symbols.ES_Future_Chain, (decimal)0.2),
                new PortfolioTarget(Symbols.SPY_Option_Chain, (decimal)0.3)
            };

            var expectedMessage = @"
 { ""positions"" : [
      {""symbol"" : ""SPY"",
         ""typeofsymbol"" : ""stock"",
         ""quant"" : -30
      },
      {
         ""symbol"" : ""@ESH6"",
         ""typeofsymbol"" : ""future"",
         ""quant"" : 1
      }
   ],
   ""systemid"" : 93059035,
   ""apikey"" : ""w8u0t3TIBRUk8eVSjD""
}";


        }
    }
}
