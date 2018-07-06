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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Using rolling windows for efficient storage of historical data; which automatically clears after a period of time.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="history and warm up" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="warm up" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="rolling windows" />
    public class RollingWindowAlgorithm : QCAlgorithm
    {
        private RollingWindow<TradeBar> _window;
        private RollingWindow<IndicatorDataPoint> _smaWin;
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013,10,1);  // Set Start Date
            SetEndDate(2013,11,1);    // Set End Date
            SetCash(100000);          // Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            AddEquity("SPY", Resolution.Daily);

            // Creates a Rolling Window indicator to keep the 2 TradeBar
            _window = new RollingWindow<TradeBar>(2);    // For other security types, use QuoteBar

            // Creates an indicator and adds to a rolling window when it is updated
            var sma = SMA("SPY", 5);
            sma.Updated += (sender, updated) => _smaWin.Add(updated);
            _smaWin = new RollingWindow<IndicatorDataPoint>(5);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            // Add SPY TradeBar in rollling window
            _window.Add(data["SPY"]);

            // Wait for windows to be ready.
            if (!_window.IsReady || !_smaWin.IsReady) return;

            var currBar = _window[0];                   // Current bar had index zero.
            var pastBar = _window[1];                   // Past bar has index one.
            Log($"Price: {pastBar.Time} -> {pastBar.Close} ... {currBar.Time} -> {currBar.Close}");

            var currSma = _smaWin[0];                   // Current SMA had index zero.
            var pastSma = _smaWin[_smaWin.Count - 1];   // Oldest SMA has index of window count minus 1.
            Log($"SMA: {pastSma.Time} -> {pastSma.Value} ... {currSma.Time} -> {currSma.Value}");

            if (!Portfolio.Invested && currSma > pastSma)
            {
                SetHoldings("SPY", 1);
            }
        }
    }
}