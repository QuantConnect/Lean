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
using System.Runtime.InteropServices;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test volume adjusted behavior
    /// </summary>
    public class AdjustedVolumeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string Ticker = "GOOGL";
        private readonly FactorFile _factorFile = FactorFile.Read(Ticker, "USA");
        private readonly IEnumerator<decimal> _expectedAdjustedVolume = new List<decimal> { 5488974, 4635736, 
            4759984, 7273568, 4329774, 3426174, 4133260, 3882244, 3924251, 5162299, 3831660 }.GetEnumerator();
        private Symbol _googl;

        public override void Initialize()
        {
            SetStartDate(2014, 3, 25);      //Set Start Date
            SetEndDate(2014, 4, 7);         //Set End Date
            SetCash(100000);                            //Set Strategy Cash

            UniverseSettings.DataNormalizationMode = DataNormalizationMode.SplitAdjusted;
            _googl = AddEquity(Ticker, Resolution.Daily).Symbol;
            
            Schedule.On(DateRules.On(2014, 4, 3), TimeRules.At(5, 0), () => Log("GOOG 2:1 split"));

            // Prime our expected values
            _expectedAdjustedVolume.MoveNext();
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
                if (_expectedAdjustedVolume.Current != googlData.Volume)
                {
                    // Our values don't match lets try and give a reason why
                    var dayFactor = _factorFile.GetPriceScaleFactor(googlData.Time);
                    var probableAdjustedVolume = googlData.Volume / dayFactor;

                    if (_expectedAdjustedVolume.Current == probableAdjustedVolume)
                    {
                        throw new Exception($"Volume was incorrect; but manually adjusted value is correct." +
                            $" Adjustment by multiplying volume by {1/dayFactor} is not occurring.");
                    }
                    else
                    {
                        throw new Exception($"Volume was incorrect; even when adjusted manually by" +
                            $" multiplying volume by {1/dayFactor}. Data may have changed.");
                    }
                }

                // Move to our next expected value
                _expectedAdjustedVolume.MoveNext();
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-85.948%"},
            {"Drawdown", "7.300%"},
            {"Expectancy", "0"},
            {"Net Profit", "-7.251%"},
            {"Sharpe Ratio", "-3.008"},
            {"Probabilistic Sharpe Ratio", "3.159%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.831"},
            {"Beta", "-0.223"},
            {"Annual Standard Deviation", "0.262"},
            {"Annual Variance", "0.069"},
            {"Information Ratio", "-2.045"},
            {"Tracking Error", "0.289"},
            {"Treynor Ratio", "3.525"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$110000000.00"},
            {"Fitness Score", "0.006"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-3.445"},
            {"Return Over Maximum Drawdown", "-11.853"},
            {"Portfolio Turnover", "0.084"},
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
            {"OrderListHash", "b5d828a6c9a32c55f26d2df34ed80f05"}
        };
    }
}
