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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using Timer = System.Timers.Timer;
using QuantConnect.Lean.Engine.HistoricalData;

namespace QuantConnect.Lean.Engine.DataFeeds.Queues
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used for testing. <see cref="FakeHistoryProvider"/>
    /// </summary>
    public class FakeDataQueue : IDataQueueHandler, IDataQueueUniverseProvider
    {
        private int _count;
        private readonly Random _random = new Random();
        private int _dataPointsPerSecondPerSymbol;

        private readonly Timer _timer;
        private readonly IOptionChainProvider _optionChainProvider;
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;
        private readonly IDataAggregator _aggregator;
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly Dictionary<Symbol, TimeZoneOffsetProvider> _symbolExchangeTimeZones;

        /// <summary>
        /// Continuous UTC time provider
        /// </summary>
        protected virtual ITimeProvider TimeProvider { get; } = RealTimeProvider.Instance;


        /// <summary>
        /// Initializes a new instance of the <see cref="FakeDataQueue"/> class to randomly emit data for each symbol
        /// </summary>
        public FakeDataQueue()
            : this(Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(nameof(AggregationManager)))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeDataQueue"/> class to randomly emit data for each symbol
        /// </summary>
        public FakeDataQueue(IDataAggregator dataAggregator, int dataPointsPerSecondPerSymbol = 500000)
        {
            _aggregator = dataAggregator;
            _dataPointsPerSecondPerSymbol = dataPointsPerSecondPerSymbol;

            var mapFileProvider = Composer.Instance.GetPart<IMapFileProvider>();
            var historyManager = (IHistoryProvider)Composer.Instance.GetPart<HistoryProviderManager>();
            if (historyManager == null)
            {
                historyManager = Composer.Instance.GetPart<IHistoryProvider>();
            }
            var optionChainProvider = new LiveOptionChainProvider();
            optionChainProvider.Initialize(new(mapFileProvider, historyManager));
            _optionChainProvider = optionChainProvider;

            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _symbolExchangeTimeZones = new Dictionary<Symbol, TimeZoneOffsetProvider>();
            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) => true;
            _subscriptionManager.UnsubscribeImpl += (s, t) => true;

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
                var ticksPerSecond = (_count - lastCount)/elapsed.TotalSeconds;
                Log.Trace("TICKS PER SECOND:: " + ticksPerSecond.ToStringInvariant("000000.0") + " ITEMS IN QUEUE:: " + 0);
                lastCount = _count;
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
            _subscriptionManager.Subscribe(dataConfig);

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
            _subscriptionManager.Unsubscribe(dataConfig);
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
            var symbols = _subscriptionManager.GetSubscribedSymbols();


            foreach (var symbol in symbols)
            {
                if (symbol.IsCanonical() || symbol.Contains("UNIVERSE"))
                {
                    continue;
                }
                var offsetProvider = GetTimeZoneOffsetProvider(symbol);
                var trades = SubscriptionManager.DefaultDataTypes()[symbol.SecurityType].Contains(TickType.Trade);
                var quotes = SubscriptionManager.DefaultDataTypes()[symbol.SecurityType].Contains(TickType.Quote);

                // emits 500k per second
                for (var i = 0; i < _dataPointsPerSecondPerSymbol; i++)
                {
                    var now = TimeProvider.GetUtcNow();
                    var exchangeTime = offsetProvider.ConvertFromUtc(now);
                    var lastTrade = 100 + (decimal)Math.Abs(Math.Sin(now.TimeOfDay.TotalMilliseconds));
                    if (trades)
                    {
                        _count++;
                        _aggregator.Update(new Tick
                        {
                            Time = exchangeTime,
                            Symbol = symbol,
                            Value = lastTrade,
                            TickType = TickType.Trade,
                            Quantity = _random.Next(10, (int)_timer.Interval)
                        });
                    }

                    if (quotes)
                    {
                        _count++;
                        var bidPrice = lastTrade * 0.95m;
                        var askPrice = lastTrade * 1.05m;
                        var bidSize = _random.Next(10, (int) _timer.Interval);
                        var askSize = _random.Next(10, (int)_timer.Interval);
                        _aggregator.Update(new Tick(exchangeTime, symbol, "", "", bidSize: bidSize, bidPrice: bidPrice, askPrice: askPrice, askSize: askSize));
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
                _symbolExchangeTimeZones[symbol] = offsetProvider = new TimeZoneOffsetProvider(exchangeTimeZone, TimeProvider.GetUtcNow(), Time.EndOfTime);
            }
            return offsetProvider;
        }

        /// <summary>
        /// Method returns a collection of Symbols that are available at the data source.
        /// </summary>
        /// <param name="symbol">Symbol to lookup</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <returns>Enumerable of Symbols, that are associated with the provided Symbol</returns>
        public IEnumerable<Symbol> LookupSymbols(Symbol symbol, bool includeExpired, string securityCurrency = null)
        {
            switch (symbol.SecurityType)
            {
                case SecurityType.Option:
                case SecurityType.IndexOption:
                case SecurityType.FutureOption:
                    foreach (var result in _optionChainProvider.GetOptionContractList(symbol, DateTime.UtcNow.Date))
                    {
                        yield return result;
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Checks if the FakeDataQueue can perform selection
        /// </summary>
        public bool CanPerformSelection()
        {
            return true;
        }
    }
}
