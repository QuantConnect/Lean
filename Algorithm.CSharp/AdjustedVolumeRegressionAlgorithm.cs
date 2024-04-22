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
    /// Regression algorithm to test volume adjusted behavior
    /// </summary>
    public class AdjustedVolumeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;
        private const string Ticker = "AAPL";
        private CorporateFactorProvider _factorFile;
        private readonly IEnumerator<decimal> _expectedAdjustedVolume = new List<decimal> { 6164842, 3044047, 3680347, 3468303, 2169943, 2652523,
            1499707, 1518215, 1655219, 1510487 }.GetEnumerator();
        private readonly IEnumerator<decimal> _expectedAdjustedAskSize = new List<decimal> { 215600, 5600, 25200, 8400, 5600, 5600, 2800,
            8400, 14000, 2800 }.GetEnumerator();
        private readonly IEnumerator<decimal> _expectedAdjustedBidSize = new List<decimal> { 2800, 11200, 2800, 2800, 2800, 5600, 11200,
            8400, 30800, 2800 }.GetEnumerator();

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);      //Set Start Date
            SetEndDate(2014, 6, 5);         //Set End Date

            UniverseSettings.DataNormalizationMode = DataNormalizationMode.SplitAdjusted;
            _aapl = AddEquity(Ticker, Resolution.Minute).Symbol;

            var dataProvider =
                Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider",
                    "DefaultDataProvider"));

            var mapFileProvider = new LocalDiskMapFileProvider();
            mapFileProvider.Initialize(dataProvider);
            var factorFileProvider = new LocalDiskFactorFileProvider();
            factorFileProvider.Initialize(mapFileProvider, dataProvider);


            _factorFile = factorFileProvider.Get(_aapl) as CorporateFactorProvider;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_aapl, 1);
            }

            if (data.Splits.ContainsKey(_aapl))
            {
                Log(data.Splits[_aapl].ToString());
            }

            if (data.Bars.ContainsKey(_aapl))
            {
                var aaplData = data.Bars[_aapl];

                // Assert our volume matches what we expect
                if (_expectedAdjustedVolume.MoveNext() && _expectedAdjustedVolume.Current != aaplData.Volume)
                {
                    // Our values don't match lets try and give a reason why
                    var dayFactor = _factorFile.GetPriceScale(aaplData.Time, DataNormalizationMode.SplitAdjusted);
                    var probableAdjustedVolume = aaplData.Volume / dayFactor;

                    if (_expectedAdjustedVolume.Current == probableAdjustedVolume)
                    {
                        throw new ArgumentException($"Volume was incorrect; but manually adjusted value is correct." + 
                            $" Adjustment by multiplying volume by {1 / dayFactor} is not occurring.");
                    }
                    else
                    {
                        throw new ArgumentException($"Volume was incorrect; even when adjusted manually by" + 
                            $" multiplying volume by {1 / dayFactor}. Data may have changed.");
                    }
                }
            }

            if (data.QuoteBars.ContainsKey(_aapl))
            {
                var aaplQuoteData = data.QuoteBars[_aapl];

                // Assert our askSize matches what we expect
                if (_expectedAdjustedAskSize.MoveNext() && _expectedAdjustedAskSize.Current != aaplQuoteData.LastAskSize)
                {
                    // Our values don't match lets try and give a reason why
                    var dayFactor = _factorFile.GetPriceScale(aaplQuoteData.Time, DataNormalizationMode.SplitAdjusted);
                    var probableAdjustedAskSize = aaplQuoteData.LastAskSize / dayFactor;

                    if (_expectedAdjustedAskSize.Current == probableAdjustedAskSize)
                    {
                        throw new ArgumentException($"Ask size was incorrect; but manually adjusted value is correct." +
                            $" Adjustment by multiplying size by {1 / dayFactor} is not occurring.");
                    }
                    else
                    {
                        throw new ArgumentException($"Ask size was incorrect; even when adjusted manually by" +
                            $" multiplying size by {1 / dayFactor}. Data may have changed.");
                    }
                }

                // Assert our bidSize matches what we expect
                if (_expectedAdjustedBidSize.MoveNext() && _expectedAdjustedBidSize.Current != aaplQuoteData.LastBidSize)
                {
                    // Our values don't match lets try and give a reason why
                    var dayFactor = _factorFile.GetPriceScale(aaplQuoteData.Time, DataNormalizationMode.SplitAdjusted);
                    var probableAdjustedBidSize = aaplQuoteData.LastBidSize / dayFactor;

                    if (_expectedAdjustedBidSize.Current == probableAdjustedBidSize)
                    {
                        throw new ArgumentException($"Bid size was incorrect; but manually adjusted value is correct." +
                            $" Adjustment by multiplying size by {1 / dayFactor} is not occurring.");
                    }
                    else
                    {
                        throw new ArgumentException($"Bid size was incorrect; even when adjusted manually by" +
                            $" multiplying size by {1 / dayFactor}. Data may have changed.");
                    }
                }
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
        public long DataPoints => 795;

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
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100146.57"},
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
            {"Total Fees", "$21.60"},
            {"Estimated Strategy Capacity", "$42000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "99.56%"},
            {"OrderListHash", "60f03c8c589a4f814dc4e8945df23207"}
        };
    }
}
