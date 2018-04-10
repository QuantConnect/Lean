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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Execution model that submits orders while the current market prices is at least the configured number of standard
    /// deviations away from the mean in the favorable direction (below/above for buy/sell respectively)
    /// </summary>
    public class StandardDeviationExecutionModel : IExecutionModel
    {
        private readonly int _period;
        private readonly decimal _deviations;
        private readonly Resolution _resolution;
        private readonly PortfolioTargetCollection _targetsCollection;
        private readonly Dictionary<Symbol, SymbolData> _symbolData;

        /// <summary>
        /// Gets or sets the maximum order value in units of the account currency.
        /// This defaults to $20,000. For example, if purchasing a stock with a price
        /// of $100, then the maximum order size would be 200 shares.
        /// </summary>
        public decimal MaximumOrderValue { get; set; } = 20 * 1000;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardDeviationExecutionModel"/> class
        /// </summary>
        /// <param name="period">Period of the standard deviation indicator</param>
        /// <param name="deviations">The number of deviations away from the mean before submitting an order</param>
        /// <param name="resolution">The resolution of the STD and SMA indicators</param>
        public StandardDeviationExecutionModel(
            int period = 60,
            decimal deviations = 2m,
            Resolution resolution = Resolution.Minute
            )
        {
            _period = period;
            _deviations = deviations;
            _resolution = resolution;
            _targetsCollection = new PortfolioTargetCollection();
            _symbolData = new Dictionary<Symbol, SymbolData>();
        }

        /// <summary>
        /// Executes market orders if the standard deviation of price is more than the configured number of deviations
        /// in the favorable direction.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The portfolio targets</param>
        public void Execute(QCAlgorithmFramework algorithm, IPortfolioTarget[] targets)
        {
            _targetsCollection.AddRange(targets);

            foreach (var target in _targetsCollection)
            {
                var symbol = target.Symbol;

                // calculate remaining quantity to be ordered
                var unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target);

                // fetch our symbol data containing our STD/SMA indicators
                SymbolData data;
                if (!_symbolData.TryGetValue(symbol, out data))
                {
                    continue;
                }

                // ensure we're receiving price data before submitting orders
                if (data.Security.Price == 0m)
                {
                    continue;
                }

                // check order entry conditions
                if (data.STD.IsReady && PriceIsFavorable(data, unorderedQuantity))
                {
                    // get the maximum order size based on total order value
                    var maxOrderSize = OrderSizing.Value(data.Security, MaximumOrderValue);
                    var orderSize = Math.Min(maxOrderSize, Math.Abs(unorderedQuantity));

                    // round down to even lot size
                    orderSize -= orderSize % data.Security.SymbolProperties.LotSize;
                    if (orderSize != 0)
                    {
                        algorithm.MarketOrder(symbol, Math.Sign(unorderedQuantity) * orderSize);
                    }
                }

                // check to see if we're done with this target
                unorderedQuantity = OrderSizing.GetUnorderedQuantity(algorithm, target);
                if (unorderedQuantity == 0m)
                {
                    _targetsCollection.Remove(target.Symbol);
                }
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            var addedSymbols = new List<Symbol>();
            foreach (var added in changes.AddedSecurities)
            {
                // initialize new securities
                if (!_symbolData.ContainsKey(added.Symbol))
                {
                    var symbolData = new SymbolData(algorithm, added, _period, _resolution);
                    addedSymbols.Add(added.Symbol);
                    _symbolData[added.Symbol] = symbolData;
                }
            }

            if (addedSymbols.Count > 0)
            {
                // warmup our indicators by pushing history through the consolidators
                algorithm.History(addedSymbols, _period, _resolution)
                    .PushThroughConsolidators(symbol =>
                    {
                        SymbolData data;
                        return _symbolData.TryGetValue(symbol, out data) ? data.Consolidator : null;
                    });
            }

            foreach (var removed in changes.RemovedSecurities)
            {
                // clean up data from removed securities
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
        /// Determines if the current price is more than the configured number of standard deviations
        /// away from the mean in the favorable direction.
        /// </summary>
        private bool PriceIsFavorable(SymbolData data, decimal unorderedQuantity)
        {
            var deviations = _deviations * data.STD;
            if (unorderedQuantity > 0)
            {
                var price = data.Security.BidPrice == 0
                    ? data.Security.Price
                    : data.Security.BidPrice;

                if (price < data.SMA - deviations)
                {
                    return true;
                }
            }
            else
            {
                var price = data.Security.AskPrice == 0
                    ? data.Security.AskPrice
                    : data.Security.Price;

                if (price > data.SMA + deviations)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if it's safe to remove the associated symbol data
        /// </summary>
        private bool IsSafeToRemove(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            // confirm the security isn't currently a member of any universe
            return !algorithm.UniverseManager.Any(kvp => kvp.Value.ContainsMember(symbol));
        }

        class SymbolData
        {
            public Security Security { get; }
            public StandardDeviation STD { get; }
            public SimpleMovingAverage SMA { get; }
            public IDataConsolidator Consolidator { get; }

            public SymbolData(QCAlgorithmFramework algorithm, Security security, int period, Resolution resolution)
            {
                Security = security;
                Consolidator = algorithm.ResolveConsolidator(security.Symbol, resolution);
                var smaName = algorithm.CreateIndicatorName(security.Symbol, "SMA" + period, resolution);
                SMA = new SimpleMovingAverage(smaName, period);
                var stdName = algorithm.CreateIndicatorName(security.Symbol, "STD" + period, resolution);
                STD = new StandardDeviation(stdName, period);

                algorithm.SubscriptionManager.AddConsolidator(security.Symbol, Consolidator);
                Consolidator.DataConsolidated += (sender, consolidated) =>
                {
                    SMA.Update(consolidated.EndTime, consolidated.Value);
                    STD.Update(consolidated.EndTime, consolidated.Value);
                };
            }
        }
    }
}
