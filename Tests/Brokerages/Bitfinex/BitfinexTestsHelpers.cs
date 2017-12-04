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
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    public class BitfinexTestsHelpers
    {
        public static Security GetSecurity(decimal price = 1m, decimal quantity = 1.23m)
        {
            var fake = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), CreateConfig(), new Cash(CashBook.AccountCurrency, 1000, price),
                new SymbolProperties("BTCUSD", CashBook.AccountCurrency, 1, 1, 0.01m));
            fake.Holdings = new SecurityHolding(fake);
            fake.Holdings.SetHoldings(price, quantity);

            return fake;
        }

        public static SecurityManager CreateHoldings(decimal quantity)
        {
            var manager = new SecurityManager(new TimeKeeper(DateTime.UtcNow));
            manager.Add(GetSecurity(123, quantity));

            return manager;
        }

        private static SubscriptionDataConfig CreateConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex), Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false);
        }

        public static void AddOrder(BitfinexBrokerage unit, int id, string brokerId, int quantity)
        {
            var order = new Orders.MarketOrder { BrokerId = new List<string> { brokerId }, Quantity = quantity, Id = id, Symbol = GetSecurity().Symbol };
            unit.CachedOrderIDs.TryAdd(1, order);
            unit.FillSplit.TryAdd(id, new BitfinexFill(order));
        }
    }
}