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

using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm of the Identity indicator with the filtering enhancement. Filtering is used to check
    /// the output of the indicator before returning it.
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    public class FilteredIdentityAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;
        private FilteredIdentity _identity;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 5, 2);  //Set Start Date
            SetEndDate(StartDate);     //Set End Date
            SetCash(100000);           //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            var security = AddForex("EURUSD", Resolution.Tick);

            _symbol = security.Symbol;
            _identity = FilteredIdentity(_symbol, filter: Filter);
        }

        /// <summary>
        /// Filter function: if data is a tick of TickType.Trade
        /// </summary>
        /// <param name="data">Data for applying the filter</param>
        /// <returns>True if we have TickType.Trade</returns>
        private bool Filter(IBaseData data)
        {
            var tick = data as Tick;
            if (tick != null)
            {
                return tick.TickType == TickType.Trade;
            }

            return true;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            // Since we are only accepting TickType.Trade,
            // this indicator will never be ready
            if (!_identity.IsReady) return;

            if (!Portfolio.Invested)
            {
                SetHoldings(_symbol, 1);
                Debug("Purchased Stock");
            }
        }
    }
}