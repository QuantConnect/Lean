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
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Execution
{
    /// <summary>
    /// Execution model that submits orders while the current market price is more favorable that the current volume weighted average price.
    /// </summary>
    public class VolumeWeightedAveragePriceExecutionModel : IExecutionModel
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
        public void Execute(QCAlgorithmFramework algorithm, IPortfolioTarget[] targets)
        {
            // update the complete set of portfolio targets with the new targets
            _targetsCollection.AddRange(targets);

            foreach (var target in _targetsCollection)
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

                // ensure we're receiving price data before submitting orders
                if (data.Security.Price == 0m)
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
        private bool IsSafeToRemove(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            // confirm the security isn't currently a member of any universe
            return !algorithm.UniverseManager.Any(kvp => kvp.Value.ContainsMember(symbol));
        }

        /// <summary>
        /// Determines if the current price is better than VWAP
        /// </summary>
        private bool PriceIsFavorable(SymbolData data, decimal unorderedQuantity)
        {
            var vwap = data.VWAP;
            if (unorderedQuantity > 0)
            {
                var price = data.Security.BidPrice == 0
                    ? data.Security.Price
                    : data.Security.BidPrice;

                if (price < vwap)
                {
                    return true;
                }
            }
            else
            {
                var price = data.Security.AskPrice == 0
                    ? data.Security.AskPrice
                    : data.Security.Price;

                if (price > vwap)
                {
                    return true;
                }
            }

            return false;
        }

        private class SymbolData
        {
            public Security Security { get; }
            public IntradayVwap VWAP { get; }
            public IDataConsolidator Consolidator { get; }

            public SymbolData(QCAlgorithmFramework algorithm, Security security)
            {
                Security = security;
                Consolidator = algorithm.ResolveConsolidator(security.Symbol, security.Resolution);
                var name = algorithm.CreateIndicatorName(security.Symbol, "VWAP", security.Resolution);
                VWAP = new IntradayVwap(name);

                algorithm.RegisterIndicator(security.Symbol, VWAP, Consolidator, bd => (BaseData) bd);
            }
        }

        /// <summary>
        /// Defines the canonical intraday VWAP indicator
        /// </summary>
        public class IntradayVwap : IndicatorBase<BaseData>
        {
            private DateTime _lastDate;
            private decimal _sumOfVolume;
            private decimal _sumOfPriceTimesVolume;

            /// <summary>
            /// Gets a flag indicating when this indicator is ready and fully initialized
            /// </summary>
            public override bool IsReady => _sumOfVolume > 0;

            /// <summary>
            /// Initializes a new instance of the <see cref="IntradayVwap"/> class
            /// </summary>
            /// <param name="name">The name of the indicator</param>
            public IntradayVwap(string name)
                : base(name)
            {
            }

            /// <summary>
            /// Computes the new VWAP
            /// </summary>
            protected override IndicatorResult ValidateAndComputeNextValue(BaseData input)
            {
                decimal volume, averagePrice;
                if (!TryGetVolumeAndAveragePrice(input, out volume, out averagePrice))
                {
                    return new IndicatorResult(0, IndicatorStatus.InvalidInput);
                }

                // reset vwap on daily boundaries
                if (_lastDate != input.EndTime.Date)
                {
                    _sumOfVolume = 0m;
                    _sumOfPriceTimesVolume = 0m;
                    _lastDate = input.EndTime.Date;
                }

                // running totals for Σ PiVi / Σ Vi
                _sumOfVolume += volume;
                _sumOfPriceTimesVolume += averagePrice * volume;

                if (_sumOfVolume == 0m)
                {
                    // if we have no trade volume then use the current price as VWAP
                    return input.Value;
                }

                return _sumOfPriceTimesVolume / _sumOfVolume;
            }

            /// <summary>
            /// Computes the next value of this indicator from the given state.
            /// NOTE: This must be overriden since it's abstract in the base, but
            /// will never be invoked since we've override the validate method above.
            /// </summary>
            /// <param name="input">The input given to the indicator</param>
            /// <returns>A new value for this indicator</returns>
            protected override decimal ComputeNextValue(BaseData input)
            {
                throw new NotImplementedException($"{nameof(IntradayVwap)}.{nameof(ComputeNextValue)} should never be invoked.");
            }

            /// <summary>
            /// Determines the volume and price to be used for the current input in the VWAP computation
            /// </summary>
            protected bool TryGetVolumeAndAveragePrice(BaseData input, out decimal volume, out decimal averagePrice)
            {
                var tick = input as Tick;

                if (tick?.TickType == TickType.Trade)
                {
                    volume = tick.Quantity;
                    averagePrice = tick.LastPrice;
                    return true;
                }

                var tradeBar = input as TradeBar;
                if (tradeBar?.IsFillForward == false)
                {
                    volume = tradeBar.Volume;
                    averagePrice = (tradeBar.High + tradeBar.Low + tradeBar.Close) / 3m;
                    return true;
                }

                volume = 0;
                averagePrice = 0;
                return false;
            }
        }
    }
}