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
using com.fxcm.fix;
using com.fxcm.fix.pretrade;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.Fxcm
{
    /// <summary>
    /// FXCM brokerage - implementation of IDataQueueHandler interface
    /// </summary>
    public partial class FxcmBrokerage
    {
        #region IDataQueueHandler implementation

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        private bool Subscribe(IEnumerable<Symbol> symbols)
        {
            var request = new MarketDataRequest();
            foreach (var symbol in symbols)
            {
                TradingSecurity fxcmSecurity;
                if (_fxcmInstruments.TryGetValue(_symbolMapper.GetBrokerageSymbol(symbol), out fxcmSecurity))
                {
                    request.addRelatedSymbol(fxcmSecurity);

                    // cache exchange time zone for symbol
                    DateTimeZone exchangeTimeZone;
                    if (!_symbolExchangeTimeZones.TryGetValue(symbol, out exchangeTimeZone))
                    {
                        exchangeTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.FXCM, symbol, symbol.SecurityType).TimeZone;
                        _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
                    }

                }
            }
            request.setSubscriptionRequestType(SubscriptionRequestTypeFactory.SUBSCRIBE);
            request.setMDEntryTypeSet(MarketDataRequest.MDENTRYTYPESET_ALL);

            lock (_locker)
            {
                _gateway.sendMessage(request);
            }

            return true;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            Log.Trace("FxcmBrokerage.Unsubscribe(): {0}", string.Join(",", symbols));

            var request = new MarketDataRequest();
            foreach (var symbol in symbols)
            {
                request.addRelatedSymbol(_fxcmInstruments[_symbolMapper.GetBrokerageSymbol(symbol)]);
            }
            request.setSubscriptionRequestType(SubscriptionRequestTypeFactory.UNSUBSCRIBE);
            request.setMDEntryTypeSet(MarketDataRequest.MDENTRYTYPESET_ALL);

            lock (_locker)
            {
                _gateway.sendMessage(request);
            }

            return true;
        }

        /// <summary>
        /// Returns true if this brokerage supports the specified symbol
        /// </summary>
        private static bool CanSubscribe(Symbol symbol)
        {
            // ignore unsupported security types
            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd)
                return false;

            // ignore universe symbols
            return !symbol.Value.Contains("-UNIVERSE-");
        }

        #endregion

    }
}
