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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Equal weighting alpha streams portfolio construction model that will generate aggregated security targets taking into account all the alphas positions
    /// and an equal weighting factor for each alpha, which is also factored by the relation of the alphas portfolio value and the current algorithms portfolio value,
    /// overriding <see cref="GetAlphaWeight"/> allows custom weighting implementations
    /// </summary>
    public class EqualWeightingAlphaStreamsPortfolioConstructionModel : IPortfolioConstructionModel
    {
        private bool _rebalance;
        private readonly Queue<Symbol> _removedSymbols = new Queue<Symbol>();
        private Dictionary<Symbol, decimal> _unitQuantity = new Dictionary<Symbol, decimal>();
        private Dictionary<Symbol, PortfolioTarget> _targetsPerSymbol = new Dictionary<Symbol, PortfolioTarget>();
        private Dictionary<Symbol, Dictionary<Symbol, PortfolioTarget>> _targetsPerSymbolPerAlpha = new Dictionary<Symbol, Dictionary<Symbol, PortfolioTarget>>();

        /// <summary>
        /// Access the last portfolio state per alpha
        /// </summary>
        protected Dictionary<Symbol, AlphaStreamsPortfolioState> LastPortfolioPerAlpha = new Dictionary<Symbol, AlphaStreamsPortfolioState>();

        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portfolio targets from</param>
        /// <returns>An enumerable of portfolio targets to be sent to the execution model</returns>
        public IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            while (_removedSymbols.TryDequeue(out var removedSymbol))
            {
                yield return new PortfolioTarget(removedSymbol, 0);
            }

            var updatedTargets = new Dictionary<Symbol, IPortfolioTarget>();
            foreach (var portfolioState in algorithm.CurrentSlice?.Get<AlphaStreamsPortfolioState>().Values ?? Enumerable.Empty<AlphaStreamsPortfolioState>())
            {
                if (!_rebalance)
                {
                    foreach (var portfolioTarget in ProcessPortfolioState(portfolioState, algorithm))
                    {
                        updatedTargets[portfolioTarget.Symbol] = portfolioTarget;
                    }
                }
                // keep the last state per alpha
                LastPortfolioPerAlpha[portfolioState.Symbol] = portfolioState;
            }

            // if an alpha is removed or added we just rebalance all the targets because the weight changes of each alpha
            if (_rebalance)
            {
                foreach (var portfolioTarget in LastPortfolioPerAlpha.Values.SelectMany(portfolioState => ProcessPortfolioState(portfolioState, algorithm)))
                {
                    updatedTargets[portfolioTarget.Symbol] = portfolioTarget;
                }
                _rebalance = false;
            }

            foreach (var portfolioTarget in updatedTargets.Values)
            {
                yield return portfolioTarget;
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            changes.FilterCustomSecurities = false;

            foreach (var security in changes.RemovedSecurities)
            {
                if (security.Type != SecurityType.Base)
                {
                    _removedSymbols.Enqueue(security.Symbol);
                }
                else if (IsAlphaStreamsPortfolioState(security.Symbol))
                {
                    _rebalance = true;
                    _targetsPerSymbolPerAlpha.Remove(security.Symbol);
                    LastPortfolioPerAlpha.Remove(security.Symbol);
                }
            }
            foreach (var security in changes.AddedSecurities)
            {
                if (security.Type == SecurityType.Base && IsAlphaStreamsPortfolioState(security.Symbol))
                {
                    _rebalance = true;
                    _targetsPerSymbolPerAlpha[security.Symbol] = new Dictionary<Symbol, PortfolioTarget>();
                }
            }
        }

        /// <summary>
        /// Determines the portfolio weight to give a specific alpha. Default implementation just returns equal weighting
        /// </summary>
        protected virtual decimal GetAlphaWeight(AlphaStreamsPortfolioState portfolioState, QCAlgorithm algorithm)
        {
            if (portfolioState.TotalPortfolioValue == 0)
            {
                return 0;
            }
            var equalWeightFactor = 1m / _targetsPerSymbolPerAlpha.Count;
            return (algorithm.Portfolio.TotalPortfolioValue * equalWeightFactor) / portfolioState.TotalPortfolioValue;
        }

        private IEnumerable<IPortfolioTarget> ProcessPortfolioState(AlphaStreamsPortfolioState portfolioState, QCAlgorithm algorithm)
        {
            var alphaId = portfolioState.Symbol;

            if(!_targetsPerSymbolPerAlpha.TryGetValue(alphaId, out var ourExistingTargets))
            {
                _targetsPerSymbolPerAlpha[alphaId] = ourExistingTargets = new Dictionary<Symbol, PortfolioTarget>();
            }

            var alphaWeightFactor = GetAlphaWeight(portfolioState, algorithm);
            // first we create all the new aggregated targets for the provided portfolio state
            var newTargets = new Dictionary<Symbol, PortfolioTarget>();
            foreach (var positionGroup in portfolioState.PositionGroups ?? Enumerable.Empty<PositionGroupState>())
            {
                foreach (var position in positionGroup.Positions)
                {
                    // let's keep the unit quantity so we can round by it
                    _unitQuantity[position.Symbol] = position.UnitQuantity;

                    newTargets.TryGetValue(position.Symbol, out var existingAggregatedTarget);
                    var quantity = position.Quantity * alphaWeightFactor + (existingAggregatedTarget?.Quantity ?? 0);
                    newTargets[position.Symbol] = new PortfolioTarget(position.Symbol, quantity.DiscretelyRoundBy(position.UnitQuantity, MidpointRounding.ToZero));
                }
            }

            // We adjust the new targets based on what we already have:
            // - We add any existing targets if any -> other alphas
            //    - But we deduct our own existing target from it if any (previous state)
            foreach (var ourNewTarget in newTargets.Values)
            {
                var symbol = ourNewTarget.Symbol;
                var newAggregatedTarget = ourNewTarget;
                if (_targetsPerSymbol.TryGetValue(symbol, out var existingAggregatedTarget))
                {
                    ourExistingTargets.TryGetValue(symbol, out var ourExistingTarget);

                    var quantity = existingAggregatedTarget.Quantity + ourNewTarget.Quantity - (ourExistingTarget?.Quantity ?? 0);
                    newAggregatedTarget = new PortfolioTarget(symbol, quantity.DiscretelyRoundBy(_unitQuantity[symbol], MidpointRounding.ToZero));
                }

                ourExistingTargets[symbol] = ourNewTarget;
                _targetsPerSymbol[symbol] = newAggregatedTarget;
                yield return newAggregatedTarget;
            }

            // We adjust existing targets for symbols that got removed from this alpha
            foreach (var removedTarget in ourExistingTargets.Values.Where(target => !newTargets.ContainsKey(target.Symbol)))
            {
                var symbol = removedTarget.Symbol;
                var newAggregatedTarget = removedTarget;
                if (_targetsPerSymbol.TryGetValue(symbol, out var existingAggregatedTarget))
                {
                    var quantity = existingAggregatedTarget.Quantity - removedTarget.Quantity;
                    newAggregatedTarget = new PortfolioTarget(symbol, quantity.DiscretelyRoundBy(_unitQuantity[symbol], MidpointRounding.ToZero));
                }

                ourExistingTargets.Remove(symbol);
                if (newAggregatedTarget.Quantity != 0)
                {
                    _targetsPerSymbol[symbol] = newAggregatedTarget;
                }
                else
                {
                    _targetsPerSymbol.Remove(symbol);
                }

                yield return newAggregatedTarget;
            }
        }

        private static bool IsAlphaStreamsPortfolioState(Symbol symbol)
        {
            return symbol.ID.Symbol.TryGetCustomDataType(out var type) && type.Equals(nameof(AlphaStreamsPortfolioState), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
