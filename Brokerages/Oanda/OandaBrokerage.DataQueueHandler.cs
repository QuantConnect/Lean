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
using System.Globalization;
using System.Linq;
using QuantConnect.Brokerages.Oanda.DataType;
using QuantConnect.Brokerages.Oanda.DataType.Communications;
using QuantConnect.Brokerages.Oanda.Session;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Oanda Brokerage - implementation of IDataQueueHandler interface
    /// </summary>
    public partial class OandaBrokerage
    {
        private readonly List<Tick> _ticks = new List<Tick>();
        private HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();
        private RatesSession _ratesSession;

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
                                      where !_subscribedSymbols.Contains(symbol)
                                      select symbol).ToList();
            if (symbolsToSubscribe.Count == 0)
                return;

            Log.Trace("OandaBrokerage.Subscribe(): {0}", string.Join(",", symbolsToSubscribe.Select(x => x.Value)));

            // Oanda does not allow more than a few rate streaming sessions, 
            // so we only use a single session for all currently subscribed symbols
            symbolsToSubscribe = symbolsToSubscribe.Union(_subscribedSymbols.ToList()).ToList();

            SubscribeSymbols(symbolsToSubscribe);

            _subscribedSymbols = symbolsToSubscribe.ToHashSet();
        }

        /// <summary>
        /// Removes the specified symbols from the subscription
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

            Log.Trace("OandaBrokerage.Unsubscribe(): {0}", string.Join(",", symbolsToUnsubscribe.Select(x => x.Value)));

            // Oanda does not allow more than a few rate streaming sessions, 
            // so we only use a single session for all currently subscribed symbols
            var symbolsToSubscribe = _subscribedSymbols.ToList().Where(x => !symbolsToUnsubscribe.Contains(x)).ToList();

            SubscribeSymbols(symbolsToSubscribe);

            _subscribedSymbols = symbolsToSubscribe.ToHashSet();
        }

        /// <summary>
        /// Subscribes to the requested symbols (using a single streaming session)
        /// </summary>
        /// <param name="symbolsToSubscribe"></param>
        private void SubscribeSymbols(List<Symbol> symbolsToSubscribe)
        {
            var instruments = symbolsToSubscribe
                .Select(symbol => new Instrument { instrument = _symbolMapper.GetBrokerageSymbol(symbol) })
                .ToList();

            if (_ratesSession != null)
            {
                _ratesSession.DataReceived -= OnDataReceived;
                _ratesSession.StopSession();
            }

            if (instruments.Count > 0)
            {
                _ratesSession = new RatesSession(this, _accountId, instruments);
                _ratesSession.DataReceived += OnDataReceived;
                _ratesSession.StartSession();
            }
        }

        /// <summary>
        /// Returns a DateTime from an RFC3339 string (with microsecond resolution)
        /// </summary>
        /// <param name="time">The time string</param>
        private static DateTime GetDateTimeFromString(string time)
        {
            return DateTime.ParseExact(time, "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Event handler for streaming ticks
        /// </summary>
        /// <param name="data">The data object containing the received tick</param>
        private void OnDataReceived(RateStreamResponse data)
        {
            if (data.IsHeartbeat())
            {
                lock (_lockerConnectionMonitor)
                {
                    _lastHeartbeatUtcTime = DateTime.UtcNow;
                }
                return;
            }

            if (data.tick == null) return;

            var securityType = _symbolMapper.GetBrokerageSecurityType(data.tick.instrument);
            var symbol = _symbolMapper.GetLeanSymbol(data.tick.instrument, securityType, Market.Oanda);
            var time = GetDateTimeFromString(data.tick.time);
            var bidPrice = Convert.ToDecimal(data.tick.bid);
            var askPrice = Convert.ToDecimal(data.tick.ask);
            var tick = new Tick(time, symbol, bidPrice, askPrice);

            lock (_ticks)
            {
                _ticks.Add(tick);
            }
        }

        #endregion
    }
}