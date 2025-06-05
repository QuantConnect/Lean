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
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Provides a test implementation of a security provider
    /// </summary>
    public class SecurityProvider : ISecurityProvider
    {
        private readonly OrderProvider _orderProvider;
        private readonly BrokerageName _brokerageName;
        private readonly Dictionary<Symbol, Security> _securities;

        public SecurityProvider(Dictionary<Symbol, Security> securities, BrokerageName brokerageName, OrderProvider orderProvider)
        {
            _orderProvider = orderProvider;
            _brokerageName = brokerageName;
            _securities = securities;
        }

        public SecurityProvider(Dictionary<Symbol, Security> securities) : this(securities, BrokerageName.Default, null)
        {
        }

        public SecurityProvider() : this(new Dictionary<Symbol, Security>())
        {
        }

        public Security this[Symbol symbol]
        {
            get { return GetSecurity(symbol); }
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

        private Security CreateSecurity(Symbol symbol)
        {
            var symbolProperties = SymbolProperties.GetDefault(Currencies.USD);
            var quoteCurrency = new Cash(Currencies.USD, 0, 1m);
            try
            {
                var spdb = SymbolPropertiesDatabase.FromDataFolder();
                symbolProperties = spdb.GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, Currencies.USD);
                quoteCurrency = new Cash(symbolProperties.QuoteCurrency, 0, 1m);
            }
            catch (Exception ex)
            {
                // shouldn't happen
                QuantConnect.Logging.Log.Error(ex);
            }

            var security = new Security(
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
                quoteCurrency,
                symbolProperties,
                new CashBook(),
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            try
            {
                if (_orderProvider != null)
                {
                    var brokerageModel = BrokerageModel.Create(_orderProvider, _brokerageName, AccountType.Margin);
                    security.FeeModel = brokerageModel.GetFeeModel(security);
                }
            }
            catch (Exception ex)
            {
                // shouldn't happen
                QuantConnect.Logging.Log.Error(ex);
            }

            _securities[symbol] = security;
            return security;
        }
    }
}
