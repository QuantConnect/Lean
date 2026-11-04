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
    /// Regression algorithm asserting the ObjectStore key rules: an unsupported key is sanitized and used on every
    /// save and read, and once the sanitized name is written directly a save through the unsupported key throws
    /// </summary>
    public class ObjectStoreKeySanitizationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialize the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);

            SetBenchmark(x => 0);

            AddEquity("SPY", Resolution.Daily);
        }

        /// <summary>
        /// End of algorithm run event handler. This method is called at the end of a backtest or live trading operation.
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            // an unsupported key, e.g. built from a user-facing name, is sanitized and used on every save and read
            const string invalidKey = "trade_log_ai_hardware_&_cloud.csv";
            const string sanitizedKey = "trade_log_ai_hardware___cloud.csv";
            if (!ObjectStore.Save(invalidKey, "a,b\n1,2"))
            {
                throw new RegressionTestException("Expected the unsupported key to be sanitized and saved");
            }
            if (!ObjectStore.Keys.Contains(sanitizedKey) || ObjectStore.Keys.Contains(invalidKey))
            {
                throw new RegressionTestException($"Expected the object to be stored under '{sanitizedKey}', keys: {string.Join(", ", ObjectStore.Keys)}");
            }
            if (!ObjectStore.ContainsKey(invalidKey) || ObjectStore.Read(invalidKey) != "a,b\n1,2" || ObjectStore.Read(sanitizedKey) != "a,b\n1,2")
            {
                throw new RegressionTestException("Expected the object to be read through both the original and the sanitized key");
            }

            // a later save through the same key updates the object
            if (!ObjectStore.Save(invalidKey, "a,b\n3,4") || ObjectStore.Read(sanitizedKey) != "a,b\n3,4")
            {
                throw new RegressionTestException("Expected the second save through the unsupported key to update the object");
            }

            // once the sanitized name is written directly, as an upload or another run would, it is never replaced through the unsupported key
            if (!ObjectStore.Save(sanitizedKey, "c,d\n5,6"))
            {
                throw new RegressionTestException("Expected the save through the sanitized key to succeed");
            }
            var errorMessage = string.Empty;
            try
            {
                ObjectStore.Save(invalidKey, "e,f\n7,8");
            }
            catch (ArgumentException exception)
            {
                errorMessage = exception.Message;
            }
            if (!errorMessage.Contains($"its sanitized name '{sanitizedKey}' already holds another object"))
            {
                throw new RegressionTestException($"Expected the save through the unsupported key to throw, got: '{errorMessage}'");
            }
            if (ObjectStore.Read(sanitizedKey) != "c,d\n5,6")
            {
                throw new RegressionTestException("Expected the object written through the sanitized key to be kept");
            }

            ObjectStore.Delete(sanitizedKey);
            if (ObjectStore.ContainsKey(invalidKey))
            {
                throw new RegressionTestException("Expected the delete of the sanitized key to remove the object the unsupported key reads");
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
        public long DataPoints => 6;

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
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
