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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for fractional forex pair
    /// </summary>
    public class FractionalQuantityRegressionAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2015, 11, 12);
            SetEndDate(2016, 04, 01);

            //Set the cash for the strategy:
            SetCash(100000);
            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            SetTimeZone(NodaTime.DateTimeZone.Utc);
            var security = AddSecurity(SecurityType.Crypto, "BTCUSD", Resolution.Daily, Market.GDAX, false, 3.3m, true);
            var con = new QuoteBarConsolidator(1);
            SubscriptionManager.AddConsolidator("BTCUSD", con);
            con.DataConsolidated += DataConsolidated;
            SetBenchmark(security.Symbol);
        }

        private void DataConsolidated(object sender, QuoteBar e)
        {
            var quantity = Math.Truncate((Portfolio.Cash + Portfolio.TotalFees) / Math.Abs(e.Value + 1));
            if (!Portfolio.Invested)
            {
                Order("BTCUSD", quantity);
            }
            else if (Portfolio["BTCUSD"].Quantity == quantity)
            {
                Order("BTCUSD", 0.1);
            }
            else if (Portfolio["BTCUSD"].Quantity == quantity + 0.1m)
            {
                Order("BTCUSD", 0.01);
            }
            else if (Portfolio["BTCUSD"].Quantity == quantity + 0.11m)
            {
                Order("BTCUSD", -0.02);
            }
            else if (Portfolio["BTCUSD"].Quantity == quantity + 0.09m)
            {
                //should fail
                Order("BTCUSD", 0.001);

                SetHoldings("BTCUSD", -2.0m);
                SetHoldings("BTCUSD", 2.0m);
                Quit();
            }
        }
    }
}