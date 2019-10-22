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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class SecurityChangesTests
    {
        [Test]
        public void WillNotFilterCustomSecuritiesByDefault()
        {
            var security = new Security(Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            var customSecurity = new Security(Symbol.CreateBase(typeof(TradeBar), Symbols.SPY, QuantConnect.Market.USA),
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            var changes = new SecurityChanges(new List<Security> { security, customSecurity },
                new List<Security> { security, customSecurity });

            Assert.IsTrue(changes.AddedSecurities.Contains(customSecurity));
            Assert.IsTrue(changes.AddedSecurities.Contains(security));

            Assert.IsTrue(changes.RemovedSecurities.Contains(customSecurity));
            Assert.IsTrue(changes.RemovedSecurities.Contains(security));
        }

        [Test]
        public void FilterCustomSecuritiesIfDesired()
        {
            var security = new Security(Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            var customSecurity = new Security(Symbol.CreateBase(typeof(TradeBar), Symbols.SPY, QuantConnect.Market.USA),
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());

            var changes = new SecurityChanges(new List<Security> { security, customSecurity },
                new List<Security> { security, customSecurity });

            changes.FilterCustomSecurities = true;
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                Assert.AreNotEqual(SecurityType.Base, addedSecurity.Type);
            }

            foreach (var removedSecurity in changes.RemovedSecurities)
            {
                Assert.AreNotEqual(SecurityType.Base, removedSecurity.Type);
            }
        }
    }
}
