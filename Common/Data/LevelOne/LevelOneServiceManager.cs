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
using System.Threading;
using QuantConnect.Logging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Util;

namespace QuantConnect.Data.LevelOne
{
    /// <summary>
    /// Manages subscriptions and real-time updates for multiple <see cref="LevelOneMarketData"/> instances.
    /// Facilitates routing of quote and trade data to a shared <see cref="IDataAggregator"/> in a thread-safe manner.
    /// </summary>
    public sealed class LevelOneServiceManager : IDisposable
    {
        /// <summary>
        /// The shared data aggregator that receives all tick updates from subscribed symbols.
        /// </summary>
        private readonly IDataAggregator _dataAggregator;

        /// <summary>
        /// Synchronization lock used to ensure thread safety during updates.
        /// </summary>
        private readonly Lock _lock = new();

        /// <summary>
        /// Maps subscribed symbols to their corresponding <see cref="LevelOneMarketData"/> instances.
        /// </summary>
        private readonly ConcurrentDictionary<Symbol, LevelOneMarketData> _levelOneServiceBySymbol = new();

        /// <summary>
        /// Internal subscription manager used to delegate low-level subscribe/unsubscribe logic.
        /// </summary>
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Gets whether there are no active subscriptions.
        /// </summary>
        public bool IsEmpty => _levelOneServiceBySymbol.IsEmpty;

        /// <summary>
        /// Gets the number of currently subscribed symbols.
        /// </summary>
        public int Count => _levelOneServiceBySymbol.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelOneServiceManager"/> class.
        /// </summary>
        /// <param name="dataAggregator">The aggregator to which all tick data will be published.</param>
        /// <param name="subscribeCallback">Delegate used to perform symbol subscription logic.</param>
        /// <param name="unsubscribeCallback">Delegate used to perform symbol unsubscription logic.</param>
        public LevelOneServiceManager(IDataAggregator dataAggregator, Func<IEnumerable<Symbol>, TickType, bool> subscribeCallback, Func<IEnumerable<Symbol>, TickType, bool> unsubscribeCallback)
        {
            _dataAggregator = dataAggregator;

            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager()
            {
                SubscribeImpl = subscribeCallback,
                UnsubscribeImpl = unsubscribeCallback
            };
        }

        /// <summary>
        /// Subscribes to the specified symbol based on the given <see cref="SubscriptionDataConfig"/>.
        /// </summary>
        /// <param name="dataConfig">The subscription configuration containing symbol and type information.</param>
        public void Subscribe(SubscriptionDataConfig dataConfig)
        {
            _levelOneServiceBySymbol[dataConfig.Symbol] = new(dataConfig.Symbol, BaseDataReceived);
            _subscriptionManager.Subscribe(dataConfig);
        }

        /// <summary>
        /// Unsubscribes from the specified symbol and removes its associated service instance.
        /// </summary>
        /// <param name="dataConfig">The subscription configuration used for unsubscription.</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            if (_levelOneServiceBySymbol.TryRemove(dataConfig.Symbol, out var levelOneService))
            {
                levelOneService.BaseDataReceived -= BaseDataReceived;
            }

            _subscriptionManager.Unsubscribe(dataConfig);
        }

        /// <summary>
        /// Handles incoming quote data for a symbol.
        /// Deduplicates updates and routes changes to the relevant <see cref="LevelOneMarketData"/> instance.
        /// </summary>
        /// <param name="symbol">The symbol for which quote data is received.</param>
        /// <param name="quoteDateTimeUtc">The UTC timestamp of the quote.</param>
        /// <param name="bidPrice">The bid price.</param>
        /// <param name="bidSize">The size at the bid price.</param>
        /// <param name="askPrice">The ask price.</param>
        /// <param name="askSize">The size at the ask price.</param>
        public void HandleQuote(Symbol symbol, DateTime quoteDateTimeUtc, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            if (_levelOneServiceBySymbol.TryGetValue(symbol, out var levelOneService) && levelOneService != null)
            {
                if (levelOneService.BestAskPrice == askPrice && levelOneService.BestAskSize == askSize
                && levelOneService.BestBidPrice == bidPrice && levelOneService.BestBidSize == bidSize)
                {
                    return;
                }

                levelOneService.UpdateQuote(quoteDateTimeUtc, bidPrice, bidSize, askPrice, askSize);
            }
            else
            {
                Log.Error($"{nameof(LevelOneServiceManager)}.{nameof(HandleQuote)}: Symbol {symbol} not found in {nameof(_levelOneServiceBySymbol)}. This could indicate an unexpected symbol or a missing initialization step.");
            }
        }

