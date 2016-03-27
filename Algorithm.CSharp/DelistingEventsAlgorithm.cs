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
 *
*/

using System;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Showcases the delisting event of QCAlgorithm
    /// </summary>
    /// <remarks>
    /// The data for this algorithm isn't in the github repo, so this will need to be run on the QC site
    /// </remarks>
    public class DelistingEventsAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2007, 05, 16);  //Set Start Date
            SetEndDate(2007, 05, 25);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "AAA", Resolution.Daily);
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Transactions.OrdersCount == 0)
            {
                SetHoldings("AAA", 1);
                Debug("Purchased Stock");
            }

            foreach (var kvp in data.Bars)
            {
                var symbol = kvp.Key;
                var tradeBar = kvp.Value;
                Console.WriteLine("OnData(Slice): {0}: {1}: {2}", Time, symbol, tradeBar.Close.ToString("0.00"));
            }

            // the slice can also contain delisting data: data.Delistings in a dictionary string->Delisting
        }

        public void OnData(Delistings data)
        {
            foreach (var kvp in data)
            {
                var symbol = kvp.Key;
                var delisting = kvp.Value;
                if (delisting.Type == DelistingType.Warning)
                {
                    Console.WriteLine("OnData(Delistings): {0}: {1} will be delisted at end of day today.", Time, symbol);
                }
                if (delisting.Type == DelistingType.Delisted)
                {
                    Console.WriteLine("OnData(Delistings): {0}: {1} has been delisted.", Time, symbol);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Console.WriteLine("OnOrderEvent(OrderEvent): {0}: {1}", Time, orderEvent);
        }
    }
}
