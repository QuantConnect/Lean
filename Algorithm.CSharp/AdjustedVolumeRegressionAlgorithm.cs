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
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test volume adjusted behavior
    /// </summary>
    public class AdjustedVolumeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;
        private const string Ticker = "AAPL";
        private readonly FactorFile _factorFile = FactorFile.Read(Ticker, "USA");
        private readonly IEnumerator<decimal> _expectedAdjustedVolume = new List<decimal> { 1541212.54m, 761012.76m, 920087.92m, 867076.87m, 542486.54m, 663131.66m, 
            374927.37m, 379554.38m, 413805.41m, 377622.38m }.GetEnumerator();
        private readonly IEnumerator<decimal> _expectedAdjustedAskSize = new List<decimal> { 53900.05m, 1400.00m, 6300.01m, 2100.00m, 1400.00m, 1400.00m, 700.00m, 
            2100.00m, 3500.00m, 700.00m }.GetEnumerator();
        private readonly IEnumerator<decimal> _expectedAdjustedBidSize = new List<decimal> { 700.00m, 2800.00m, 700.00m, 700.00m, 700.00m, 1400.00m, 2800.00m,
            2100.00m, 7700.01m, 700.00m }.GetEnumerator();


        private List<decimal> _testTradeVolume = new List<decimal>();
        private List<decimal> _testQuoteAskSize = new List<decimal>();
        private List<decimal> _testQuoteBidSize = new List<decimal>();

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);      //Set Start Date
            SetEndDate(2014, 6, 5);         //Set End Date

            UniverseSettings.DataNormalizationMode = DataNormalizationMode.SplitAdjusted;
            _aapl = AddEquity(Ticker, Resolution.Minute).Symbol;
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
                var aaplVolume = decimal.Round(aaplData.Volume, 2);

                // Assert our volume matches what we expect
                if (_expectedAdjustedVolume.MoveNext() && _expectedAdjustedVolume.Current != aaplVolume)
                {
                    // Our values don't match lets try and give a reason why
                    var dayFactor = _factorFile.GetSplitFactor(aaplData.Time);
                    var probableAdjustedVolume = decimal.Round(aaplData.Volume / dayFactor, 2);

                    if (_expectedAdjustedVolume.Current == probableAdjustedVolume)
                    {
                        throw new Exception($"Volume was incorrect; but manually adjusted value is correct." + 
                            $" Adjustment by multiplying volume by {1 / dayFactor} is not occurring.");
                    }
                    else
                    {
                        throw new Exception($"Volume was incorrect; even when adjusted manually by" + 
                            $" multiplying volume by {1 / dayFactor}. Data may have changed.");
                    }
                }
            }

            if (data.QuoteBars.ContainsKey(_aapl))
            {
                var aaplQuoteData = data.QuoteBars[_aapl];
                var aaplAskSize = decimal.Round(aaplQuoteData.LastAskSize, 2);
                var aaplBidSize = decimal.Round(aaplQuoteData.LastBidSize, 2);

                // Assert our askSize matches what we expect
                if (_expectedAdjustedAskSize.MoveNext() && _expectedAdjustedAskSize.Current != aaplAskSize)
                {
                    // Our values don't match lets try and give a reason why
                    var dayFactor = _factorFile.GetSplitFactor(aaplQuoteData.Time);
                    var probableAdjustedAskSize = decimal.Round(aaplQuoteData.LastAskSize / dayFactor, 2);

                    if (_expectedAdjustedAskSize.Current == probableAdjustedAskSize)
                    {
                        throw new Exception($"Ask size was incorrect; but manually adjusted value is correct." +
                            $" Adjustment by multiplying size by {1 / dayFactor} is not occurring.");
                    }
                    else
                    {
                        throw new Exception($"Ask size was incorrect; even when adjusted manually by" +
                            $" multiplying size by {1 / dayFactor}. Data may have changed.");
                    }
                }

                // Assert our bidSize matches what we expect
                if (_expectedAdjustedBidSize.MoveNext() && _expectedAdjustedBidSize.Current != aaplBidSize)
                {
                    // Our values don't match lets try and give a reason why
                    var dayFactor = _factorFile.GetSplitFactor(aaplQuoteData.Time);
                    var probableAdjustedBidSize = decimal.Round(aaplQuoteData.LastBidSize / dayFactor, 2);

                    if (_expectedAdjustedBidSize.Current == probableAdjustedBidSize)
                    {
                        throw new Exception($"Bid size was incorrect; but manually adjusted value is correct." +
                            $" Adjustment by multiplying size by {1 / dayFactor} is not occurring.");
                    }
                    else
                    {
                        throw new Exception($"Bid size was incorrect; even when adjusted manually by" +
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "$5.40"},
            {"Estimated Strategy Capacity", "$42000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0"},
            {"Return Over Maximum Drawdown", "0"},
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
            {"OrderListHash", "43a72d9759cdbd442d5b53a44370e579"}
        };
    }
}
