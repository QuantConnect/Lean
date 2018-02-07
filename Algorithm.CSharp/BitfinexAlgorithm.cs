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

using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using System;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{

    public class BitfinexAlgorithm : QCAlgorithm
    {
        private bool hasTraded;

        public override void Initialize()
        {
            SetStartDate(2015, 11, 12);
            SetEndDate(2016, 04, 01);

            //Set the cash for the strategy:
            SetCash(100000);
            SetBrokerageModel(BrokerageName.Bitfinex, AccountType.Margin);

            SetTimeZone(NodaTime.DateTimeZone.Utc);
            var security = AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Tick, Market.Bitfinex, false, 3.3m, true);
            AddCrypto("IOTBTC", Resolution.Tick, Market.Bitfinex, false, 3.3m);
            AddCrypto("IOTUSD", Resolution.Tick, Market.Bitfinex, false, 3.3m);
            SetBenchmark(security.Symbol);

            //var con = new QuoteBarConsolidator(new TimeSpan(0, 1, 0));
            //SubscriptionManager.AddConsolidator("BTCUSD", con);
            //con.DataConsolidated += DataConsolidated;
        }

        public override void OnData(Slice slice)
        {
            if (slice.Ticks.ContainsKey("IOTBTC"))
            {
                var tick = slice.Ticks["IOTBTC"].ToArray().First();
                if (!hasTraded)
                {
                    LimitOrder("IOTBTC", -6m, tick.AskPrice + 0.000001m);
                    hasTraded = true;
                }
                else
                {
                    foreach (var item in Transactions.GetOpenOrders())
                    {
                    }
                }

                // MarketOrder("IOTBTC", 7m);
            }
        }

        private void DataConsolidated(object sender, QuoteBar e)
        {
        }
    }
}