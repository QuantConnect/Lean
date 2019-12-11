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
using NUnit.Framework;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Tests.Algorithm.Framework
{
    [TestFixture]
    public class NotifiedSecurityChangesTests
    {
        [Test]
        public void UpdateCallsDisposeOnDisposableInstances()
        {
            var disposable = new Disposable(Symbols.SPY);
            var dictionary = new Dictionary<Symbol, Disposable>
            {
                [Symbols.SPY] = disposable
            };

            var SPY = new Equity(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("USD", 1m, 1m),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache()
            );
            var changes = SecurityChanges.Removed(SPY);
            NotifiedSecurityChanges.UpdateDictionary(dictionary, changes,
                security => security.Symbol,
                security => new Disposable(security.Symbol)
            );

            Assert.IsTrue(disposable.Disposed);
        }

        private class Disposable : IDisposable
        {
            public bool Disposed { get; private set; }
            public Symbol Symbol { get; private set; }

            public Disposable(Symbol symbol)
            {
                Symbol = symbol;
            }
            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
