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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Statistics
{
    /// <summary>
    /// Class in charge of calculating the Assets Under Management (AUM) Capacity values.
    /// Will use the sample values of the last year.
    /// </summary>
    /// <remarks>See https://www.quantconnect.com/forum/discussion/6194/insight-scoring-metric/p1 </remarks>
    public class AssetsUnderManagementCapacityManager
    {
        private readonly object _lock = new object();

        private readonly SecurityPortfolioManager _portfolio;
        private readonly ISubscriptionDataConfigProvider _subscriptionDataConfigProvider;

        private readonly Dictionary<Symbol, decimal> _maximumSymbolCapacity;
        private readonly RollingWindow<decimal> _historicalPortfolioCapacity;
        private DateTime _previousInputTime;

        /// <summary>
        /// Assets Under Management (AUM) Capacity
        /// </summary>
        public decimal AumCapacity =>
            _historicalPortfolioCapacity.Count == 0 ? 0 : _historicalPortfolioCapacity.Average();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetsUnderManagementCapacityManager"/> class
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="subscriptionDataConfigProvider">Provides access to registered <see cref="SubscriptionDataConfig"/></param>
        /// <param name="orderEventProvider">Provides access to the order events</param>
        public AssetsUnderManagementCapacityManager(
            SecurityPortfolioManager portfolio,
            ISubscriptionDataConfigProvider subscriptionDataConfigProvider,
            IOrderEventProvider orderEventProvider)
        {
            _portfolio = portfolio;
            _subscriptionDataConfigProvider = subscriptionDataConfigProvider;

            _maximumSymbolCapacity = new Dictionary<Symbol, decimal>();
            _historicalPortfolioCapacity = new RollingWindow<decimal>(30);
            _previousInputTime = DateTime.MinValue;

            orderEventProvider.NewOrderEvent += HandleNewOrderEvent;
        }

        /// <summary>
        /// Handles new order event:
        /// When dealing with a security with high resolution data, create a consolidator to compute the
        /// total trade volume capacity using a volume of the past 5 minutes. For low resolution, use the last bar.
        /// </summary>
        private void HandleNewOrderEvent(object sender, OrderEvent orderEvent)
        {
            var symbol = orderEvent.Symbol;

            var configs = _subscriptionDataConfigProvider.GetSubscriptionDataConfigs(symbol);
            if (configs.IsNullOrEmpty())
            {
                Log.Error($"AssetsUnderManagementCapacityManager: Could not find any SubscriptionDataConfig for {symbol}.");
                return;
            }

            var consolidator = default(IDataConsolidator);
            var hasHighResolution = false;
            var security = _portfolio.Securities[symbol];
            BaseData lastData = security.Cache.GetData<TradeBar>();

            // For high resolution data, create a consolidator that will compute
            // the AUM Capacity after a 5-minute period bar is closed
            foreach (var config in configs.Where(x => x.Resolution < Resolution.Hour))
            {
                hasHighResolution = true;

                if (config.Type.IsAssignableFrom(typeof(TradeBar)))
                {
                    consolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(5));
                }

                if (config.Type.IsAssignableFrom(typeof(Tick)) &&
                    config.TickType == TickType.Trade)
                {
                    consolidator = new TickConsolidator(TimeSpan.FromMinutes(5));
                    lastData = security.Cache.GetData<Tick>();
                }

                if (consolidator == null)
                {
                    continue;
                }

                // Warm up the consolidator to mark the begging of the period
                if (lastData != null)
                {
                    consolidator.Update(lastData);
                }

                config.Consolidators.Add(consolidator);
                consolidator.DataConsolidated += (s, data) => Update(consolidator, data, orderEvent.OrderId, security);
                return;
            }

            if (hasHighResolution)
            {
                Log.Error(
                    "AssetsUnderManagementCapacityManager.HandleNewOrderEvent: " +
                    $"Could not create consolidator for high resolution data because no trade data for {symbol} was found."
                );
                return;
            }

            // For low resolution, use the last data available
            if (lastData != null)
            {
                Update(null, lastData, orderEvent.OrderId, security);
            }
        }

        /// <summary>
        /// Updates the AUM Capacity with the latest data available after a order fill
        /// </summary>
        /// <param name="consolidator">Consolidator (if using high resolution data) to be removed after the update</param>
        /// <param name="data">Last price trade bar available or consolidated</param>
        /// <param name="orderId">Order fill ID</param>
        /// <param name="security">The security of the data</param>
        /// <remarks>
        /// The whole method body is locked, since order events can be triggered at any time in live mode
        /// </remarks>
        private void Update(IDataConsolidator consolidator, IBaseData data, int orderId, Security security)
        {
            lock (_lock)
            {
                var symbol = data.Symbol;

                var holdingsTurnover = 1m;
                var holdingsValue = security.Holdings.AbsoluteHoldingsValue;

                if (holdingsValue != 0)
                {
                    // Gets the order of the event to get its value in account currency
                    var order = _portfolio.Transactions.GetOrderById(orderId);
                    var orderValue = Math.Abs(order.GetValue(security));
                    holdingsTurnover = orderValue / holdingsValue;

                    if (holdingsTurnover == 0)
                    {
                        RemoveConsolidator(consolidator, symbol);
                        return;
                    }
                }

                var totalMarketDollarVolume = GetTotalMarketDollarVolume(data) * security.SymbolProperties.ContractMultiplier;
                var totalTradeVolumeCapacity = totalMarketDollarVolume / holdingsTurnover;

                // If it is a new day, we discard the previous day data
                var utcDate = data.EndTime.ConvertToUtc(security.Exchange.TimeZone).Date;
                if (_previousInputTime != utcDate)
                {
                    _previousInputTime = utcDate;
                    _maximumSymbolCapacity.Clear();
                    _historicalPortfolioCapacity.Add(0m);
                }

                // Updates AUM Capacity if there is new information or lower total trade volume capacity for the security
                decimal capacity;
                if (!_maximumSymbolCapacity.TryGetValue(symbol, out capacity) ||
                    totalTradeVolumeCapacity < capacity)
                {
                    _maximumSymbolCapacity[symbol] = totalTradeVolumeCapacity;
                    _historicalPortfolioCapacity[0] = _maximumSymbolCapacity.Sum(x => x.Value);
                }

                RemoveConsolidator(consolidator, symbol);
            }
        }

        /// <summary>
        /// Gets the total market volume which is a fraction of the current trade bar volume
        /// </summary>
        /// <param name="data">Last price trade bar available or consolidated</param>
        private static decimal GetTotalMarketDollarVolume(IBaseData data)
        {
            var tradeBar = (TradeBar) data;

            // Maximum percentage of market volume that is tradeable
            var factor = tradeBar.Period == Time.OneDay
                ? .025m
                : .050m;

            return factor * tradeBar.Volume * tradeBar.Price;
        }

        /// <summary>
        /// Remove consolidator for a given symbol
        /// </summary>
        /// <param name="consolidator">Consolidator to be removed</param>
        /// <param name="symbol">Symbol associated with the consolidator</param>
        private void RemoveConsolidator(IDataConsolidator consolidator, Symbol symbol)
        {
            if (consolidator == null)
            {
                return;
            }

            var configs = _subscriptionDataConfigProvider.GetSubscriptionDataConfigs(symbol);
            foreach (var config in configs)
            {
                if (config.Consolidators.Remove(consolidator))
                {
                    break;
                }
            }

            consolidator.DisposeSafely();
        }
    }
}