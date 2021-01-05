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

using QuantConnect.Brokerages.GDAX;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    public class GDAXTestsHelpers
    {
        private static readonly Symbol Btcusd = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);

        public static Security GetSecurity(decimal price = 1m, SecurityType securityType = SecurityType.Crypto, Resolution resolution = Resolution.Minute)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                CreateConfig(securityType, resolution),
                new Cash(Currencies.USD, 1000, price),
                new SymbolProperties("BTCUSD", Currencies.USD, 1, 1, 0.01m, string.Empty),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private static SubscriptionDataConfig CreateConfig(SecurityType securityType = SecurityType.Crypto, Resolution resolution = Resolution.Minute)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbol.Create("BTCUSD", securityType, Market.GDAX), resolution, TimeZones.Utc, TimeZones.Utc,
            false, true, false);
        }

        public static void AddOrder(GDAXBrokerage unit, int id, string brokerId, decimal quantity)
        {
            var order = new Orders.MarketOrder { BrokerId = new List<string> { brokerId }, Symbol = Btcusd, Quantity = quantity, Id = id };
            order.PriceCurrency = Currencies.USD;
            unit.CachedOrderIDs.TryAdd(1, order);
            unit.FillSplit.TryAdd(id, new GDAXFill(order));
        }

        public static WebSocketMessage GetArgs(string json)
        {
            return new WebSocketMessage(json);
        }
    }
}
