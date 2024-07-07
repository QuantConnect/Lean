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
            var security = new Security(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var customSecurity = new Security(
                Symbol.CreateBase(typeof(TradeBar), Symbols.SPY, QuantConnect.Market.USA),
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var changes = CreateNonInternal(
                new List<Security> { security, customSecurity },
                new List<Security> { security, customSecurity }
            );

            Assert.IsTrue(changes.AddedSecurities.Contains(customSecurity));
            Assert.IsTrue(changes.AddedSecurities.Contains(security));

            Assert.IsTrue(changes.RemovedSecurities.Contains(customSecurity));
            Assert.IsTrue(changes.RemovedSecurities.Contains(security));
        }

        [Test]
        public void FilterCustomSecuritiesIfDesired()
        {
            var security = new Security(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var customSecurity = new Security(
                Symbol.CreateBase(typeof(TradeBar), Symbols.SPY, QuantConnect.Market.USA),
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                new Cash(Currencies.USD, 0, 0),
                SymbolProperties.GetDefault(Currencies.USD),
                new IdentityCurrencyConverter(Currencies.USD),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var changes = CreateNonInternal(
                new List<Security> { security, customSecurity },
                new List<Security> { security, customSecurity }
            );

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

        /// <summary>
        /// Returns a new instance of <see cref="SecurityChanges"/> with the specified securities marked as added
        /// </summary>
        /// <param name="securities">The added securities</param>
        /// <remarks>Useful for testing</remarks>
        /// <returns>A new security changes instance with the specified securities marked as added</returns>
        public static SecurityChanges AddedNonInternal(params Security[] securities)
        {
            if (securities == null || securities.Length == 0)
                return SecurityChanges.None;
            return CreateNonInternal(securities, Enumerable.Empty<Security>());
        }

        /// <summary>
        /// Returns a new instance of <see cref="SecurityChanges"/> with the specified securities marked as removed
        /// </summary>
        /// <param name="securities">The removed securities</param>
        /// <remarks>Useful for testing</remarks>
        /// <returns>A new security changes instance with the specified securities marked as removed</returns>
        public static SecurityChanges RemovedNonInternal(params Security[] securities)
        {
            if (securities == null || securities.Length == 0)
                return SecurityChanges.None;
            return CreateNonInternal(Enumerable.Empty<Security>(), securities);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityChanges"/> class all none internal
        /// </summary>
        /// <param name="addedSecurities">Added symbols list</param>
        /// <param name="removedSecurities">Removed symbols list</param>
        /// <remarks>Useful for testing</remarks>
        public static SecurityChanges CreateNonInternal(
            IEnumerable<Security> addedSecurities,
            IEnumerable<Security> removedSecurities
        )
        {
            return SecurityChanges.Create(
                addedSecurities.ToList(),
                removedSecurities.ToList(),
                new List<Security>(),
                new List<Security>()
            );
        }
    }
}
