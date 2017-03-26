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

using System.Collections.Generic;
using System.Linq;
using com.fxcm.fix;
using com.fxcm.fix.pretrade;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
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
        private readonly List<Tick> _ticks = new List<Tick>();
        private readonly HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>(); 
        
        #region IDataQueueHandler implementation

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (_ticks)
            {
                var copy = _ticks.ToArray();
                _ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            var symbolsToSubscribe = (from symbol in symbols 
                                      where !_subscribedSymbols.Contains(symbol) && CanSubscribe(symbol)
                                      select symbol).ToList();
            if (symbolsToSubscribe.Count == 0)
                return;

            Log.Trace("FxcmBrokerage.Subscribe(): {0}", string.Join(",", symbolsToSubscribe));

            var request = new MarketDataRequest();
            foreach (var symbol in symbolsToSubscribe)
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

            foreach (var symbol in symbolsToSubscribe)
            {
                _subscribedSymbols.Add(symbol);
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            var symbolsToUnsubscribe = (from symbol in symbols 
                                        where _subscribedSymbols.Contains(symbol) 
                                        select symbol).ToList();
            if (symbolsToUnsubscribe.Count == 0)
                return;

            Log.Trace("FxcmBrokerage.Unsubscribe(): {0}", string.Join(",", symbolsToUnsubscribe));

            var request = new MarketDataRequest();
            foreach (var symbol in symbolsToUnsubscribe)
            {
                request.addRelatedSymbol(_fxcmInstruments[_symbolMapper.GetBrokerageSymbol(symbol)]);
            }
            request.setSubscriptionRequestType(SubscriptionRequestTypeFactory.UNSUBSCRIBE);
            request.setMDEntryTypeSet(MarketDataRequest.MDENTRYTYPESET_ALL);

            lock (_locker)
            {
                _gateway.sendMessage(request);
            }

            foreach (var symbol in symbolsToUnsubscribe)
            {
                _subscribedSymbols.Remove(symbol);
            }
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
