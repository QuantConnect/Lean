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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityCacheProviderTests : ISecurityProvider
    {
        private SecurityCacheProvider _cacheProvider;
        private Dictionary<Symbol, Security> _securities;
        
        [SetUp]
        public void Setup()
        {
            _securities = new Dictionary<Symbol, Security>();
            _cacheProvider = new SecurityCacheProvider(this);
        }

        [Test]
        public void InitialStateCase()
        {
            var cache = _cacheProvider.GetSecurityCache(Symbols.SPY);
            Assert.IsNotNull(cache);
        }

        [Test]
        public void ExistingCustom()
        {
            // add custom data
            var baseSymbol = Symbol.CreateBase(typeof(Quandl), Symbols.SPY, Market.USA);
            var baseCache = _cacheProvider.GetSecurityCache(baseSymbol);
            var baseSecurity = new Security(
                baseSymbol,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash("USA", 0m, 1m), 
                SymbolProperties.GetDefault("USA"),
                new IdentityCurrencyConverter("USA"), 
                RegisteredSecurityDataTypesProvider.Null,
                baseCache);
            _securities.Add(baseSymbol, baseSecurity);
            // add some data to its cache
            var dataToStore = new List<TradeBar>
            {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            baseCache.StoreData(dataToStore, typeof(TradeBar));

            // add the underlying of the custom data
            var underlyingCache = _cacheProvider.GetSecurityCache(Symbols.SPY);

            IReadOnlyList<BaseData> data;
            // we expect it to have the data for the custom data cache
            Assert.IsTrue(underlyingCache.TryGetValue(typeof(TradeBar), out data));
            Assert.AreEqual(dataToStore, data);

            // we add some data to the custom cache
            var newData = new List<Quandl> {new Quandl()};
            baseCache.StoreData(newData, typeof(Quandl));

            // the data should also be in the underlying cache
            Assert.AreEqual(newData, underlyingCache.GetAll<Quandl>());
        }

        [Test]
        public void ExistingUnderlying()
        {
            // add underlying
            var underlyingCache = _cacheProvider.GetSecurityCache(Symbols.SPY);
            var underlyingSecurity = new Security(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash("USA", 0m, 1m),
                SymbolProperties.GetDefault("USA"),
                new IdentityCurrencyConverter("USA"),
                RegisteredSecurityDataTypesProvider.Null,
                underlyingCache);
            _securities.Add(Symbols.SPY, underlyingSecurity);

            // add base using underlying
            var baseSymbol = Symbol.CreateBase(typeof(Quandl), Symbols.SPY, Market.USA);
            var baseCache = _cacheProvider.GetSecurityCache(baseSymbol);

            // we store data in the underlying cache and expect it to be available through the base cache too
            var dataToStore = new List<TradeBar> {
                new TradeBar(DateTime.UtcNow, Symbols.SPY, 10m, 20m, 5m, 15m, 10000)
            };
            underlyingCache.StoreData(dataToStore, typeof(TradeBar));

            IReadOnlyList<BaseData> data;
            Assert.IsTrue(baseCache.TryGetValue(typeof(TradeBar), out data));
            Assert.AreEqual(dataToStore, data);

            // we store data in the base cache and expect it to be available through the underlying cache too
            var newData = new List<Quandl> { new Quandl() };
            baseCache.StoreData(newData, typeof(Quandl));

            // the data should also be in the underlying cache
            Assert.AreEqual(newData, underlyingCache.GetAll<Quandl>());
        }

        public Security GetSecurity(Symbol symbol)
        {
            if (_securities.ContainsKey(symbol))
            {
                return _securities[symbol];
            }

            return null;
        }
    }
}
