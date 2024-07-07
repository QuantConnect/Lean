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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating CFD asset types and requesting history.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="cfd" />
    public class BasicTemplateCfdAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetAccountCurrency("EUR");

            SetStartDate(2019, 2, 20);
            SetEndDate(2019, 2, 21);
            SetCash("EUR", 100000);

            _symbol = AddCfd("DE30EUR").Symbol;

            // Historical Data
            var history = History(_symbol, 60, Resolution.Daily);
            Log($"Received {history.Count()} bars from CFD historical data call.");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            // Access Data
            if (slice.QuoteBars.ContainsKey(_symbol))
            {
                var quoteBar = slice.QuoteBars[_symbol];
                Log($"{quoteBar.EndTime} :: {quoteBar.Close}");
            }

            if (!Portfolio.Invested)
                SetHoldings(_symbol, 1);
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{Time} {orderEvent.ToString()}");
        }
    }
}
