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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm has examples of how to add an equity indicating the <see cref="DataNormalizationMode"/>
    /// directly with the <see cref="QCAlgorithm.AddEquity"/> method instead of using the <see cref="Equity.SetDataNormalizationMode"/> method.
    /// </summary>
    public class SetEquityDataNormalizationModeOnAddEquity : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly DataNormalizationMode _spyNormalizationMode = DataNormalizationMode.Raw;
        private readonly DataNormalizationMode _googNormalizationMode = DataNormalizationMode.Adjusted;
        private readonly DataNormalizationMode _aaplNormalizationMode = DataNormalizationMode.TotalReturn;
        private Equity _spyEquity;
        private Equity _googEquity;
        private Equity _aaplEquity;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);

            _spyEquity = AddEquity("SPY", Resolution.Minute, dataNormalizationMode: _spyNormalizationMode);
            CheckEquityDataNormalizationMode(_spyEquity, _spyNormalizationMode);

            _googEquity = AddEquity("GOOG", Resolution.Minute, dataNormalizationMode: _googNormalizationMode);
            CheckEquityDataNormalizationMode(_googEquity, _googNormalizationMode);

            _aaplEquity = AddEquity("AAPL", Resolution.Minute, dataNormalizationMode: _aaplNormalizationMode);
            CheckEquityDataNormalizationMode(_aaplEquity, _aaplNormalizationMode);
        }

        public override void OnData(Slice slice)
        {
        }

        private void CheckEquityDataNormalizationMode(Equity equity, DataNormalizationMode expectedNormalizationMode)
        {
            var subscriptions = SubscriptionManager.Subscriptions.Where(x => x.Symbol == equity.Symbol);
            if (subscriptions.Any(x => x.DataNormalizationMode != expectedNormalizationMode))
            {
                throw new Exception($"Expected {equity.Symbol} to have data normalization mode {expectedNormalizationMode} but was {subscriptions.First().DataNormalizationMode}");
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
        public long DataPoints => 5227479;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Information Ratio", "5.176"},
            {"Tracking Error", "0.071"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
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
