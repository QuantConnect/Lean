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
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Custom data universe selection regression algorithm asserting it's behavior. See GH issue #6396
    /// </summary>
    public class NoUniverseSelectorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private SecurityChanges _changes = SecurityChanges.None;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 03, 31);

            UniverseSettings.Resolution = Resolution.Daily;
            AddUniverse<CoarseFundamental>();
        }

        public void OnData(Slice slice)
        {
            // if we have no changes, do nothing
            if (_changes == SecurityChanges.None) return;

            // liquidate removed securities
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            var activeAndWithDataSecurities = ActiveSecurities.Count(x => x.Value.HasData);
            // we want 1/N allocation in each security in our universe
            foreach (var security in _changes.AddedSecurities)
            {
                if (security.HasData)
                {
                    SetHoldings(security.Symbol, 1m / activeAndWithDataSecurities);
                }
            }

            _changes = SecurityChanges.None;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
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
        public long DataPoints => 42596;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "15"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-50.972%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98449.86"},
            {"Net Profit", "-1.550%"},
            {"Sharpe Ratio", "-4.375"},
            {"Sortino Ratio", "-3.048"},
            {"Probabilistic Sharpe Ratio", "2.821%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.766"},
            {"Beta", "0.896"},
            {"Annual Standard Deviation", "0.099"},
            {"Annual Variance", "0.01"},
            {"Information Ratio", "-12.019"},
            {"Tracking Error", "0.067"},
            {"Treynor Ratio", "-0.486"},
            {"Total Fees", "$17.93"},
            {"Estimated Strategy Capacity", "$220000.00"},
            {"Lowest Capacity Asset", "BNO UN3IMQ2JU1YD"},
            {"Portfolio Turnover", "14.29%"},
            {"OrderListHash", "f751fd0ba1203f81e6b40f0cb74d959f"}
        };
    }
}
