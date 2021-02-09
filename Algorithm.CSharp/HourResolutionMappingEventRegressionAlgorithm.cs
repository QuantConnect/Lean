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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue #5232
    /// </summary>
    public class HourResolutionMappingEventRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _dateTime;
        private SymbolChangedEvent _changedEvent;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2008, 08, 20);
            SetEndDate(2008, 10, 1);

            AddEquity("SPWR", Resolution.Hour, fillDataForward:false);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            _dateTime = Time.Date;
            if (!Portfolio.Invested)
            {
                SetHoldings("SPWR", 1);
            }

            foreach (var symbolChangedEvent in data.SymbolChangedEvents.Values)
            {
                _changedEvent = symbolChangedEvent;
                Log($"{Time}: {symbolChangedEvent.OldSymbol} -> {symbolChangedEvent.NewSymbol}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_dateTime != EndDate.Date)
            {
                throw new Exception($"Last day was {_dateTime}, should be algorithm end date: {EndDate.Date}");
            }
            if (_changedEvent == null)
            {
                throw new Exception("We got not symbol change event! 'SPWR' should of been mapped");
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
            {"Compounding Annual Return", "-78.316%"},
            {"Drawdown", "31.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "-16.363%"},
            {"Sharpe Ratio", "-0.506"},
            {"Probabilistic Sharpe Ratio", "27.578%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.431"},
            {"Beta", "1.976"},
            {"Annual Standard Deviation", "1.118"},
            {"Annual Variance", "1.25"},
            {"Information Ratio", "-0.071"},
            {"Tracking Error", "0.866"},
            {"Treynor Ratio", "-0.286"},
            {"Total Fees", "$5.40"},
            {"Fitness Score", "0.008"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-1.038"},
            {"Return Over Maximum Drawdown", "-2.536"},
            {"Portfolio Turnover", "0.033"},
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
            {"OrderListHash", "8d5c6263fbdfa4b2338fc725e27b93e9"}
        };
    }
}
