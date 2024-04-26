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
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this algorithm we demonstrate how to use the raw data for our securities
    /// and verify that the behavior is correct.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="regression test" />
    public class RawDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string Ticker = "GOOGL";
        private CorporateFactorProvider _factorFile;
        private readonly IEnumerator<decimal> _expectedRawPrices = new List<decimal> { 1157.93m, 1158.72m,
            1131.97m, 1114.28m, 1120.15m, 1114.51m, 1134.89m, 567.55m, 571.50m, 545.25m, 540.63m }.GetEnumerator();
        private Symbol _googl;

        public override void Initialize()
        {
            SetStartDate(2014, 3, 25);      //Set Start Date
            SetEndDate(2014, 4, 7);         //Set End Date
            SetCash(100000);                            //Set Strategy Cash

            // Set our DataNormalizationMode to raw
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            _googl = AddEquity(Ticker, Resolution.Daily).Symbol;

            // Get our factor file for this regression
            var dataProvider =
                Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider",
                    "DefaultDataProvider"));

            var mapFileProvider = new LocalDiskMapFileProvider();
            mapFileProvider.Initialize(dataProvider);
            var factorFileProvider = new LocalDiskFactorFileProvider();
            factorFileProvider.Initialize(mapFileProvider, dataProvider);
            _factorFile = factorFileProvider.Get(_googl) as CorporateFactorProvider;

            // Prime our expected values
            _expectedRawPrices.MoveNext();
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_googl, 1);
            }

            if (data.Bars.ContainsKey(_googl))
            {
                var googlData = data.Bars[_googl];

                // Assert our volume matches what we expected
                if (_expectedRawPrices.Current != googlData.Close)
                {
                    // Our values don't match lets try and give a reason why
                    var dayFactor = _factorFile.GetPriceFactor(googlData.Time, DataNormalizationMode.Adjusted);
                    var probableRawPrice = googlData.Close / dayFactor; // Undo adjustment

                    if (_expectedRawPrices.Current == probableRawPrice)
                    {
                        throw new Exception($"Close price was incorrect; it appears to be the adjusted value");
                    }
                    else
                    {
                        throw new Exception($"Close price was incorrect; Data may have changed.");
                    }
                }

                // Move to our next expected value
                _expectedRawPrices.MoveNext();
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
        public long DataPoints => 92;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-86.060%"},
            {"Drawdown", "7.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "92720.82"},
            {"Net Profit", "-7.279%"},
            {"Sharpe Ratio", "-2.914"},
            {"Sortino Ratio", "-2.855"},
            {"Probabilistic Sharpe Ratio", "4.001%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.373"},
            {"Beta", "1.927"},
            {"Annual Standard Deviation", "0.254"},
            {"Annual Variance", "0.064"},
            {"Information Ratio", "-2.852"},
            {"Tracking Error", "0.192"},
            {"Treynor Ratio", "-0.384"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$110000000.00"},
            {"Lowest Capacity Asset", "GOOG T1AZ164W5VTX"},
            {"Portfolio Turnover", "7.21%"},
            {"OrderListHash", "a34fbe3e3812d3f453cd63924c06f89a"}
        };
    }
}
