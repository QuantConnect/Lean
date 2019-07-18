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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Execution model that submits orders while the current market price is more favorable that the current volume weighted average price.
    /// </summary>
    public class VolumeWeightedAveragePriceExecutionModel : ExecutionModel
    {
        private readonly PortfolioTargetCollection _targetsCollection = new PortfolioTargetCollection();
        private readonly Dictionary<Symbol, SymbolData> _symbolData = new Dictionary<Symbol, SymbolData>();

        /// <summary>
        /// Gets or sets the maximum order quantity as a percentage of the current bar's volume.
        /// This defaults to 0.01m = 1%. For example, if the current bar's volume is 100, then
        /// the maximum order size would equal 1 share.
        /// </summary>
        public decimal MaximumOrderQuantityPercentVolume { get; set; } = 0.01m;

        /// <summary>
        /// Submit orders for the specified portolio targets.
        /// This model is free to delay or spread out these orders as it sees fit
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets to be ordered</param>
        public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            // update the complete set of portfolio targets with the new targets
            _targetsCollection.AddRange(targets);

            // for performance we check count value, OrderByMarginImpact and ClearFulfilled are expensive to call
            if (_targetsCollection.Count > 0)
            {
                foreach (var target in _targetsCollection.OrderByMarginImpact(algorithm))
                {
                    var symbol = target.Symbol;

                    // calculate remaining quantity to be ordered
                    var unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target);

                    // fetch our symbol data containing our VWAP indicator
                    SymbolData data;
                    if (!_symbolData.TryGetValue(symbol, out data))
                    {
                        continue;
                    }

                    // check order entry conditions
                    if (PriceIsFavorable(data, unorderedQuantity))
                    {
                        // get the maximum order size based on a percentage of current volume
                        var maxOrderSize = OrderSizing.PercentVolume(data.Security, MaximumOrderQuantityPercentVolume);
                        var orderSize = Math.Min(maxOrderSize, Math.Abs(unorderedQuantity));

                        // round down to even lot size
                        orderSize -= orderSize % data.Security.SymbolProperties.LotSize;
                        if (orderSize != 0)
                        {
                            algorithm.MarketOrder(data.Security.Symbol, Math.Sign(unorderedQuantity) * orderSize);
                        }
                    }
                }

                _targetsCollection.ClearFulfilled(algorithm);
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                if (!_symbolData.ContainsKey(added.Symbol))
                {
                    _symbolData[added.Symbol] = new SymbolData(algorithm, added);
                }
            }

            foreach (var removed in changes.RemovedSecurities)
            {
                // clean up removed security data
                SymbolData data;
                if (_symbolData.TryGetValue(removed.Symbol, out data))
                {
                    if (IsSafeToRemove(algorithm, removed.Symbol))
                    {
                        _symbolData.Remove(removed.Symbol);
                        algorithm.SubscriptionManager.RemoveConsolidator(removed.Symbol, data.Consolidator);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if it's safe to remove the associated symbol data
        /// </summary>
        protected virtual bool IsSafeToRemove(QCAlgorithm algorithm, Symbol symbol)
        {
            // confirm the security isn't currently a member of any universe
            return !algorithm.UniverseManager.Any(kvp => kvp.Value.ContainsMember(symbol));
        }

        /// <summary>
        /// Determines if the current price is better than VWAP
        /// </summary>
        protected virtual bool PriceIsFavorable(SymbolData data, decimal unorderedQuantity)
        {
            if (unorderedQuantity > 0)
            {
                if (data.Security.BidPrice < data.VWAP)
                {
                    return true;
                }
            }
            else
            {
                if (data.Security.AskPrice > data.VWAP)
                {
                    return true;
                }
            }

            return false;
        }

        protected class SymbolData
        {
            public Security Security { get; }
            public IntradayVwap VWAP { get; }
            public IDataConsolidator Consolidator { get; }

            public SymbolData(QCAlgorithm algorithm, Security security)
            {
                Security = security;
                Consolidator = algorithm.ResolveConsolidator(security.Symbol, security.Resolution);
                var name = algorithm.CreateIndicatorName(security.Symbol, "VWAP", security.Resolution);
                VWAP = new IntradayVwap(name);

                algorithm.RegisterIndicator(security.Symbol, VWAP, Consolidator, bd => (BaseData) bd);
            }
        }
    }
}