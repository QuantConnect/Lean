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

using System.Linq;
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Custom;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Alpha model that will handle adding and removing securities from the algorithm based on the current portfolio of the different alphas
    /// </summary>
    public sealed class AlphaStreamAlphaModule : AlphaModel
    {
        private Dictionary<Symbol, HashSet<Symbol>> _symbolsPerAlpha = new Dictionary<Symbol, HashSet<Symbol>>();

        /// <summary>
        /// Initialize new <see cref="AlphaStreamAlphaModule"/>
        /// </summary>
        public AlphaStreamAlphaModule(string name = null)
        {
            Name = name ?? "AlphaStreamAlphaModule";
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            foreach (var portfolioState in data.Get<PortfolioState>().Values)
            {
                ProcessPortfolioState(algorithm, portfolioState);
            }

            return Enumerable.Empty<Insight>();
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            changes.FilterCustomSecurities = false;
            foreach (var addedSecurity in changes.AddedSecurities)
            {
                if (addedSecurity.Symbol.IsCustomDataType<PortfolioState>())
                {
                    if (!_symbolsPerAlpha.ContainsKey(addedSecurity.Symbol))
                    {
                        _symbolsPerAlpha[addedSecurity.Symbol] = new HashSet<Symbol>();
                    }
                    // warmup alpha state, adding target securities
                    ProcessPortfolioState(algorithm, addedSecurity.Cache.GetData<PortfolioState>());
                }
            }

            algorithm.Log($"OnSecuritiesChanged: {changes}");
        }

        /// <summary>
        /// Will handle adding and removing securities from the algorithm based on the current portfolio of the different alphas
        /// </summary>
        private void ProcessPortfolioState(QCAlgorithm algorithm, PortfolioState portfolioState)
        {
            if (portfolioState == null)
            {
                return;
            }

            var alphaId = portfolioState.Symbol;
            if (!_symbolsPerAlpha.TryGetValue(alphaId, out var currentSymbols))
            {
                _symbolsPerAlpha[alphaId] = currentSymbols = new HashSet<Symbol>();
            }

            var newSymbols = new HashSet<Symbol>(currentSymbols.Count);
            foreach (var symbol in portfolioState.PositionGroups?.SelectMany(positionGroup => positionGroup.Positions).Select(state => state.Symbol) ?? Enumerable.Empty<Symbol>())
            {
                // only add it if it's not used by any alpha (already added check)
                if (newSymbols.Add(symbol) && !UsedBySomeAlpha(symbol))
                {
                    algorithm.AddSecurity(symbol,
                        resolution: algorithm.UniverseSettings.Resolution,
                        extendedMarketHours: algorithm.UniverseSettings.ExtendedMarketHours);
                }
            }
            _symbolsPerAlpha[alphaId] = newSymbols;

            foreach (var symbol in currentSymbols.Where(symbol => !UsedBySomeAlpha(symbol)))
            {
                algorithm.RemoveSecurity(symbol);
            }
        }

        private bool UsedBySomeAlpha(Symbol asset)
        {
            return _symbolsPerAlpha.Any(pair => pair.Value.Contains(asset));
        }
    }
}
