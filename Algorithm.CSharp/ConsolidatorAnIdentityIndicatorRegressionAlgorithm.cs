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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Security.Principal;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue #8017
    /// </summary>
    public class ConsolidatorAnIdentityIndicatorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Dictionary<DateTime, decimal> _expectedValues = new Dictionary<DateTime, decimal> {
            { new DateTime(2013, 10, 8),  144.75578537200m },
            { new DateTime(2013, 10, 9),  143.07840976800m },
            { new DateTime(2013, 10, 10), 143.15622616200m },
            { new DateTime(2013, 10, 11),  146.32940578400m }
        };
        private Identity _identity;
        private int _assertCount;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var symbol = AddEquity("SPY", Resolution.Minute).Symbol;
            Consolidate(symbol, Resolution.Daily, (TradeBar bar) =>
            {
                _assertCount++;
                if (_expectedValues[Time] != bar.Value)
                {
                    throw new Exception($"{Time} - Consolidate unexpected current value: {bar.Value}");
                }
            });
            _identity = Identity(symbol, Resolution.Daily);
            _identity.Updated += _identity_Updated;
            var min = MIN(symbol, 5, Resolution.Daily);
            min.Updated += Min_Updated;
        }

        private void _identity_Updated(object sender, IndicatorDataPoint updated)
        {
            _assertCount++;
            if (_expectedValues[Time] != _identity.Current.Value)
            {
                throw new Exception($"{Time} - _identity_Updated unexpected current value: {_identity.Current.Value}");
            }
        }

        private void Min_Updated(object sender, IndicatorDataPoint updated)
        {
            _assertCount++;
            if (_expectedValues[Time] != _identity.Current.Value)
            {
                throw new Exception($"{Time} - Min_Updated unexpected current value: {_identity.Current.Value}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_assertCount != 12)
            {
                throw new Exception($"IUnexpected assertiong count: {_assertCount}");
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
        public long DataPoints => 3943;

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
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
