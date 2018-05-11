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
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template options framework algorithm uses framework components to define an algorithm
    /// that trades options.
    /// </summary>
    public class BasicTemplateOptionsFrameworkAlgorithm : QCAlgorithmFramework
    {
        private const string UnderlyingTicker = "GOOG";

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var equity = AddEquity(UnderlyingTicker);
            var option = AddOption(UnderlyingTicker);
            option.SetFilter(filter =>
            {
                return filter
                    // limit options contracts to a maximum of 180 days in the future
                    .Expiration(TimeSpan.Zero, TimeSpan.FromDays(180))
                    // select the latest expiring put contract
                    .Contracts(c =>
                    {
                        return c.Where(x => x.ID.OptionRight == OptionRight.Put)
                            .OrderByDescending(x => x.ID.Date)
                            .ThenBy(x => Math.Abs(filter.Underlying.Price - x.ID.StrikePrice))
                            .Take(1);
                    })
                    // forces the filter to execute only at market open and maintain the same
                    // list of contracts for the entire trading day.
                    .OnlyApplyFilterAtMarketOpen();
            });

            // set framework models
            SetUniverseSelection(new ManualUniverseSelectionModel(Securities.Keys));
            SetAlpha(new ConstantOptionContractAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new SingleSharePortofioConstructionModel());
            SetExecution(new PairedMarketAndMarketOnCloseExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
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

            public override IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
            {
                // limit generated predictions to option symbols
                return base.Update(algorithm, data)
                    .Where(insight => insight.Symbol.SecurityType == SecurityType.Option);
            }
        }

        /// <summary>
        /// Portoflio construction model that sets target quantities to 1.
        /// </summary>
        class SingleSharePortofioConstructionModel : IPortfolioConstructionModel
        {
            public IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithmFramework algorithm, Insight[] insights)
            {
                foreach (var insight in insights)
                {
                    yield return new PortfolioTarget(insight.Symbol, 1);
                }
            }

            public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
            {
                // no need to track anything here
            }
        }

        /// <summary>
        /// Execution model that submits a market order and a market on close order for each target.
        /// This prevents overnight holdings.
        /// </summary>
        class PairedMarketAndMarketOnCloseExecutionModel : IExecutionModel
        {
            public void Execute(QCAlgorithmFramework algorithm, IPortfolioTarget[] targets)
            {
                foreach (var target in targets)
                {
                    // submit both orders at the same time for the full target quantity
                    algorithm.MarketOrder(target.Symbol, target.Quantity);
                    algorithm.MarketOnCloseOrder(target.Symbol, -target.Quantity);
                }
            }

            public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
            {
                // no need to track anything here
            }
        }
    }
}