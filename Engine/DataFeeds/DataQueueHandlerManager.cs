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
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Packets;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used to handle multiple live datafeeds
    /// </summary>
    public class DataQueueHandlerManager : IDataQueueHandler, IDataQueueUniverseProvider
    {
        private ITimeProvider _frontierTimeProvider;
        private readonly IAlgorithmSettings _algorithmSettings;
        private readonly Dictionary<SubscriptionDataConfig, Queue<IDataQueueHandler>> _dataConfigAndDataHandler = new();

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public DataQueueHandlerManager(IAlgorithmSettings settings)
        {
            _algorithmSettings = settings;
        }

        /// <summary>
        /// Collection of data queue handles being used
        /// </summary>
        /// <remarks>Protected for testing purposes</remarks>
        protected List<IDataQueueHandler> DataHandlers { get; set; } = new();

        /// <summary>
        /// True if the composite queue handler has any <see cref="IDataQueueUniverseProvider"/> instance
        /// </summary>
        public bool HasUniverseProvider => DataHandlers.OfType<IDataQueueUniverseProvider>().Any();

        /// <summary>
        /// Event triggered when an unsupported configuration is detected
        /// </summary>
        public event EventHandler<SubscriptionDataConfig> UnsupportedConfiguration;

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            Exception failureException = null;
            foreach (var dataHandler in DataHandlers)
            {
                // Emit ticks & custom data as soon as we get them, they don't need any kind of batching behavior applied to them
                // only use the frontier time provider if we need to
                var immediateEmission = dataConfig.Resolution == Resolution.Tick || dataConfig.IsCustomData || _frontierTimeProvider == null;
                var exchangeTimeZone = dataConfig.ExchangeTimeZone;

                IEnumerator<BaseData> enumerator;
                try
                {
                    enumerator = dataHandler.Subscribe(dataConfig, immediateEmission ? newDataAvailableHandler
                        : (sender, eventArgs) => {
                            // let's only wake up the main thread if the data point is allowed to be emitted, else we could fill forward previous bar and not let this one through
                            var dataAvailable = eventArgs as NewDataAvailableEventArgs;
                            if (dataAvailable == null || dataAvailable.DataPoint == null
                                || dataAvailable.DataPoint.EndTime.ConvertToUtc(exchangeTimeZone) <= _frontierTimeProvider.GetUtcNow())
                            {
                                newDataAvailableHandler?.Invoke(sender, eventArgs);
                            }
                        });
                }
                catch (Exception exception)
                {
                    // we will try the next DQH if any, if it handles the request correctly we ignore the error
                    failureException = exception;
                    continue;
                }

                // Check if the enumerator is not empty
                if (enumerator != null)
                {
                    if (!_dataConfigAndDataHandler.TryGetValue(dataConfig, out var dataQueueHandlers))
                    {
                        // we can get the same subscription request multiple times, the aggregator manager handles updating each enumerator
                        // but we need to keep track so we can call unsubscribe later to the target data queue handler
                        _dataConfigAndDataHandler[dataConfig] = dataQueueHandlers = new Queue<IDataQueueHandler>();
                    }
                    dataQueueHandlers.Enqueue(dataHandler);

                    if (immediateEmission)
                    {
                        return enumerator;
                    }

                    var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(dataConfig.Symbol.ID.Market, dataConfig.Symbol, dataConfig.Symbol.SecurityType);
                    if (LeanData.UseStrictEndTime(_algorithmSettings.DailyStrictEndTimeEnabled, dataConfig.Symbol, dataConfig.Increment, exchangeHours))
                    {
                        // before the first frontier enumerator we adjust the endtimes if required
                        enumerator = new StrictDailyEndTimesEnumerator(enumerator, exchangeHours);
                    }

                    return new FrontierAwareEnumerator(enumerator, _frontierTimeProvider,
                        new TimeZoneOffsetProvider(exchangeTimeZone, _frontierTimeProvider.GetUtcNow(), Time.EndOfTime)
                    );
                }
            }

            if (failureException != null)
            {
                // we were not able to serve the request with any DQH and we got an exception, let's bubble it up
                throw failureException;
            }

            // filter out warning for expected cases to reduce noise
            if (!dataConfig.Symbol.Value.Contains("-UNIVERSE-", StringComparison.InvariantCultureIgnoreCase)
                && dataConfig.Type != typeof(Delisting)
                && !dataConfig.Symbol.IsCanonical())
            {
                UnsupportedConfiguration?.Invoke(this, dataConfig);
            }
            return null;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public virtual void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            if (_dataConfigAndDataHandler.TryGetValue(dataConfig, out var dataHandlers))
            {
                var dataHandler = dataHandlers.Dequeue();
                dataHandler.Unsubscribe(dataConfig);

                if (dataHandlers.Count == 0)
                {
                    // nothing left
                    _dataConfigAndDataHandler.Remove(dataConfig);
                }
            }
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
            var dataHandlersConfig = job.DataQueueHandler;
            Log.Trace($"CompositeDataQueueHandler.SetJob(): will use {dataHandlersConfig}");
            foreach (var dataHandlerName in dataHandlersConfig.DeserializeList())
            {
                var dataHandler = Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(dataHandlerName);
                dataHandler.SetJob(job);
                DataHandlers.Add(dataHandler);
            }

            _frontierTimeProvider = InitializeFrontierTimeProvider();
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
            foreach (var dataHandler in DataHandlers)
            {
                dataHandler.Dispose();
            }
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
            foreach (var dataHandler in GetUniverseProviders())
            {
                var result = dataHandler.LookupSymbols(symbol, includeExpired, securityCurrency).ToList();
                if (result.Any())
                {
                    return result;
                }
            }
            return Enumerable.Empty<Symbol>();
        }

        /// <summary>
        /// Returns whether selection can take place or not.
        /// </summary>
        /// <remarks>This is useful to avoid a selection taking place during invalid times, for example IB reset times or when not connected,
        /// because if allowed selection would fail since IB isn't running and would kill the algorithm</remarks>
        /// <returns>True if selection can take place</returns>
        public bool CanPerformSelection()
        {
            return GetUniverseProviders().Any(provider => provider.CanPerformSelection());
        }

        /// <summary>
        /// Creates the frontier time provider instance
        /// </summary>
        /// <remarks>Protected for testing purposes</remarks>
        protected virtual ITimeProvider InitializeFrontierTimeProvider()
        {
            var timeProviders = DataHandlers.OfType<ITimeProvider>().ToList();
            if (timeProviders.Any())
            {
                Log.Trace($"DataQueueHandlerManager.InitializeFrontierTimeProvider(): will use the following IDQH frontier time providers: [{string.Join(",", timeProviders.Select(x => x.GetType()))}]");
                return new CompositeTimeProvider(timeProviders);
            }
            return null;
        }

        private IEnumerable<IDataQueueUniverseProvider> GetUniverseProviders()
        {
            var yielded = false;
            foreach (var universeProvider in DataHandlers.OfType<IDataQueueUniverseProvider>())
            {
                yielded = true;
                yield return universeProvider;
            }

            if (!yielded)
            {
                throw new NotSupportedException("The DataQueueHandler does not support Options and Futures.");
            }
        }
    }
}
