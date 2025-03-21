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

using System;
using System.Text;
using QuantConnect.Data;
using System.Globalization;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of fill forward when using daily strict end times
    /// </summary>
    public class FillForwardStrictEndTimeHourRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly bool _updateExpectedData;
        private readonly StringBuilder _data = new();

        protected virtual string ExpectedDataFile => $"../../TestData/{GetType().Name}.zip";
        protected virtual int StartDate => 4;
        protected virtual Resolution FillForwardResolution => Resolution.Hour;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2021, 1, StartDate);
            SetEndDate(2021, 1, 15);

            AddIndex("SPX", Resolution.Daily);
            AddEquity("SPY", FillForwardResolution);

            Settings.DailyPreciseEndTime = true;
        }

        /// <summary>
        /// Index EMA Cross trading index options of the index.
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (slice.ContainsKey("SPX"))
            {
                var spxData = slice.Bars["SPX"];
                var message = $"{Time.ToString(CultureInfo.GetCultureInfo("en-US"))} ==== FF {spxData.IsFillForward}. {spxData.ToString().Replace(',', '.')} {spxData.Time:HH:mm:ss}->{spxData.EndTime:HH:mm:ss}";
                _data.AppendLine(message);
                Debug(message);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var data = _data.ToString();
            if (_updateExpectedData)
            {
                Compression.ZipData(ExpectedDataFile, new Dictionary<string, string>() { { "zip_entry_name.txt", data } });
                return;
            }

            var expected  = string.Join(';', Compression.ReadLines(ExpectedDataFile)).ReplaceLineEndings("");
            if (expected != data.ReplaceLineEndings(";").RemoveFromEnd(";"))
            {
                throw new RegressionTestException($"Unexpected data: \"{data}\"{Environment.NewLine}Expected: \"{expected}\"");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 222;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
            {"Information Ratio", "-5.208"},
            {"Tracking Error", "0.103"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
