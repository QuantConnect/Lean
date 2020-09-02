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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using Timer = System.Timers.Timer;

namespace QuantConnect.Lean.Engine.DataFeeds.Queues
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used for testing
    /// </summary>
    public class FakeDataQueue : IDataQueueHandler
    {
        private int count;
        private readonly Random _random = new Random();

        private readonly Timer _timer;
        private readonly HashSet<Symbol> _symbols;
        private readonly object _sync = new object();
        private readonly IDataAggregator _aggregator;
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly Dictionary<Symbol, TimeZoneOffsetProvider> _symbolExchangeTimeZones;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeDataQueue"/> class to randomly emit data for each symbol
        /// </summary>
        public FakeDataQueue()
        {
            _symbols = new HashSet<Symbol>();
            _aggregator = new AggregationManager();
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _symbolExchangeTimeZones = new Dictionary<Symbol, TimeZoneOffsetProvider>();

            // load it up to start
            PopulateQueue();
            PopulateQueue();
            PopulateQueue();
            PopulateQueue();

            _timer = new Timer
            {
                AutoReset = false,
                Enabled = true,
                Interval = 1000,
            };

            var lastCount = 0;
            var lastTime = DateTime.UtcNow;
            _timer.Elapsed += (sender, args) =>
            {
                var elapsed = (DateTime.UtcNow - lastTime);
                var ticksPerSecond = (count - lastCount)/elapsed.TotalSeconds;
                Log.Trace("TICKS PER SECOND:: " + ticksPerSecond.ToStringInvariant("000000.0") + " ITEMS IN QUEUE:: " + 0);
                lastCount = count;
                lastTime = DateTime.UtcNow;
                PopulateQueue();
                try
                {
                    _timer.Reset();
                }
                catch (ObjectDisposedException)
                {
                    // pass
                }
            };
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            lock (_sync)
            {
                _symbols.Add(dataConfig.Symbol);
            }

            return enumerator;
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            lock (_sync)
            {
                _symbols.Remove(dataConfig.Symbol);
            }
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Returns whether the data provider is connected
        /// </summary>
        /// <returns>true if the data provider is connected</returns>
        public bool IsConnected => true;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _timer.Stop();
            _timer.DisposeSafely();
        }

        /// <summary>
        /// Pumps a bunch of ticks into the queue
        /// </summary>
        private void PopulateQueue()
        {
            List<Symbol> symbols;
            lock (_sync)
            {
                symbols = _symbols.ToList();
            }

            foreach (var symbol in symbols)
            {
                var offsetProvider = GetTimeZoneOffsetProvider(symbol);
                var trades = SubscriptionManager.DefaultDataTypes()[symbol.SecurityType].Contains(TickType.Trade);
                var quotes = SubscriptionManager.DefaultDataTypes()[symbol.SecurityType].Contains(TickType.Quote);

                // emits 500k per second
                for (var i = 0; i < 500000; i++)
                {
                    if (trades)
                    {
                        count++;
                        _aggregator.Update(new Tick
                        {
                            Time = offsetProvider.ConvertFromUtc(DateTime.UtcNow),
                            Symbol = symbol,
                            Value = 10 + (decimal)Math.Abs(Math.Sin(DateTime.UtcNow.TimeOfDay.TotalMinutes)),
                            TickType = TickType.Trade,
                            Quantity = _random.Next(10, (int)_timer.Interval)
                        });
                    }

                    if (quotes)
                    {
                        count++;
                        var bid = 10 + (decimal) Math.Abs(Math.Sin(DateTime.UtcNow.TimeOfDay.TotalMinutes));
                        var bidSize = _random.Next(10, (int) _timer.Interval);
                        var askSize = _random.Next(10, (int)_timer.Interval);
                        var time = offsetProvider.ConvertFromUtc(DateTime.UtcNow);
                        _aggregator.Update(new Tick(time, symbol, "", "",bid, bidSize, bid * 1.01m, askSize));
                    }
                }
            }
        }

        private TimeZoneOffsetProvider GetTimeZoneOffsetProvider(Symbol symbol)
        {
            TimeZoneOffsetProvider offsetProvider;
            if (!_symbolExchangeTimeZones.TryGetValue(symbol, out offsetProvider))
            {
                // read the exchange time zone from market-hours-database
                var exchangeTimeZone = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
                _symbolExchangeTimeZones[symbol] = offsetProvider = new TimeZoneOffsetProvider(exchangeTimeZone, DateTime.UtcNow, Time.EndOfTime);
            }
            return offsetProvider;
        }
    }
}
