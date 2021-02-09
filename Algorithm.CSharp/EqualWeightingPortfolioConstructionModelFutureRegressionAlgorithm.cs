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
        private int _fillCount;

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
            return new []
            {
                QuantConnect.Symbol.Create(Futures.Indices.SP500EMini, SecurityType.Future, Market.CME),
                QuantConnect.Symbol.Create(Futures.Metals.Gold, SecurityType.Future, Market.COMEX)
            };
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
            if (orderEvent.Status == OrderStatus.Filled)
            {
                _fillCount++;
                if (_fillCount == 2)
                {
                    if (Portfolio.TotalHoldingsValue / Portfolio.TotalPortfolioValue < 10)
                    {
                        throw new Exception("Expected to be trading using the futures margin leverage");
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
            {"Total Trades", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.91%"},
            {"Compounding Annual Return", "-99.755%"},
            {"Drawdown", "29.500%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-7.463%"},
            {"Sharpe Ratio", "-0.605"},
            {"Probabilistic Sharpe Ratio", "34.850%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-13.685"},
            {"Beta", "6.59"},
            {"Annual Standard Deviation", "1.632"},
            {"Annual Variance", "2.665"},
            {"Information Ratio", "-2.023"},
            {"Tracking Error", "1.441"},
            {"Treynor Ratio", "-0.15"},
            {"Total Fees", "$33.30"},
            {"Fitness Score", "0.079"},
            {"Kelly Criterion Estimate", "-9.366"},
            {"Kelly Criterion Probability Value", "0.607"},
            {"Sortino Ratio", "-4.657"},
            {"Return Over Maximum Drawdown", "-5.203"},
            {"Portfolio Turnover", "4.377"},
            {"Total Insights Generated", "10"},
            {"Total Insights Closed", "8"},
            {"Total Insights Analysis Completed", "8"},
            {"Long Insight Count", "10"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$-78.89231"},
            {"Total Accumulated Estimated Alpha Value", "$-12.82"},
            {"Mean Population Estimated Insight Value", "$-1.6025"},
            {"Mean Population Direction", "25%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "25.058%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "f4c4b763b5ade918cfb7932e276d069f"}
        };
    }
}
