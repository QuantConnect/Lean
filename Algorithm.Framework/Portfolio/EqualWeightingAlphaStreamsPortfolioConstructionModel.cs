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
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// 
    /// </summary>
    public class EqualWeightingAlphaStreamsPortfolioConstructionModel : IPortfolioConstructionModel
    {
        private bool _rebalance;
        private readonly Queue<Symbol> _removedSymbols = new Queue<Symbol>();
        private Dictionary<Symbol, decimal> _unitQuantity = new Dictionary<Symbol, decimal>();
        private Dictionary<Symbol, PortfolioTarget> _targetsPerSymbol = new Dictionary<Symbol, PortfolioTarget>();
        private Dictionary<Symbol, AlphaStreamsPortfolioState> _lastPortfolioPerAlpha = new Dictionary<Symbol, AlphaStreamsPortfolioState>();
        private Dictionary<Symbol, Dictionary<Symbol, PortfolioTarget>> _targetsPerSymbolPerAlpha = new Dictionary<Symbol, Dictionary<Symbol, PortfolioTarget>>();

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

            foreach (var portfolioState in algorithm.CurrentSlice?.Get<AlphaStreamsPortfolioState>().Values ?? Enumerable.Empty<AlphaStreamsPortfolioState>())
            {
                if (!_rebalance)
                {
                    foreach (var portfolioTarget in ProcessPortfolioState(portfolioState, algorithm.Portfolio.TotalPortfolioValue))
                    {
                        yield return portfolioTarget;
                    }
                }
                // keep the last state per alpha
                _lastPortfolioPerAlpha[portfolioState.Symbol] = portfolioState;
            }

            // if an alpha is removed or added we just rebalance all the targets
            if (_rebalance)
            {
                foreach (var portfolioTarget in _lastPortfolioPerAlpha.Values.SelectMany(portfolioState => ProcessPortfolioState(portfolioState, algorithm.Portfolio.TotalPortfolioValue)))
                {
                    yield return portfolioTarget;
                }

                _rebalance = false;
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
                    _lastPortfolioPerAlpha.Remove(security.Symbol);
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
        protected virtual decimal GetAlphaWeight(Symbol alphaId)
        {
            return 1m / _targetsPerSymbolPerAlpha.Count;
        }

        private IEnumerable<IPortfolioTarget> ProcessPortfolioState(AlphaStreamsPortfolioState portfolioState, decimal totalPortfolioValue)
        {
            var alphaId = portfolioState.Symbol;

            var portfolioValueFactor = (totalPortfolioValue * GetAlphaWeight(alphaId)) / portfolioState.TotalPortfolioValue;
            // first we create all the new aggregated targets for the current portfolio state
            var newTargets = new Dictionary<Symbol, PortfolioTarget>();
            foreach (var positionGroup in portfolioState.PositionGroups ?? Enumerable.Empty<PositionGroupState>())
            {
                foreach (var position in positionGroup.Positions)
                {
                    // let's keep the unit quantity so we can round by it
                    _unitQuantity[position.Symbol] = position.UnitQuantity;

                    newTargets.TryGetValue(position.Symbol, out var existingAggregatedTarget);
                    var quantity = position.Quantity * portfolioValueFactor + (existingAggregatedTarget?.Quantity ?? 0);
                    newTargets[position.Symbol] = new PortfolioTarget(position.Symbol, quantity.DiscretelyRoundBy(position.UnitQuantity, MidpointRounding.ToZero));
                }
            }

            // We adjust the new targets:
            // - We add any existing targets if any -> other alphas
            //    - But we deduct our own existing target from it if any
            _targetsPerSymbolPerAlpha.TryGetValue(alphaId, out var ourExistingTargets);
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

            // We adjust targets for symbols that got removed from this alpha
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
            var index = symbol.ID.Symbol.LastIndexOf('.');
            return index != -1 && symbol.ID.Symbol.Length > index + 1
                && symbol.ID.Symbol.Substring(index + 1).Equals(nameof(AlphaStreamsPortfolioState), StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
