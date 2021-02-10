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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Report
{
    /// <summary>
    /// Provides a test implementation of a security provider
    /// </summary>
    public class ReportSecurityProvider : ISecurityProvider
    {
        private readonly Dictionary<Symbol, Security> _securities;

        public ReportSecurityProvider(Dictionary<Symbol, Security> securities)
        {
            _securities = securities;
        }

        public ReportSecurityProvider()
        {
            _securities = new Dictionary<Symbol, Security>();
        }

        public Security this[Symbol symbol]
        {
            get { return _securities[symbol]; }
            set { _securities[symbol] = value; }
        }

        public Security GetSecurity(Symbol symbol)
        {
            Security holding;
            _securities.TryGetValue(symbol, out holding);

            return holding ?? CreateSecurity(symbol);
        }

        public bool TryGetValue(Symbol symbol, out Security security)
        {
            return _securities.TryGetValue(symbol, out security);
        }

        internal static Security CreateSecurity(Symbol symbol)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    symbol,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
           );
        }
    }
}
