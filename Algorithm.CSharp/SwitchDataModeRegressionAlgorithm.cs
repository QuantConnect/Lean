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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;


namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression test algorithm reproduces issue https://github.com/QuantConnect/Lean/issues/4031 
    /// fixed in PR https://github.com/QuantConnect/Lean/pull/4650
    /// Adjusted data have already been all loaded by the workers so DataNormalizationMode change has no effect in the data itself
    /// </summary>
    public class SwitchDataModeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "AAPL";

        private readonly Dictionary<DateTime, decimal?> _expectedCloseValues = new Dictionary<DateTime, decimal?>() {
            { new DateTime(2014, 6, 6, 9, 57, 0), 86.04398m},
            { new DateTime(2014, 6, 6, 9, 58, 0), 86.05196m},
            { new DateTime(2014, 6, 6, 9, 59, 0), 648.29m},
            { new DateTime(2014, 6, 6, 10, 0, 0), 647.86m},
            { new DateTime(2014, 6, 6, 10, 1, 0), 646.84m},
            { new DateTime(2014, 6, 6, 10, 2, 0), 647.64m},
            { new DateTime(2014, 6, 6, 10, 3, 0), 646.9m}
        };

        public override void Initialize()
        {
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 6);

            var aapl = AddEquity(UnderlyingTicker, Resolution.Minute);
        }

        public override void OnData(Slice data)
        {
            if (Time.Hour == 9 && Time.Minute == 58)
            {
                AddOption(UnderlyingTicker);
            }

            AssertValue(data);
        }

        public override void OnEndOfAlgorithm()
        {
            if (_expectedCloseValues.Count > 0)
            {
                throw new Exception($"Not all expected data points were received.");
            }
        }

        private void AssertValue(Slice data)
        {
            decimal? value;
            if (_expectedCloseValues.TryGetValue(data.Time, out value))
            {
                if (data.Bars.FirstOrDefault().Value?.Close.SmartRounding() != value)
                {
                    throw new Exception($"Expected tradebar price, expected {value} but was {data.Bars.First().Value.Close.SmartRounding()}");
                }

                _expectedCloseValues.Remove(data.Time);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "$0.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0"},
            {"Return Over Maximum Drawdown", "0"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
