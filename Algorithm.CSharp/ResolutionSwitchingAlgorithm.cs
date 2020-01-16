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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Statistics;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Adds daily data, then switches over to minute data after a few days.
    /// This is to test the behavior of the sampling that occurs while the algorithm
    /// is executing and its final alignment to the benchmark series in the <see cref="StatisticsBuilder"/> class.
    /// </summary>
    /// <remarks>
    /// -=-=-= WARNING =-=-=-
    ///
    /// if you are a user of the platform looking for how to switch the resolution of a symbol, we recommend
    /// you add data in a high resolution (i.e. minute, second) and use a <see cref="TradeBarConsolidator"/> to aggregate the
    /// data to your desired resolution.
    ///
    /// This algorithm exists to test the internals of LEAN, and should not be used in any algorithm.
    ///
    /// -=-=-= WARNING =-=-=-
    /// </remarks>
    public class ResolutionSwitchingAlgorithm : QCAlgorithm
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            AddEquity("SPY", Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                MarketOrder(_spy, 651); // QTY 651 is equal to `SetHoldings(_spy, 1)`
                Debug("Purchased Stock");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.RemovedSecurities.Count > 0)
            {
                AddEquity("SPY", Resolution.Minute);
            }
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            if (UtcTime.Date == new DateTime(2013, 10, 9))
            {
                RemoveSecurity(symbol);
            }
        }
    }
}
