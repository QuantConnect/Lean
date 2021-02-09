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
using System.Linq;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Futures framework algorithm that uses open interest to select the active contract.
    /// </summary>
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="futures" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="filter selection" />
    public class OpenInterestFuturesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly HashSet<DateTime> ExpectedExpiryDates = new HashSet<DateTime>
        {
            new DateTime(2013, 12, 27),
            new DateTime(2014, 02, 26)
        };

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Tick;

            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 11);
            SetCash(10000000);

            // set framework models
            SetUniverseSelection(
                new OpenInterestFutureUniverseSelectionModel(
                    this,
                    t => new[] {QuantConnect.Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX)},
                    null,
                    ExpectedExpiryDates.Count
                )
            );
        }

        public override void OnData(Slice slice)
        {
            if (Transactions.OrdersCount == 0 && slice.HasData)
            {
                var matched = slice.Keys.Where(s => !ExpectedExpiryDates.Contains(s.ID.Date)).ToList();
                if (matched.Count != 0)
                {
                    throw new Exception($"{matched.Count}/{slice.Keys.Count} were unexpected expiry date(s): " + string.Join(", ", matched.Select(x => x.ID.Date)));
                }

                foreach (var symbol in slice.Keys)
                {
                    MarketOrder(symbol, 1);
                }
            }
            else if (Portfolio.Any(p => p.Value.Invested))
            {
                Liquidate();
            }
        }

        /// <summary>
        ///     This is used by the regression test system to indicate if the open source Lean repository has the required data to
        ///     run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        ///     This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = {Language.CSharp};

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "0.003%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0.351"},
            {"Net Profit", "0.000%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.70"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-58.133"},
            {"Tracking Error", "0.173"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$7.40"},
            {"Fitness Score", "0.017"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.017"},
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
            {"OrderListHash", "75477a2d1f470d97bdbc5689712c54bf"}
        };
    }
}
