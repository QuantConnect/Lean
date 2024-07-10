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
 *
*/

using System;
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// Estimates dollar volume capacity of algorithm (in account currency) using all Symbols in the portfolio.
    /// </summary>
    /// <remarks>
    /// Any mention of dollar volume is volume in account currency, but "dollar volume" is used
    /// to maintain consistency with financial terminology and our use
    /// case of having alphas measured capacity be in USD.
    /// </remarks>
    public class CapacityEstimate
    {
        private readonly IAlgorithm _algorithm;
        private readonly Dictionary<Symbol, SymbolCapacity> _capacityBySymbol;
        private List<SymbolCapacity> _monitoredSymbolCapacity;
        // We use multiple collections to avoid having to perform an O(n) lookup whenever
        // we're wanting to check whether a particular SymbolData instance is being "monitored",
        // but still want to preserve indexing via an integer index
        // (monitored meaning it is currently aggregating market dollar volume for its capacity calculation).
        // For integer indexing, we use the List above, v.s. for lookup we use this HashSet.
        private HashSet<SymbolCapacity> _monitoredSymbolCapacitySet;
        private DateTime _nextSnapshotDate;
        private TimeSpan _snapshotPeriod;
        private Symbol _smallestAssetSymbol;

        /// <summary>
        /// Private capacity member, We wrap this value type because it's being
        /// read and written by multiple threads.
        /// </summary>
        private ReferenceWrapper<decimal> _capacity;

        /// <summary>
        /// The total capacity of the strategy at a point in time
        /// </summary>
        public decimal Capacity
        {
            // Round our capacity to the nearest 1000
            get => _capacity.Value.DiscretelyRoundBy(1000.00m);
            private set => _capacity = new ReferenceWrapper<decimal>(value);
        }

        /// <summary>
        /// Provide a reference to the lowest capacity symbol used in scaling down the capacity for debugging.
        /// </summary>
        public Symbol LowestCapacityAsset => _smallestAssetSymbol; 

        /// <summary>
        /// Initializes an instance of the class.
        /// </summary>
        /// <param name="algorithm">Used to get data at the current time step and access the portfolio state</param>
        public CapacityEstimate(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
            _capacityBySymbol = new Dictionary<Symbol, SymbolCapacity>();
            _monitoredSymbolCapacity = new List<SymbolCapacity>();
            _monitoredSymbolCapacitySet = new HashSet<SymbolCapacity>();
            // Set the minimum snapshot period to one day, but use algorithm start/end if the algo runtime is less than seven days
            _snapshotPeriod = TimeSpan.FromDays(Math.Max(Math.Min((_algorithm.EndDate - _algorithm.StartDate).TotalDays - 1, 7), 1)); 
            _nextSnapshotDate = _algorithm.StartDate + _snapshotPeriod;
            _capacity = new ReferenceWrapper<decimal>(0);
        }

        /// <summary>
        /// Processes an order whenever it's encountered so that we can calculate the capacity
        /// </summary>
        /// <param name="orderEvent">Order event to use to calculate capacity</param>
        public void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled && orderEvent.Status != OrderStatus.PartiallyFilled)
            {
                return;
            }

            SymbolCapacity symbolCapacity;
            if (!_capacityBySymbol.TryGetValue(orderEvent.Symbol, out symbolCapacity))
            {
                symbolCapacity = new SymbolCapacity(_algorithm, orderEvent.Symbol);
                _capacityBySymbol[orderEvent.Symbol] = symbolCapacity;
            }

            symbolCapacity.OnOrderEvent(orderEvent);
            if (_monitoredSymbolCapacitySet.Contains(symbolCapacity))
            {
                return;
            }

            _monitoredSymbolCapacity.Add(symbolCapacity);
            _monitoredSymbolCapacitySet.Add(symbolCapacity);
        }

        #pragma warning disable CS1574
        /// <summary>
        /// Updates the market capacity for any Symbols that require a market update.
        /// Sometimes, after the specified <seealso cref="_snapshotPeriod"/>, we
        /// take a "snapshot" (point-in-time capacity) of the portfolio's capacity.
        ///
        /// This result will be written into the Algorithm Statistics via the <see cref="BacktestingResultHandler"/>
        /// </summary>
        #pragma warning restore CS1574
        public void UpdateMarketCapacity(bool forceProcess)
        {
            for (var i = _monitoredSymbolCapacity.Count - 1; i >= 0; --i)
            {
                var capacity = _monitoredSymbolCapacity[i];
                if (capacity.UpdateMarketCapacity())
                {
                    _monitoredSymbolCapacity.RemoveAt(i);
                    _monitoredSymbolCapacitySet.Remove(capacity);
                }
            }

            var utcDate = _algorithm.UtcTime.Date;
            if (forceProcess || utcDate >= _nextSnapshotDate && _capacityBySymbol.Count != 0)
            {
                var totalPortfolioValue = _algorithm.Portfolio.TotalPortfolioValue;
                var totalSaleVolume = _capacityBySymbol.Values
                    .Sum(s => s.SaleVolume);

                if (totalPortfolioValue == 0 || _capacityBySymbol.Count == 0)
                {
                    return;
                }

                var smallestAsset = _capacityBySymbol.Values
                    .OrderBy(c => c.MarketCapacityDollarVolume)
                    .First();

                _smallestAssetSymbol = smallestAsset.Security.Symbol;

                // When there is no trading, rely on the portfolio holdings
                var percentageOfSaleVolume = totalSaleVolume != 0
                    ? smallestAsset.SaleVolume / totalSaleVolume
                    : 0;

                var buyingPowerUsed = smallestAsset.Security.MarginModel.GetReservedBuyingPowerForPosition(new ReservedBuyingPowerForPositionParameters(smallestAsset.Security))
                    .AbsoluteUsedBuyingPower * smallestAsset.Security.Leverage;

                var percentageOfHoldings = buyingPowerUsed / totalPortfolioValue;

                var scalingFactor = Math.Max(percentageOfSaleVolume, percentageOfHoldings);
                var dailyMarketCapacityDollarVolume = smallestAsset.MarketCapacityDollarVolume / smallestAsset.Trades;

                var newCapacity = scalingFactor == 0
                    ? _capacity.Value
                    : dailyMarketCapacityDollarVolume / scalingFactor;

                // Weight our capacity based on previous value if we have one
                if (_capacity.Value != 0)
                {
                    newCapacity = (0.33m * newCapacity) + (_capacity.Value * 0.66m);
                }

                // Set our new capacity value
                Capacity = newCapacity;

                foreach (var capacity in _capacityBySymbol.Select(pair => pair.Value).ToList())
                {
                    if (!capacity.ShouldRemove())
                    {
                        capacity.Reset();
                        continue;
                    }

                    // we remove non invested and non tradable (delisted, deselected) securities this will allow the 'smallestAsset'
                    // to be changing between snapshots, and avoid the collections to grow
                    _capacityBySymbol.Remove(capacity.Security.Symbol);
                    _monitoredSymbolCapacity.Remove(capacity);
                    _monitoredSymbolCapacitySet.Remove(capacity);
                }

                _nextSnapshotDate = utcDate + _snapshotPeriod;
            }
        }
    }
}
