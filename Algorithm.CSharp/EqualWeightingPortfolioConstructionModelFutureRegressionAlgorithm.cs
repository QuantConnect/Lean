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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Futures regression algorithm intended to test the behavior of the framework models. See GH issue 4027.
    /// </summary>
    public class EqualWeightingPortfolioConstructionModelFutureRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            SetUniverseSelection(new FrontMonthFutureUniverseSelectionModel(SelectFutureChainSymbols));
            SetAlpha(new ConstantFutureContractAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());

            // leave a nice buffer to avoid getting wiped out, futures have a lot of leverage!
            Settings.FreePortfolioValuePercentage = 0.5m;
        }

        // future symbol universe selection function
        private static IEnumerable<Symbol> SelectFutureChainSymbols(DateTime utcTime)
        {
            yield return QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.USA);
        }

        /// <summary>
        /// Creates futures chain universes that select the front month contract and runs a user
        /// defined futureChainSymbolSelector every day to enable choosing different futures chains
        /// </summary>
        class FrontMonthFutureUniverseSelectionModel : FutureUniverseSelectionModel
        {
            public FrontMonthFutureUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> futureChainSymbolSelector)
                : base(TimeSpan.FromDays(1), futureChainSymbolSelector)
            {
            }

            /// <summary>
            /// Defines the future chain universe filter
            /// </summary>
            protected override FutureFilterUniverse Filter(FutureFilterUniverse filter)
            {
                return filter
                    .FrontMonth()
                    .OnlyApplyFilterAtMarketOpen();
            }
        }

        /// <summary>
        /// Implementation of a constant alpha model that only emits insights for future symbols
        /// </summary>
        class ConstantFutureContractAlphaModel : ConstantAlphaModel
        {
            public ConstantFutureContractAlphaModel(InsightType type, InsightDirection direction, TimeSpan period)
                : base(type, direction, period)
            {
            }

            protected override bool ShouldEmitInsight(DateTime utcTime, Symbol symbol)
            {
                // only emit alpha for future symbols and not underlying equity symbols
                if (symbol.SecurityType != SecurityType.Future)
                {
                    return false;
                }

                return base.ShouldEmitInsight(utcTime, symbol);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"{orderEvent}");
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
            {"Total Trades", "11"},
            {"Average Win", "48.13%"},
            {"Average Loss", "-16.50%"},
            {"Compounding Annual Return", "79228162514264337593543950335%"},
            {"Drawdown", "79.100%"},
            {"Expectancy", "0.119"},
            {"Net Profit", "191.526%"},
            {"Sharpe Ratio", "17.858"},
            {"Probabilistic Sharpe Ratio", "88.094%"},
            {"Loss Rate", "71%"},
            {"Win Rate", "29%"},
            {"Profit-Loss Ratio", "2.92"},
            {"Alpha", "178.049"},
            {"Beta", "28.197"},
            {"Annual Standard Deviation", "11.646"},
            {"Annual Variance", "135.634"},
            {"Information Ratio", "17.945"},
            {"Tracking Error", "11.531"},
            {"Treynor Ratio", "7.376"},
            {"Total Fees", "$1944.35"},
            {"Fitness Score", "0.999"},
            {"Kelly Criterion Estimate", "14.108"},
            {"Kelly Criterion Probability Value", "0.374"},
            {"Sortino Ratio", "126431249752217"},
            {"Return Over Maximum Drawdown", "992643312426759.791"},
            {"Portfolio Turnover", "278.562"},
            {"Total Insights Generated", "5"},
            {"Total Insights Closed", "4"},
            {"Total Insights Analysis Completed", "4"},
            {"Long Insight Count", "5"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$-70.18462"},
            {"Total Accumulated Estimated Alpha Value", "$-11.405"},
            {"Mean Population Estimated Insight Value", "$-2.85125"},
            {"Mean Population Direction", "25%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "25%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}