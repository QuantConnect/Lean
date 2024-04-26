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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of filtering tick data so easier to use. Tick data has lots of glitchy, spikey data which should be filtered out before usagee.
    /// </summary>
    /// <meta name="tag" content="filtering" />
    /// <meta name="tag" content="tick data" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="ticks event" />
    public class TickDataFilteringAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialize the tick filtering example algorithm
        /// </summary>
        public override void Initialize()
        {
            SetCash(25000);
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 07);
            var spy = AddEquity("SPY", Resolution.Tick);

            //Add our custom data filter.
            spy.SetDataFilter(new TickExchangeDataFilter(this));
        }

        /// <summary>
        /// Data arriving here will now be filtered.
        /// </summary>
        /// <param name="data">Ticks data array</param>
        public override void OnData(Slice data)
        {
            if (!data.ContainsKey("SPY")) return;
            var spyTickList = data["SPY"];

            //Ticks return a list of ticks this second
            foreach (var tick in spyTickList)
            {
                Debug(tick.Exchange);
            }

            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }
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
        public long DataPoints => 707410;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "25000"},
            {"End Equity", "25003.46"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "99.58%"},
            {"OrderListHash", "21ca432fd48d13ca3ee65ee494e035db"}
        };
    }

    /// <summary>
    /// Exchange filter class
    /// </summary>
    public class TickExchangeDataFilter : ISecurityDataFilter
    {
        private IAlgorithm _algo;

        /// <summary>
        /// Save instance of the algorithm namespace
        /// </summary>
        /// <param name="algo"></param>
        public TickExchangeDataFilter(IAlgorithm algo)
        {
            _algo = algo;
        }

        /// <summary>
        /// Filter out a tick from this vehicle, with this new data:
        /// </summary>
        /// <param name="data">New data packet:</param>
        /// <param name="asset">Vehicle of this filter.</param>
        public bool Filter(Security asset, BaseData data)
        {
            // TRUE -->  Accept Tick
            // FALSE --> Reject Tick
            var tick = data as Tick;

            // This is a tick bar
            if (tick != null)
            {
                if (tick.Exchange == Exchange.ARCA)
                {
                    return true;
                }
            }

            //Only allow those exchanges through.
            return false;
        }
    }
}
