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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for testing parameterized regression algorithms get valid parameters.
    /// </summary>
    public class GetParameterRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);

            CheckParameter((string)null, GetParameter("non-existing"), "GetParameter(\"non-existing\")");
            CheckParameter("100", GetParameter("non-existing", "100"), "GetParameter(\"non-existing\", \"100\")");
            CheckParameter(100, GetParameter("non-existing", 100), "GetParameter(\"non-existing\", 100)");
            CheckParameter(100d, GetParameter("non-existing", 100d), "GetParameter(\"non-existing\", 100d)");
            CheckParameter(100m, GetParameter("non-existing", 100m), "GetParameter(\"non-existing\", 100m)");

            CheckParameter("10", GetParameter("ema-fast"), "GetParameter(\"ema-fast\")");
            CheckParameter(10, GetParameter("ema-fast", 100), "GetParameter(\"ema-fast\", 100)");
            CheckParameter(10d, GetParameter("ema-fast", 100d), "GetParameter(\"ema-fast\", 100d)");
            CheckParameter(10m, GetParameter("ema-fast", 100m), "GetParameter(\"ema-fast\", 100m)");

            Quit();
        }

        private void CheckParameter<T, P>(T expected, P actual, string call)
        {
            if (expected == null && actual != null)
            {
                throw new Exception($"{call} should have returned null but returned {actual} ({actual.GetType()})");
            }

            if (expected != null && actual == null)
            {
                throw new Exception($"{call} should have returned {expected} ({expected.GetType()}) but returned null");
            }

            if (expected != null && actual != null && (expected.GetType() != actual.GetType() || !expected.Equals(actual)))
            {
                throw new Exception($"{call} should have returned {expected} ({expected.GetType()}) but returned {actual} ({actual.GetType()})");
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
        public long DataPoints => 0;

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
