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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm making sure that the added universe selection does not remove the option chain during it's daily refresh
    /// </summary>
    public class OptionChainedAndUniverseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aaplOption;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 09);

            _aaplOption = AddOption("AAPL").Symbol;
            AddUniverseSelection(new DailyUniverseSelectionModel("MyCustomSelectionModel", time => new[] { "AAPL" }, this));
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                Buy("AAPL", 1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var config = SubscriptionManager.Subscriptions.ToList();
            if (config.All(dataConfig => dataConfig.Symbol != "AAPL"))
            {
                throw new Exception("Was expecting configurations for AAPL");
            }
            if (config.All(dataConfig => dataConfig.Symbol.SecurityType != SecurityType.Option))
            {
                throw new Exception($"Was expecting configurations for {_aaplOption}");
            }
        }

        private class DailyUniverseSelectionModel : CustomUniverseSelectionModel
        {
            private DateTime _lastRefresh;
            private IAlgorithm _algorithm;

            public DailyUniverseSelectionModel(string name, Func<DateTime, IEnumerable<string>> selector, IAlgorithm algorithm) : base(name, selector)
            {
                _algorithm = algorithm;
            }

            public override DateTime GetNextRefreshTimeUtc()
            {
                if (_lastRefresh != _algorithm.Time.Date)
                {
                    _lastRefresh = _algorithm.Time.Date;
                    return DateTime.MinValue;
                }
                return DateTime.MaxValue;
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
            {"Compounding Annual Return", "0.678%"},
            {"Drawdown", "3.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.009%"},
            {"Sharpe Ratio", "7.969"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.046"},
            {"Beta", "-0.032"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-24.461"},
            {"Tracking Error", "0.044"},
            {"Treynor Ratio", "-0.336"},
            {"Total Fees", "$1.00"},
            {"Fitness Score", "0.003"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.003"},
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
            {"OrderListHash", "e2718d95499fcbdb51cabc32d6e28202"}
        };
    }
}
