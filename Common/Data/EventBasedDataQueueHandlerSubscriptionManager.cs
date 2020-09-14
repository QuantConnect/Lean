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
using QuantConnect.Logging;

namespace QuantConnect.Data
{
    /// <summary>
    /// Overrides <see cref="DataQueueHandlerSubscriptionManager"/> methods using events
    /// </summary>
    public class EventBasedDataQueueHandlerSubscriptionManager : DataQueueHandlerSubscriptionManager
    {
        /// <summary>
        /// Creates an instance of <see cref="EventBasedDataQueueHandlerSubscriptionManager"/> with a single channel name
        /// </summary>
        public EventBasedDataQueueHandlerSubscriptionManager() : this(t => Channel.Single) {}

        /// <summary>
        /// Creates an instance of <see cref="EventBasedDataQueueHandlerSubscriptionManager"/>
        /// </summary>
        /// <param name="getChannelName">Convert TickType into string</param>
        public EventBasedDataQueueHandlerSubscriptionManager(Func<TickType, string> getChannelName)
        {
            _getChannelName = getChannelName;
        }

        /// <summary>
        /// Subscription method implementation
        /// </summary>
        public Func<IEnumerable<Symbol>, TickType, bool> SubscribeImpl;
        
        /// <summary>
        /// Unsubscription method implementation
        /// </summary>
        public Func<IEnumerable<Symbol>, TickType, bool> UnsubscribeImpl;

        /// <summary>
        /// Socket channel name
        /// </summary>
        private Func<TickType, string> _getChannelName;

        /// <summary>
        /// The way Brokerage subscribes to symbol tickers
        /// </summary>
        /// <param name="symbols">Symbols to subscribe</param>
        /// <param name="tickType">Type of tick data</param>
        /// <returns></returns>
        protected override bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            Log.Trace("EventBasedDataQueueHandlerSubscriptionManager.Subscribe(): {0}", string.Join(",", symbols.Select(x => x.Value)));
            return SubscribeImpl?.Invoke(symbols, tickType) == true;
        }

        /// <summary>
        /// The way Brokerage unsubscribes from symbol tickers
        /// </summary>
        /// <param name="symbols">Symbols to unsubscribe</param>
        /// <param name="tickType">Type of tick data</param>
        /// <returns></returns>
        protected override bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            Log.Trace("EventBasedDataQueueHandlerSubscriptionManager.Unsubscribe(): {0}", string.Join(",", symbols.Select(x => x.Value)));
            return UnsubscribeImpl?.Invoke(symbols, tickType) == true;
        }

        /// <summary>
        /// Channel name
        /// </summary>
        /// <param name="tickType">Type of tick data</param>
        /// <returns>Returns Socket channel name corresponding <paramref name="tickType"/></returns>
        protected override string ChannelNameFromTickType(TickType tickType)
        {
            return _getChannelName?.Invoke(tickType);
        }
    }
}
