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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Constructs a displaced moving average ribbon and buys when all are lined up, liquidates when they all line down
    /// Ribbons are great for visualizing trends
    ///   Signals are generated when they all line up in a paricular direction
    ///     A buy signal is when the values of the indicators are increasing (from slowest to fastest).
    ///     A sell signal is when the values of the indicators are decreasing (from slowest to fastest).
    /// </summary>
    public class DisplacedMovingAverageRibbon : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private IndicatorBase<IndicatorDataPoint>[] _ribbon;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <meta name="tag" content="charting" />
        /// <meta name="tag" content="plotting indicators" />
        /// <seealso cref="QCAlgorithm.SetStartDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetEndDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public override void Initialize()
        {
            SetStartDate(2009, 01, 01);
            SetEndDate(2015, 01, 01);

            AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily);

            const int count = 6;
            const int offset = 5;
            const int period = 15;

            // define our sma as the base of the ribbon
            var sma = new SimpleMovingAverage(period);

            _ribbon = Enumerable.Range(0, count).Select(x =>
            {
                // define our offset to the zero sma, these various offsets will create our 'displaced' ribbon
                var delay = new Delay(offset*(x+1));

                // define an indicator that takes the output of the sma and pipes it into our delay indicator
                var delayedSma = delay.Of(sma);

                // register our new 'delayedSma' for automatic updates on a daily resolution
                RegisterIndicator(_spy, delayedSma, Resolution.Daily, data => data.Value);

                return delayedSma;
            }).ToArray();
        }

        private DateTime _previous;

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public override void OnData(Slice data)
        {
            // wait for our entire ribbon to be ready
            if (!_ribbon.All(x => x.IsReady)) return;

            // only once per day
            if (_previous.Date == Time.Date) return;

            Plot("Ribbon", "Price", data[_spy].Price);
            Plot("Ribbon", _ribbon);


            // check for a buy signal
            var values = _ribbon.Select(x => x.Current.Value).ToArray();

            var holding = Portfolio[_spy];
            if (holding.Quantity <= 0 && IsAscending(values))
            {
                SetHoldings(_spy, 1.0);
            }
            else if (holding.Quantity > 0 && IsDescending(values))
            {
                Liquidate(_spy);
            }

            _previous = Time;
        }

        /// <summary>
        /// Returns true if the specified values are in ascending order
        /// </summary>
        private bool IsAscending(IEnumerable<decimal> values)
        {
            decimal? last = null;
            foreach (var val in values)
            {
                if (last == null)
                {
                    last = val;
                    continue;
                }

                if (last.Value < val)
                {
                    return false;
                }
                last = val;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the specified values are in descending order
        /// </summary>
        private bool IsDescending(IEnumerable<decimal> values)
        {
            decimal? last = null;
            foreach (var val in values)
            {
                if (last == null)
                {
                    last = val;
                    continue;
                }

                if (last.Value > val)
                {
                    return false;
                }
                last = val;
            }
            return true;
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 12074;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "7"},
            {"Average Win", "19.17%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "16.729%"},
            {"Drawdown", "12.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "253075.04"},
            {"Net Profit", "153.075%"},
            {"Sharpe Ratio", "1.05"},
            {"Sortino Ratio", "1.078"},
            {"Probabilistic Sharpe Ratio", "56.405%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.051"},
            {"Beta", "0.507"},
            {"Annual Standard Deviation", "0.107"},
            {"Annual Variance", "0.011"},
            {"Information Ratio", "-0.083"},
            {"Tracking Error", "0.105"},
            {"Treynor Ratio", "0.221"},
            {"Total Fees", "$49.40"},
            {"Estimated Strategy Capacity", "$740000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.32%"},
            {"OrderListHash", "62d1401e91275f777d0865a13906e7c8"}
        };
    }
}
