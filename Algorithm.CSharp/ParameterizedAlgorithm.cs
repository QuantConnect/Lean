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

using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Parameters;
using QuantConnect.Interfaces;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of the parameter system of QuantConnect. Using parameters you can pass the values required into C# algorithms for optimization.
    /// </summary>
    /// <meta name="tag" content="optimization" />
    /// <meta name="tag" content="using quantconnect" />
    public class ParameterizedAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // We place attributes on top of our fields or properties that should receive
        // their values from the job. The values 100 and 200 are just default values that
        // are only used if the parameters do not exist.
        [Parameter("ema-fast")]
        private int _fastPeriod = 100;

        [Parameter("ema-slow")]
        private int _slowPeriod = 200;

        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100*1000);

            AddSecurity(SecurityType.Equity, "SPY");

            _fast = EMA("SPY", _fastPeriod);
            _slow = EMA("SPY", _slowPeriod);
        }

        public override void OnData(Slice data)
        {
            // wait for our indicators to ready
            if (!_fast.IsReady || !_slow.IsReady) return;

            if (_fast > _slow*1.001m)
            {
                SetHoldings("SPY", 1);
            }
            else if (_fast < _slow*0.999m)
            {
                Liquidate("SPY");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 10;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "286.047%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101742.04"},
            {"Net Profit", "1.742%"},
            {"Sharpe Ratio", "23.023"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.266"},
            {"Beta", "0.356"},
            {"Annual Standard Deviation", "0.086"},
            {"Annual Variance", "0.007"},
            {"Information Ratio", "-0.044"},
            {"Tracking Error", "0.147"},
            {"Treynor Ratio", "5.531"},
            {"Total Fees", "$3.45"},
            {"Estimated Strategy Capacity", "$48000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.72%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "1fd15c0ef2042df5cd6e6d590000318e"}
        };
    }
}