        /// <summary>
        /// Handles incoming last trade data for a symbol and routes it to the corresponding <see cref="LevelOneMarketData"/> instance.
        /// </summary>
        /// <param name="symbol">The symbol for which trade data is received.</param>
        /// <param name="tradeDateTimeUtc">The UTC timestamp of the trade.</param>
        /// <param name="lastQuantity">The trade size.</param>
        /// <param name="lastPrice">The trade price.</param>
        /// <param name="saleCondition">Optional sale condition string.</param>
        /// <param name="exchange">Optional exchange identifier.</param>
        public void HandleLastTrade(Symbol symbol, DateTime tradeDateTimeUtc, decimal lastQuantity, decimal lastPrice, string saleCondition = "", string exchange = "")
        {
            if (!_levelOneServiceBySymbol.TryGetValue(symbol, out var levelOneService) || levelOneService == null)
            {
                Log.Error($"{nameof(LevelOneServiceManager)}.{nameof(HandleLastTrade)}: Symbol {symbol} not found in {nameof(_levelOneServiceBySymbol)}. This could indicate an unexpected symbol or a missing initialization step.");
                return;
            }

            levelOneService.UpdateLastTrade(tradeDateTimeUtc, lastQuantity, lastPrice, saleCondition, exchange);
        }

        /// <summary>
        /// Handles open interest updates for the specified symbol.
        /// If the symbol is subscribed, forwards the open interest data to the corresponding
        /// <see cref="LevelOneMarketData"/> instance for publishing.
        /// </summary>
        /// <param name="symbol">The trading symbol associated with the open interest update.</param>
        /// <param name="openInterestDateTimeUtc">The UTC timestamp when the open interest value was observed.</param>
        /// <param name="openInterest">The reported open interest value.</param>
        public void HandleOpenInterest(Symbol symbol, DateTime openInterestDateTimeUtc, decimal openInterest)
        {
            if (!_levelOneServiceBySymbol.TryGetValue(symbol, out var levelOneService) || levelOneService == null)
            {
                Log.Error($"{nameof(LevelOneServiceManager)}.{nameof(HandleLastTrade)}: Symbol {symbol} not found in {nameof(_levelOneServiceBySymbol)}. This could indicate an unexpected symbol or a missing initialization step.");
                return;
            }

            levelOneService.UpdateOpenInterest(openInterestDateTimeUtc, openInterest);
        }

        /// <summary>
        /// Returns subscribed symbols
        /// </summary>
        /// <returns>list of <see cref="Symbol"/> currently subscribed</returns>
        public IEnumerable<Symbol> GetSubscribedSymbols()
        {
            return _subscriptionManager.GetSubscribedSymbols();
        }

        /// <summary>
        /// Handles BaseData updates emitted by <see cref="LevelOneMarketData"/> instances.
        /// Forwards the BaseData to the shared data aggregator in a thread-safe manner.
        /// </summary>
        /// <param name="_">The originator of the BaseData.</param>
        /// <param name="eventData">The BaseData event data.</param>
        private void BaseDataReceived(object _, BaseDataEventArgs eventData)
        {
            lock (_lock)
            {
                _dataAggregator.Update(eventData.BaseData);
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="LevelOneServiceManager"/>.
        /// </summary>
        public void Dispose()
        {
            _subscriptionManager.Dispose();
        }
    }
}
