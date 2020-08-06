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

using QuantConnect.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Data
{
    public abstract class DataQueueHandlerSubscriptionManager
    {

        protected ConcurrentDictionary<Channel, int> _subscribersByChannel = new ConcurrentDictionary<Channel, int>();

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

        protected abstract bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType);

        protected abstract bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType);

        protected abstract string ChannelNameFromTickType(TickType tickType);

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

        public IEnumerable<Symbol> GetSubscribedSymbols()
        {
            return _subscribersByChannel.Keys
                .Select(c => c.Symbol)
                .Distinct();
        }
    }
}
