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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace QuantConnect.Brokerages.LevelOneOrderBook
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
                SubscribeImpl = (symbols, tickType) => SubscribeCallbackWrapper(symbols, tickType, subscribeCallback),
                UnsubscribeImpl = (symbols, tickType) => UnsubscribeCallbackWrapper(symbols, tickType, unsubscribeCallback)
            };
        }

        /// <summary>
        /// Subscribes to the specified symbol based on the given <see cref="SubscriptionDataConfig"/>.
        /// </summary>
        /// <param name="dataConfig">The subscription configuration containing symbol and type information.</param>
        public void Subscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Subscribe(dataConfig);
        }

        /// <summary>
        /// Unsubscribes from the specified symbol and removes its associated service instance.
        /// </summary>
        /// <param name="dataConfig">The subscription configuration used for unsubscription.</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
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
        public void HandleQuote(Symbol symbol, DateTime? quoteDateTimeUtc, decimal? bidPrice, decimal? bidSize, decimal? askPrice, decimal? askSize)
        {
            if (TryGetLevelOneMarketData(symbol, out var levelOneMarketData))
            {
                levelOneMarketData.UpdateQuote(quoteDateTimeUtc, bidPrice, bidSize, askPrice, askSize);
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
        public void HandleLastTrade(Symbol symbol, DateTime? tradeDateTimeUtc, decimal? lastQuantity, decimal? lastPrice, string saleCondition = "", string exchange = "")
        {
            if (TryGetLevelOneMarketData(symbol, out var levelOneMarketData))
            {
                levelOneMarketData.UpdateLastTrade(tradeDateTimeUtc, lastQuantity, lastPrice, saleCondition, exchange);
            }
        }

        /// <summary>
        /// Handles open interest updates for the specified symbol.
        /// If the symbol is subscribed, forwards the open interest data to the corresponding
        /// <see cref="LevelOneMarketData"/> instance for publishing.
        /// </summary>
        /// <param name="symbol">The trading symbol associated with the open interest update.</param>
        /// <param name="openInterestDateTimeUtc">The UTC timestamp when the open interest value was observed.</param>
        /// <param name="openInterest">The reported open interest value.</param>
        public void HandleOpenInterest(Symbol symbol, DateTime? openInterestDateTimeUtc, decimal? openInterest)
        {
            if (TryGetLevelOneMarketData(symbol, out var levelOneMarketData))
            {
                levelOneMarketData.UpdateOpenInterest(openInterestDateTimeUtc, openInterest);
            }
        }

        /// <summary>
        /// Sets the <see cref="LevelOneMarketData.IgnoreZeroSizeUpdates"/> flag for the specified symbol,
        /// controlling how zero-sized quote updates are handled for that symbol's market data stream.
        /// </summary>
        /// <param name="symbol">The symbol whose quote update behavior should be configured.</param>
        /// <param name="ignoreZeroSizeUpdates">
        /// If <c>true</c>, zero-sized bid or ask updates will be ignored for the given symbol,
        /// preserving existing book values. If <c>false</c>, zero sizes will be applied as valid updates.
        /// </param>
        /// <remarks>
        /// This is typically used to differentiate between real-time and delayed data feeds, where zero-size
        /// updates in real-time may indicate incomplete data, but in delayed feeds may represent actual market states.
        /// </remarks>
        public void SetIgnoreZeroSizeUpdates(Symbol symbol, bool ignoreZeroSizeUpdates)
        {
            if (TryGetLevelOneMarketData(symbol, out var levelOneMarketData))
            {
                levelOneMarketData.IgnoreZeroSizeUpdates = ignoreZeroSizeUpdates;
            }
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
        /// Wraps the subscription delegate to attach symbol-specific handlers and track active Level 1 services.
        /// </summary>
        /// <param name="symbols">The symbols to subscribe.</param>
        /// <param name="tickType">The tick type to subscribe for.</param>
        /// <param name="subscribeCallback">The original subscription logic delegate.</param>
        /// <returns>True if the subscription was successful; otherwise, false.</returns>
        private bool SubscribeCallbackWrapper(IEnumerable<Symbol> symbols, TickType tickType, Func<IEnumerable<Symbol>, TickType, bool> subscribeCallback)
        {
            if (subscribeCallback(symbols, tickType))
            {
                foreach (var symbol in symbols)
                {
                    _levelOneServiceBySymbol[symbol] = new(symbol);
                    _levelOneServiceBySymbol[symbol].BaseDataReceived += BaseDataReceived;
                }

                return true;
            }

            Log.Error($"{nameof(LevelOneServiceManager)}.{nameof(SubscribeCallbackWrapper)}: Failed for symbols: {string.Join(", ", symbols.Select(s => s.Value))}");
            return false;
        }

        /// <summary>
        /// Wraps the unsubscription delegate to detach symbol-specific handlers and remove Level 1 service tracking.
        /// </summary>
        /// <param name="symbols">The symbols to unsubscribe.</param>
        /// <param name="tickType">The tick type to unsubscribe from.</param>
        /// <param name="unsubscribeCallback">The original unsubscription logic delegate.</param>
        /// <returns>True if the unsubscription was successful; otherwise, false.</returns>
        private bool UnsubscribeCallbackWrapper(IEnumerable<Symbol> symbols, TickType tickType, Func<IEnumerable<Symbol>, TickType, bool> unsubscribeCallback)
        {
            if (unsubscribeCallback(symbols, tickType))
            {
                foreach (var symbol in symbols)
                {
                    if (_levelOneServiceBySymbol.TryRemove(symbol, out var levelOneService))
                    {
                        levelOneService.BaseDataReceived -= BaseDataReceived;
                    }
                }
                return true;
            }

            Log.Error($"{nameof(LevelOneServiceManager)}.{nameof(UnsubscribeCallbackWrapper)}: Failed for symbols: {string.Join(", ", symbols.Select(s => s.Value))}");
            return true;
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="LevelOneMarketData"/> instance associated with the specified symbol.
        /// </summary>
        /// <param name="symbol">The symbol whose market data instance is to be retrieved.</param>
        /// <param name="levelOneMarketData">
        /// When this method returns, contains the <see cref="LevelOneMarketData"/> instance associated with the symbol,
        /// if the symbol is found; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the symbol is found and the associated market data instance is returned;
        /// otherwise, <c>false</c>. Logs an error if the symbol is not found.
        /// </returns>
        private bool TryGetLevelOneMarketData(Symbol symbol, out LevelOneMarketData levelOneMarketData)
        {
            if (_levelOneServiceBySymbol.TryGetValue(symbol, out levelOneMarketData))
            {
                return true;
            }

            Log.Error($"{nameof(LevelOneServiceManager)}.{nameof(HandleLastTrade)}: Symbol {symbol} not found in {nameof(_levelOneServiceBySymbol)}. This could indicate an unexpected symbol or a missing initialization step.");
            return false;
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
