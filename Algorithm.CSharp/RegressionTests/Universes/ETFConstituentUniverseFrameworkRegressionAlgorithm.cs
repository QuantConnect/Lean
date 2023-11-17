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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests ETF constituents universe selection with the algorithm framework models (Alpha, PortfolioConstruction, Execution)
    /// </summary>
    public class ETFConstituentUniverseFrameworkRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<ETFConstituentData> ConstituentData = new List<ETFConstituentData>();
        
        /// <summary>
        /// Initializes the algorithm, setting up the framework classes and ETF constituent universe settings
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2020, 12, 1);
            SetEndDate(2021, 1, 31);
            SetCash(100000);
            
            SetAlpha(new ETFConstituentAlphaModel());
            SetPortfolioConstruction(new ETFConstituentPortfolioModel());
            SetExecution(new ETFConstituentExecutionModel());

            var spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            UniverseSettings.Resolution = Resolution.Hour;
            AddUniverseWrapper(spy);
        }

        protected virtual void AddUniverseWrapper(Symbol symbol)
        {
            AddUniverse(Universe.ETF(symbol, UniverseSettings, FilterETFConstituents));
        }

        /// <summary>
        /// Filters ETF constituents
        /// </summary>
        /// <param name="constituents">ETF constituents</param>
        /// <returns>ETF constituent Symbols that we want to include in the algorithm</returns>
        public IEnumerable<Symbol> FilterETFConstituents(IEnumerable<ETFConstituentData> constituents)
        {
            var constituentData = constituents
                .Where(x => (x.Weight ?? 0m) >= 0.001m)
                .ToList();

            ConstituentData = constituentData;

            return constituentData
                .Select(x => x.Symbol)
                .ToList();
        }

        /// <summary>
        /// no-op for performance
        /// </summary>
        public override void OnData(Slice data)
        {
        }

        /// <summary>
        /// Alpha model for ETF constituents, where we generate insights based on the weighting
        /// of the ETF constituent
        /// </summary>
        private class ETFConstituentAlphaModel : IAlphaModel
        {
            public void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
            }

            /// <summary>
            /// Creates new insights based on constituent data and their weighting
            /// in their respective ETF
            /// </summary>
            public IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
            {
                var algo = (ETFConstituentUniverseFrameworkRegressionAlgorithm) algorithm;
                
                foreach (var constituent in algo.ConstituentData)
                {
                    if (!data.Bars.ContainsKey(constituent.Symbol) &&
                        !data.QuoteBars.ContainsKey(constituent.Symbol))
                    {
                        continue;
                    }
                    
                    var insightDirection = constituent.Weight != null && constituent.Weight >= 0.01m
                        ? InsightDirection.Up
                        : InsightDirection.Down;
                    
                    yield return new Insight(
                        algorithm.UtcTime,
                        constituent.Symbol,
                        TimeSpan.FromDays(1),
                        InsightType.Price,
                        insightDirection,
                        1 * (double)insightDirection,
                        1.0,
                        weight: (double)(constituent.Weight ?? 0));
                }
            }
        }

        /// <summary>
        /// Generates targets for ETF constituents, which will be set to the weighting
        /// of the constituent in their respective ETF
        /// </summary>
        private class ETFConstituentPortfolioModel : IPortfolioConstructionModel
        {
            private bool _hasAdded;
            
            /// <summary>
            /// Securities changed, detects if we've got new additions to the universe
            /// so that we don't try to trade every loop
            /// </summary>
            public void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                _hasAdded = changes.AddedSecurities.Count != 0;
            }

            /// <summary>
            /// Creates portfolio targets based on the insights provided to us by the alpha model.
            /// Emits portfolio targets setting the quantity to the weight of the constituent
            /// in its respective ETF.
            /// </summary>
            public IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
            {
                if (!_hasAdded)
                {
                    yield break;
                }

                foreach (var insight in insights)
                {
                    yield return new PortfolioTarget(insight.Symbol, (decimal) (insight.Weight ?? 0));
                    _hasAdded = false;
                }
            }
        }

        /// <summary>
        /// Executes based on ETF constituent weighting
        /// </summary>
        private class ETFConstituentExecutionModel : IExecutionModel
        {
            /// <summary>
            /// Liquidates if constituents have been removed from the universe
            /// </summary>
            public void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
            {
                foreach (var change in changes.RemovedSecurities)
                {
                    algorithm.Liquidate(change.Symbol);
                }
            }

            /// <summary>
            /// Creates orders for constituents that attempts to add
            /// the weighting of the constituent in our portfolio. The
            /// resulting algorithm portfolio weight might not be equal
            /// to the leverage of the ETF (1x, 2x, 3x, etc.)
            /// </summary>
            public void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
            {
                foreach (var target in targets)
                {
                    algorithm.SetHoldings(target.Symbol, target.Quantity);
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
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1905;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "3.252%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.525%"},
            {"Sharpe Ratio", "1.118"},
            {"Probabilistic Sharpe Ratio", "54.664%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.013"},
            {"Beta", "0.1"},
            {"Annual Standard Deviation", "0.018"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.526"},
            {"Tracking Error", "0.096"},
            {"Treynor Ratio", "0.203"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$1200000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.12%"},
            {"OrderListHash", "8e078b6ebb55270f9faa460ee56029a7"}
        };
    }
}
