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

using QuantConnect.Interfaces;
using QuantConnect.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Data
{
    /// <summary>
    /// Count number of subscribers for each channel (Symbol, Socket) pair
    /// </summary>
    public abstract class DataQueueHandlerSubscriptionManager
    {
        /// <summary>
        /// Counter
        /// </summary>
        protected ConcurrentDictionary<Channel, int> _subscribersByChannel = new ConcurrentDictionary<Channel, int>();

        /// <summary>
        /// Increment number of subscribers for current <see cref="TickType"/>
        /// </summary>
        /// <param name="dataConfig">defines the subscription configuration data.</param>        
        public void Subscribe(SubscriptionDataConfig dataConfig)
        {
            try
            {
                var channel = GetChannel(dataConfig);
                int count;
                if (_subscribersByChannel.TryGetValue(channel, out count))
                {
                    _subscribersByChannel.TryUpdate(channel, count + 1, count);
                    return;
                }

                if (Subscribe(new[] { dataConfig.Symbol }, dataConfig.TickType))
                {
                    _subscribersByChannel.AddOrUpdate(channel, 1);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        /// <summary>
        /// Decrement number of subscribers for current <see cref="TickType"/>
        /// </summary>
        /// <param name="dataConfig">defines the subscription configuration data.</param> 
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            try
            {
                var channel = GetChannel(dataConfig);
                int count;
                if (_subscribersByChannel.TryGetValue(channel, out count))
                {
                    if (count > 1)
                    {
                        _subscribersByChannel.TryUpdate(channel, count - 1, count);
                        return;
                    }

                    if (Unsubscribe(new[] { dataConfig.Symbol }, dataConfig.TickType))
                    {
                        _subscribersByChannel.TryRemove(channel, out count);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        /// <summary>
        /// Describes the way <see cref="IDataQueueHandler"/> implements subscription
        /// </summary>
        /// <param name="symbols">Symbols to subscribe</param>
        /// <param name="tickType">Type of tick data</param>
        /// <returns>Returns true if subsribed; otherwise false</returns>
        protected abstract bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType);

        /// <summary>
        /// Describes the way <see cref="IDataQueueHandler"/> implements unsubscription
        /// </summary>
        /// <param name="symbols">Symbols to unsubscribe</param>
        /// <param name="tickType">Type of tick data</param>
        /// <returns>Returns true if unsubsribed; otherwise false</returns>
        protected abstract bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType);

        /// <summary>
        /// Brokerage maps <see cref="TickType"/> to real socket/api channel
        /// </summary>
        /// <param name="tickType">Type of tick data</param>
        /// <returns></returns>
        protected abstract string ChannelNameFromTickType(TickType tickType);

        /// <summary>
        /// Checks if there is existing subscriber for current channel
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="tickType">Type of tick data</param>
        /// <returns>return true if there is one subscriber at least; otherwise false</returns>
        public bool IsSubscribed(Symbol symbol, TickType tickType)
        {
            return _subscribersByChannel.ContainsKey(GetChannel(
                symbol,
                tickType));
        }

        private Channel GetChannel(SubscriptionDataConfig dataConfig) => GetChannel(dataConfig.Symbol, dataConfig.TickType);

        private Channel GetChannel(Symbol symbol, TickType tickType)
        {
            return new Channel(
                ChannelNameFromTickType(tickType),
                symbol);
        }

        /// <summary>
        /// Returns subscribed symbols
        /// </summary>
        /// <returns>list of <see cref="Symbol"/> currently subscribed</returns>
        public IEnumerable<Symbol> GetSubscribedSymbols()
        {
            lock (_subscribersByChannel)
            {
                return _subscribersByChannel.Keys
                    .Select(c => c.Symbol)
                    .Distinct()
                    .ToList();
            }
        }
    }
}
