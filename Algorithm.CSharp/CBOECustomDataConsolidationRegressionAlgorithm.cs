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
using System.IO;
using QuantConnect.Data;
using QuantConnect.Data.Custom.CBOE;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests the consolidation of custom data with random data
    /// </summary>
    public class CBOECustomDataConsolidationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _vix;
        private BollingerBands _bb;
        private bool _invested;

        /// <summary>
        /// Initializes the algorithm with fake VIX data
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _vix = AddData<IncrementallyGeneratedCustomData>("VIX", Resolution.Daily).Symbol;
            _bb = BB(_vix, 30, 2, MovingAverageType.Simple, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_bb.Current.Value == 0)
            {
                throw new Exception("Bollinger Band value is zero when we expect non-zero value.");
            }

            if (!_invested && _bb.Current.Value > 0.05m)
            {
                MarketOrder(_vix, 1);
                _invested = true;
            }
        }

        /// <summary>
        /// Incrementally updating data
        /// </summary>
        private class IncrementallyGeneratedCustomData : CBOE
        {
            private const decimal _start = 10.01m;
            private static decimal _step;

            /// <summary>
            /// Gets the source of the subscription. In this case, we set it to existing
            /// equity data so that we can pass fake data from Reader
            /// </summary>
            /// <param name="config">Subscription configuration</param>
            /// <param name="date">Date we're making this request</param>
            /// <param name="isLiveMode">Is live mode</param>
            /// <returns>Source of subscription</returns>
            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                return new SubscriptionDataSource(Path.Combine(Globals.DataFolder, "equity", "usa", "minute", "spy", $"{date:yyyyMMdd}_trade.zip#{date:yyyyMMdd}_spy_minute_trade.csv"), SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
            }

            /// <summary>
            /// Reads the data, which in this case is fake incremental data
            /// </summary>
            /// <param name="config">Subscription configuration</param>
            /// <param name="line">Line of data</param>
            /// <param name="date">Date of the request</param>
            /// <param name="isLiveMode">Is live mode</param>
            /// <returns>Incremental BaseData instance</returns>
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                var vix = new CBOE();
                _step += 0.10m;
                var open = _start + _step;
                var close = _start + _step + 0.02m;
                var high = close;
                var low = open;

                return new IncrementallyGeneratedCustomData
                {
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Time = date,
                    Symbol = new Symbol(
                        SecurityIdentifier.GenerateBase(typeof(IncrementallyGeneratedCustomData), "VIX", Market.USA, false),
                        "VIX"),
                    Period = vix.Period,
                    DataType = vix.DataType
                };
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        /// <remarks>
        /// Unable to be tested in Python, due to pythonnet not supporting overriding of methods from Python
        /// </remarks>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.029%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.000%"},
            {"Sharpe Ratio", "28.4"},
            {"Probabilistic Sharpe Ratio", "88.597%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-7.067"},
            {"Tracking Error", "0.193"},
            {"Treynor Ratio", "7.887"},
            {"Total Fees", "$0.00"},
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
            {"OrderListHash", "918912ee4f64cd0290f3d58deca02713"}
        };
    }
}
