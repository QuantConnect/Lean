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
        private readonly DataNormalizationMode _ibmNormalizationMode = DataNormalizationMode.Adjusted;
        private readonly DataNormalizationMode _aigNormalizationMode = DataNormalizationMode.TotalReturn;
        private Dictionary<Equity, Tuple<decimal, decimal>> _priceRanges = new Dictionary<Equity, Tuple<decimal, decimal>>();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);

            var spyEquity = AddEquity("SPY", Resolution.Minute, dataNormalizationMode: _spyNormalizationMode);
            CheckEquityDataNormalizationMode(spyEquity, _spyNormalizationMode);
            _priceRanges.Add(spyEquity, new Tuple<decimal, decimal>(167.28m, 168.37m));

            var ibmEquity = AddEquity("IBM", Resolution.Minute, dataNormalizationMode: _ibmNormalizationMode);
            CheckEquityDataNormalizationMode(ibmEquity, _ibmNormalizationMode);
            _priceRanges.Add(ibmEquity, new Tuple<decimal, decimal>(135.864131052m, 136.819606508m));

            var aigEquity = AddEquity("AIG", Resolution.Minute, dataNormalizationMode: _aigNormalizationMode);
            CheckEquityDataNormalizationMode(aigEquity, _aigNormalizationMode);
            _priceRanges.Add(aigEquity, new Tuple<decimal, decimal>(48.73m, 49.10m));
        }

        public override void OnData(Slice slice)
        {
            foreach (var kvp in _priceRanges)
            {
                var equity = kvp.Key;
                var minExpectedPrice = kvp.Value.Item1;
                var maxExpectedPrice = kvp.Value.Item2;

                if (equity.HasData && (equity.Price < minExpectedPrice || equity.Price > maxExpectedPrice))
                {
                    throw new Exception($"{equity.Symbol}: Price {equity.Price} is  out of expected range [{minExpectedPrice}, {maxExpectedPrice}]");
                }
            }
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
        public long DataPoints => 2355;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
