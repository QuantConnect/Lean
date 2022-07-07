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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm asserts that the regular and extended hours match what is expected from the database for futures.
    /// </summary>
    public class FuturesExtendedMarketHoursTimeCheckRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _es;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 6);
            SetEndDate(2013, 10, 11);

            _es = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, fillDataForward: true, extendedMarketHours: true);
            _es.SetFilter(0, 180);
        }

        public override void OnData(Slice slice)
        {
            var esIsInRegularHours = _es.Exchange.Hours.IsOpen(Time, false);
            var esIsInExtendedHours = !esIsInRegularHours && _es.Exchange.Hours.IsOpen(Time, true);

            var currentTimeIsRegularHours = Time.TimeOfDay >= new TimeSpan(9, 30, 0) && Time.TimeOfDay < new TimeSpan(17, 0, 0);
            var currentTimeIsExtendedHours = !currentTimeIsRegularHours
                && (Time.TimeOfDay < new TimeSpan(9, 30, 0) || Time.TimeOfDay >= new TimeSpan(18, 0, 0));

            if (esIsInRegularHours != currentTimeIsRegularHours || esIsInExtendedHours != currentTimeIsExtendedHours)
            {
                throw new Exception($"At {Time}, {_es.Symbol} is either in regular hours but current time is in extended hours, or viceversa");
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
        public long DataPoints => 97442;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
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
            {"Information Ratio", "-2.564"},
            {"Tracking Error", "0.214"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
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
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
