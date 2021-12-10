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
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used to handle multiple live datafeeds
    /// </summary>
    public class DataQueueHandlerManager : IDataQueueHandler, IDataQueueUniverseProvider
    {
        private readonly Dictionary<SubscriptionDataConfig, IDataQueueHandler> _dataConfigAndDataHandler = new();

        /// <summary>
        /// Collection of data queue handles being used
        /// </summary>
        /// <remarks>Protected for testing purposes</remarks>
        protected List<IDataQueueHandler> DataHandlers { get; } = new();

        /// <summary>
        /// True if the composite queue handler has any <see cref="IDataQueueUniverseProvider"/> instance
        /// </summary>
        public bool HasUniverseProvider => DataHandlers.OfType<IDataQueueUniverseProvider>().Any();

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            foreach (var dataHandler in DataHandlers)
            {
                var enumerator = dataHandler.Subscribe(dataConfig, newDataAvailableHandler);
                // Check if the enumerator is not empty
                if (enumerator != null)
                {
                    _dataConfigAndDataHandler.Add(dataConfig, dataHandler);
                    return enumerator;
                }
            }
            return null;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public virtual void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            if (_dataConfigAndDataHandler.Remove(dataConfig, out var dataHandler))
            {
                dataHandler.Unsubscribe(dataConfig);
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
