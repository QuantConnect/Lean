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
            {"Total Trades", "7"},
            {"Average Win", "0%"},
            {"Average Loss", "-10.53%"},
            {"Compounding Annual Return", "5954496901.426%"},
            {"Drawdown", "62.100%"},
            {"Expectancy", "-1"},
            {"Net Profit", "25.977%"},
            {"Sharpe Ratio", "5.943"},
            {"Probabilistic Sharpe Ratio", "73.556%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "9.98"},
            {"Beta", "32.209"},
            {"Annual Standard Deviation", "7.43"},
            {"Annual Variance", "55.209"},
            {"Information Ratio", "5.969"},
            {"Tracking Error", "7.221"},
            {"Treynor Ratio", "1.371"},
            {"Total Fees", "$262.70"},
            {"Fitness Score", "0.307"},
            {"Kelly Criterion Estimate", "14.108"},
            {"Kelly Criterion Probability Value", "0.374"},
            {"Sortino Ratio", "-0.7"},
            {"Return Over Maximum Drawdown", "-2.102"},
            {"Portfolio Turnover", "34.419"},
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