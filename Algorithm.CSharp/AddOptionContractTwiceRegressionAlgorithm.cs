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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue #6073 where we remove and re add an option and expect it to work
    /// </summary>
    public class AddOptionContractTwiceRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contract;
        private bool _hasRemoved;
        private bool _reAdded;

        public override void Initialize()
        {
            SetStartDate(2014, 06, 06);
            SetEndDate(2014, 06, 09);

            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            UniverseSettings.MinimumTimeInUniverse = TimeSpan.Zero;
            UniverseSettings.FillForward = false;

            AddEquity("SPY", Resolution.Daily);

            var aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

            _contract = OptionChainProvider.GetOptionContractList(aapl, Time)
                .OrderBy(symbol => symbol.ID.Symbol)
                .FirstOrDefault(optionContract => optionContract.ID.OptionRight == OptionRight.Call
                    && optionContract.ID.OptionStyle == OptionStyle.American);
            AddOptionContract(_contract);
        }

        public override void OnData(Slice slice)
        {
            if (_hasRemoved)
            {
                if (!_reAdded && slice.ContainsKey(_contract) && slice.ContainsKey(_contract.Underlying))
                {
                    throw new Exception("Getting data for removed option and underlying!");
                }

                if (!Portfolio.Invested && _reAdded)
                {
                    var option = Securities[_contract];
                    var optionUnderlying = Securities[_contract.Underlying];
                    if (option.IsTradable && optionUnderlying.IsTradable
                        && slice.ContainsKey(_contract) && slice.ContainsKey(_contract.Underlying))
                    {
                        Buy(_contract, 1);
                    }
                }

                if (!Securities[_contract].IsTradable
                    && !Securities[_contract.Underlying].IsTradable
                    && !_reAdded)
                {
                    // ha changed my mind!
                    AddOptionContract(_contract);
                    _reAdded = true;
                }
            }

            if (slice.ContainsKey(_contract) && slice.ContainsKey(_contract.Underlying))
            {
                if (!_hasRemoved)
                {
                    RemoveOptionContract(_contract);
                    RemoveSecurity(_contract.Underlying);
                    _hasRemoved = true;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_hasRemoved)
            {
                throw new Exception("We did not remove the option contract!");
            }
            if (!_reAdded)
            {
                throw new Exception("We did not re add the option contract!");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 4677;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "-4.548%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99949"},
            {"Net Profit", "-0.051%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.486"},
            {"Tracking Error", "0.008"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$30000.00"},
            {"Lowest Capacity Asset", "AAPL VXBK4Q9ZIFD2|AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.07%"},
            {"OrderListHash", "c763192f852f447453941500d362dbf1"}
        };
    }
}
