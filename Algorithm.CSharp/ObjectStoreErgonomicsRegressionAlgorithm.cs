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
    /// Regression algorithm asserting the ObjectStore ergonomics: unsupported keys are rejected with the
    /// key rules stated in the error, <see cref="Storage.ObjectStore.SanitizeKey"/> converts arbitrary names
    /// into supported keys, <see cref="Storage.ObjectStore.SaveText"/> is an alias of Save and reading a
    /// missing key lists the available keys in the error. The Python version of this algorithm additionally
    /// covers the tolerant save_json/read_json and save_dataframe helpers
    /// </summary>
    public class ObjectStoreErgonomicsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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
            // an unsupported key, e.g. built from a user-facing name, is rejected stating the rules and the fix
            const string invalidKey = "trade_log_ai_hardware_&_cloud.csv";
            var errorMessage = string.Empty;
            try
            {
                ObjectStore.Save(invalidKey, "a,b\n1,2");
            }
            catch (ArgumentException exception)
            {
                errorMessage = exception.Message;
            }
            if (!errorMessage.Contains(Storage.ObjectStore.SupportedKeyRules) || !errorMessage.Contains("SanitizeKey"))
            {
                throw new RegressionTestException($"Expected the key rules in the unsupported key error, got: '{errorMessage}'");
            }

            // SanitizeKey converts the arbitrary name into a supported key
            var sanitized = Storage.ObjectStore.SanitizeKey(invalidKey);
            if (sanitized != "trade_log_ai_hardware___cloud.csv")
            {
                throw new RegressionTestException($"Unexpected sanitized key: '{sanitized}'");
            }
            if (!Storage.ObjectStore.IsSupportedKey(sanitized) || !ObjectStore.Save(sanitized, "a,b\n1,2"))
            {
                throw new RegressionTestException("Expected the sanitized key to be storable");
            }

            // SaveText is an alias of Save
            if (!ObjectStore.SaveText("ergonomics_report.txt", "The strategy went up") ||
                ObjectStore.Read("ergonomics_report.txt") != "The strategy went up")
            {
                throw new RegressionTestException("Expected the SaveText/Read round trip to succeed");
            }

            // reading a missing key lists the available keys in the error
            errorMessage = string.Empty;
            try
            {
                ObjectStore.ReadBytes("ergonomics_missing.json");
            }
            catch (KeyNotFoundException exception)
            {
                errorMessage = exception.Message;
            }
            if (!errorMessage.Contains("Keys: [") || !errorMessage.Contains($"'{sanitized}'"))
            {
                throw new RegressionTestException($"Expected the available keys in the missing key error, got: '{errorMessage}'");
            }

            ObjectStore.Delete(sanitized);
            ObjectStore.Delete("ergonomics_report.txt");
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
