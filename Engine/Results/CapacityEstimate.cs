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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.Results
{
    public class CapacityEstimate
    {
        private readonly IAlgorithm _algorithm;
        private readonly Dictionary<Symbol, SymbolCapacity> CapacityBySymbol;
        private List<SymbolCapacity> _monitoredSymbolCapacity;
        private HashSet<SymbolCapacity> _monitoredSymbolCapacitySet;
        private DateTime _nextSnapshotDate;
        private TimeSpan _snapshotPeriod;

        /// <summary>
        /// The total capacity of the strategy at a point in time
        /// </summary>
        public decimal Capacity { get; private set; }

        /// <summary>
        /// Initializes an instance of the class.
        /// </summary>
        /// <param name="algorithm">Used to get data at the current time step and access the portfolio state</param>
        public CapacityEstimate(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
            CapacityBySymbol = new Dictionary<Symbol, SymbolCapacity>();
            _monitoredSymbolCapacity = new List<SymbolCapacity>();
            _monitoredSymbolCapacitySet = new HashSet<SymbolCapacity>();
            _snapshotPeriod = TimeSpan.FromDays(7);
            _nextSnapshotDate = _algorithm.StartDate + _snapshotPeriod;
        }

        /// <summary>
        /// Processes an order whenever it's encountered so that we can calculate the capacity
        /// </summary>
        /// <param name="order">Order to use to calculate capacity</param>
        public void OnOrderEvent(OrderEvent orderEvent)
        {
            SymbolCapacity symbolCapacity;
            if (!CapacityBySymbol.TryGetValue(orderEvent.Symbol, out symbolCapacity))
            {
                symbolCapacity = new SymbolCapacity(_algorithm, orderEvent.Symbol);
                CapacityBySymbol[orderEvent.Symbol] = symbolCapacity;
            }

            symbolCapacity.OnOrderEvent(orderEvent);
            if (_monitoredSymbolCapacitySet.Contains(symbolCapacity))
            {
                return;
            }

            _monitoredSymbolCapacity.Add(symbolCapacity);
            _monitoredSymbolCapacitySet.Add(symbolCapacity);
        }

        public void UpdateMarketCapacity()
        {
            if (_monitoredSymbolCapacity.Count == 0)
            {
                return;
            }

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
            if (utcDate >= _nextSnapshotDate && CapacityBySymbol.Count != 0)
            {
                var delistings = CapacityBySymbol.Values
                    .Where(s => s.Security.IsDelisted)
                    .ToList();

                foreach (var delisted in delistings)
                {
                    CapacityBySymbol.Remove(delisted.Security.Symbol);
                    _monitoredSymbolCapacity.Remove(delisted);
                    _monitoredSymbolCapacitySet.Remove(delisted);
                }

                _nextSnapshotDate = utcDate + _snapshotPeriod;

                var totalPortfolioValue = _algorithm.Portfolio.TotalPortfolioValue;
                var totalSaleVolume = CapacityBySymbol.Values
                    .Sum(s => s.SaleVolume);

                if (totalPortfolioValue == 0)
                {
                    return;
                }

                var smallestAsset = CapacityBySymbol.Values
                    .OrderBy(c => c.MarketCapacityDollarVolume)
                    .First();

                var days = (decimal)_snapshotPeriod.TotalDays;

                // When there is no trading, rely on the portfolio holdings
                var percentageOfSaleVolume = totalSaleVolume != 0
                    ? smallestAsset.SaleVolume / totalSaleVolume
                    : 0;

                var buyingPowerUsed = smallestAsset.Security.MarginModel.GetReservedBuyingPowerForPosition(new ReservedBuyingPowerForPositionParameters(smallestAsset.Security))
                    .AbsoluteUsedBuyingPower * smallestAsset.Security.Leverage;

                var percentageOfHoldings = buyingPowerUsed / totalPortfolioValue;

                var scalingFactor = Math.Max(percentageOfSaleVolume, percentageOfHoldings);
                var dailyMarketCapacityDollarVolume = smallestAsset.MarketCapacityDollarVolume / days;

                var newCapacity = scalingFactor == 0
                    ? Capacity
                    : dailyMarketCapacityDollarVolume / scalingFactor;

                if (Capacity == 0)
                {
                    Capacity = newCapacity;
                }
                else
                {
                    Capacity = (0.33m * newCapacity) + (Capacity * 0.66m);
                }

                foreach (var symbolCapacity in CapacityBySymbol.Values)
                {
                    symbolCapacity.Reset();
                }
            }
        }
    }
}
