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
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace QuantConnect.Data.LevelOne
{
    /// <summary>
    /// Manages subscription and updates for multiple <see cref="LevelOneMarketData"/> instances,
    /// and synchronizes data publishing to a shared <see cref="IDataAggregator"/>.
    /// </summary>
    public class LevelOneServiceManager
    {
        /// <summary>
        /// The shared data aggregator receiving tick updates.
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
        /// Delegate to invoke when subscribing to symbol streams.
        /// </summary>
        private readonly Func<IEnumerable<Symbol>, bool> _subscribeCallback;

        /// <summary>
        /// Delegate to invoke when unsubscribing from symbol streams.
        /// </summary>
        private readonly Func<IEnumerable<Symbol>, bool> _unsubscribeCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="LevelOneServiceManager"/> class.
        /// </summary>
        /// <param name="dataAggregator">The aggregator to publish tick updates to.</param>
        /// <param name="subscribeCallback">The function to execute for subscribing symbols.</param>
        /// <param name="unsubscribeCallback">The function to execute for unsubscribing symbols.</param>
        public LevelOneServiceManager(IDataAggregator dataAggregator, Func<IEnumerable<Symbol>, bool> subscribeCallback, Func<IEnumerable<Symbol>, bool> unsubscribeCallback)
        {
            _dataAggregator = dataAggregator;
            _subscribeCallback = subscribeCallback;
            _unsubscribeCallback = unsubscribeCallback;
        }

        /// <summary>
        /// Subscribes to a collection of trading symbols.
        /// Creates new <see cref="LevelOneMarketData"/> instances for unsubscribed symbols.
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to.</param>
        public void Subscribe(IEnumerable<Symbol> symbols)
        {
            //Log.Trace("EventBasedDataQueueHandlerSubscriptionManager.Subscribe(): {0}", string.Join(",", symbols.Select(x => x.Value)));

            var newlyAdded = new List<Symbol>();
            foreach (var symbol in symbols)
            {
                if (_levelOneServiceBySymbol.TryAdd(symbol, new(symbol, BaseDataReceived)))
                {
                    newlyAdded.Add(symbol);
                }
            }

            if (newlyAdded.Count > 0)
            {
                _subscribeCallback(symbols);
            }
        }

        /// <summary>
        /// Unsubscribes from the specified symbols and removes associated services.
        /// </summary>
        /// <param name="symbols">The symbols to unsubscribe from.</param>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            var removed = new List<Symbol>();
            foreach (var symbol in symbols)
            {
                if (_levelOneServiceBySymbol.TryRemove(symbol, out var levelOneService))
                {
                    levelOneService.BaseDataReceived -= BaseDataReceived;
                    removed.Add(symbol);
                }
            }

            if (removed.Count > 0)
            {
                _unsubscribeCallback(symbols);
            }
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="LevelOneMarketData"/> instance for the given symbol.
        /// </summary>
        /// <param name="symbol">The trading symbol.</param>
        /// <param name="levelOneMarketData">The corresponding service if found.</param>
        /// <returns><c>true</c> if found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(Symbol symbol, out LevelOneMarketData levelOneMarketData)
        {
            if (_levelOneServiceBySymbol.TryGetValue(symbol, out levelOneMarketData))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the collection of currently subscribed symbols.
        /// </summary>
        /// <returns>The symbols associated with active subscriptions.</returns>
        public IEnumerable<Symbol> GetSubscribedSymbols()
        {
            return _levelOneServiceBySymbol.Keys;
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
    }
}
