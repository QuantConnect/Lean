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

using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm of how to use RangeConsolidator
    /// </summary>
    public class RangeConsolidatorAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private RangeBar _firstDataConsolidated;
        protected virtual int Range => 100;
        protected virtual Resolution Resolution => Resolution.Daily;

        public override void Initialize()
         {
            SetStartAndEndDates();
            AddEquity("SPY", Resolution);
            var rangeConsolidator = CreateRangeConsolidator();
            rangeConsolidator.DataConsolidated += OnDataConsolidated;
            _firstDataConsolidated = null;

            SubscriptionManager.AddConsolidator("SPY", rangeConsolidator);
        }

        public override void OnEndOfAlgorithm()
        {
            if (_firstDataConsolidated == null)
            {
                throw new Exception("The consolidator should have consolidated at least one RangeBar, but it did not consolidated any one");
            }
        }

        protected virtual void OnDataConsolidated(Object sender, RangeBar rangeBar)
        {
            if (_firstDataConsolidated == null)
            {
                _firstDataConsolidated = rangeBar;
            }
            // Log($"{rangeBar.Open} {rangeBar.High} {rangeBar.Low} {rangeBar.Close}");

            if (Math.Round(rangeBar.High - rangeBar.Low, 2) != (Range * 0.01m)) // The minimum price change for SPY is 0.01, therefore the range size of each bar equals Range * 0.01
            {
                throw new Exception($"The difference between the High and Low for all RangeBar's should be {Range * 0.01m}, but for this RangeBar was {Math.Round(rangeBar.High - rangeBar.Low), 2}");
            }
        }

        protected virtual void SetStartAndEndDates()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
        }

        protected virtual RangeConsolidator CreateRangeConsolidator()
        {
            return new RangeConsolidator(Range);
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
        public virtual long DataPoints => 48;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
