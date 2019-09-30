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
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template options framework algorithm uses framework components to define an algorithm
    /// that trades options.
    /// </summary>
    public class BasicTemplateOptionsFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 06);
            SetCash(100000);

            // set framework models
            SetUniverseSelection(new EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel(SelectOptionChainSymbols));
            SetAlpha(new ConstantOptionContractAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromHours(0.5)));
            SetPortfolioConstruction(new SingleSharePortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        // option symbol universe selection function
        private static IEnumerable<Symbol> SelectOptionChainSymbols(DateTime utcTime)
        {
            var newYorkTime = utcTime.ConvertFromUtc(TimeZones.NewYork);
            if (newYorkTime.Date < new DateTime(2014, 06, 06))
            {
                yield return QuantConnect.Symbol.Create("TWX", SecurityType.Option, Market.USA, "?TWX");
            }

            if (newYorkTime.Date >= new DateTime(2014, 06, 06))
            {
                yield return QuantConnect.Symbol.Create("AAPL", SecurityType.Option, Market.USA, "?AAPL");
            }
        }

        /// <summary>
        /// Creates option chain universes that select only the earliest expiry ATM weekly put contract
        /// and runs a user defined optionChainSymbolSelector every day to enable choosing different option chains
        /// </summary>
        class EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel : OptionUniverseSelectionModel
        {
            public EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector)
                : base(TimeSpan.FromDays(1), optionChainSymbolSelector)
            {
            }

            /// <summary>
            /// Defines the option chain universe filter
            /// </summary>
            protected override OptionFilterUniverse Filter(OptionFilterUniverse filter)
            {
                return filter
                    .Strikes(+1, +1)
                    .Expiration(TimeSpan.Zero, TimeSpan.FromDays(7))
                    .WeeklysOnly()
                    .PutsOnly()
                    .OnlyApplyFilterAtMarketOpen();
            }
        }

        /// <summary>
        /// Implementation of a constant alpha model that only emits insights for option symbols
        /// </summary>
        class ConstantOptionContractAlphaModel : ConstantAlphaModel
        {
            public ConstantOptionContractAlphaModel(InsightType type, InsightDirection direction, TimeSpan period)
                : base(type, direction, period)
            {
            }

            protected override bool ShouldEmitInsight(DateTime utcTime, Symbol symbol)
            {
                // only emit alpha for option symbols and not underlying equity symbols
                if (symbol.SecurityType != SecurityType.Option)
                {
                    return false;
                }

                return base.ShouldEmitInsight(utcTime, symbol);
            }
        }

        /// <summary>
        /// Portfolio construction model that sets target quantities to 1 for up insights and -1 for down insights
        /// </summary>
        class SingleSharePortfolioConstructionModel : PortfolioConstructionModel
        {
            public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
            {
                foreach (var insight in insights)
                {
                    yield return new PortfolioTarget(insight.Symbol, (int) insight.Direction);
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0.14%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "63.870%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.271%"},
            {"Sharpe Ratio", "9.165"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.086"},
            {"Beta", "0.327"},
            {"Annual Standard Deviation", "0.025"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-18.063"},
            {"Tracking Error", "0.04"},
            {"Treynor Ratio", "0.696"},
            {"Total Fees", "$4.00"},
            {"Total Insights Generated", "26"},
            {"Total Insights Closed", "24"},
            {"Total Insights Analysis Completed", "24"},
            {"Long Insight Count", "26"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$26.24608"},
            {"Total Accumulated Estimated Alpha Value", "$1.89555"},
            {"Mean Population Estimated Insight Value", "$0.07898125"},
            {"Mean Population Direction", "50%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "50.0482%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}
