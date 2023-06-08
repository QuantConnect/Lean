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
using QuantConnect.Util;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Provides an implementation of <see cref="IFactorFileProvider"/> that searches the local disk for a zip file containing all factor files
    /// </summary>
    public class LocalZipFactorFileProvider : IFactorFileProvider
    {
        private readonly object _lock;
        private IDataProvider _dataProvider;
        private IMapFileProvider _mapFileProvider;
        private Dictionary<AuxiliaryDataKey, bool> _seededMarket;
        private readonly Dictionary<Symbol, IFactorProvider> _factorFiles;

        /// <summary>
        /// The cached refresh period for the factor files
        /// </summary>
        /// <remarks>Exposed for testing</remarks>
        protected virtual TimeSpan CacheRefreshPeriod
        {
            get
            {
                var dueTime = Time.GetNextLiveAuxiliaryDataDueTime();
                if (dueTime > TimeSpan.FromMinutes(10))
                {
                    // Clear the cache before the auxiliary due time to avoid race conditions with consumers
                    return dueTime - TimeSpan.FromMinutes(10);
                }
                return dueTime;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="LocalZipFactorFileProvider"/> class.
        /// </summary>
        public LocalZipFactorFileProvider()
        {
            _factorFiles = new Dictionary<Symbol, IFactorProvider>();
            _seededMarket = new Dictionary<AuxiliaryDataKey, bool>();
            _lock = new object();
        }

        /// <summary>
        /// Initializes our FactorFileProvider by supplying our mapFileProvider
        /// and dataProvider
        /// </summary>
        /// <param name="mapFileProvider">MapFileProvider to use</param>
        /// <param name="dataProvider">DataProvider to use</param>
        public void Initialize(IMapFileProvider mapFileProvider, IDataProvider dataProvider)
        {
            if (_mapFileProvider != null || _dataProvider != null)
            {
                return;
            }
            
            _mapFileProvider = mapFileProvider;
            _dataProvider = dataProvider;
            StartExpirationTask();
        }

        /// <summary>
        /// Gets a <see cref="FactorFile"/> instance for the specified symbol, or null if not found
        /// </summary>
        /// <param name="symbol">The security's symbol whose factor file we seek</param>
        /// <returns>The resolved factor file, or null if not found</returns>
        public IFactorProvider Get(Symbol symbol)
        {
            symbol = symbol.GetFactorFileSymbol();
            var key = AuxiliaryDataKey.Create(symbol);
            lock (_lock)
            {
                if (!_seededMarket.ContainsKey(key))
                {
                    HydrateFactorFileFromLatestZip(key);
                    _seededMarket[key] = true;
                }

                IFactorProvider factorFile;
                if (!_factorFiles.TryGetValue(symbol, out factorFile))
                {
                    // Could not find factor file for symbol
                    Log.Error($"LocalZipFactorFileProvider.Get({symbol}): No factor file found.");
                    _factorFiles[symbol] = factorFile = symbol.GetEmptyFactorFile();
                }
                return factorFile;
            }
        }

        /// <summary>
        /// Helper method that will clear any cached factor files in a daily basis, this is useful for live trading
        /// </summary>
        protected virtual void StartExpirationTask()
        {
            lock (_lock)
            {
                // we clear the seeded markets so they are reloaded
                _seededMarket = new Dictionary<AuxiliaryDataKey, bool>();
            }
            _ = Task.Delay(CacheRefreshPeriod).ContinueWith(_ => StartExpirationTask());
        }

        /// Hydrate the <see cref="_factorFiles"/> from the latest zipped factor file on disk
        private void HydrateFactorFileFromLatestZip(AuxiliaryDataKey key)
        {
            var market = key.Market;
            // start the search with yesterday, today's file will be available tomorrow
            var todayNewYork = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork).Date;
            var date = todayNewYork.AddDays(-1);

            var count = 0;

            do
            {
                var factorFilePath = FactorFileZipHelper.GetFactorFileZipFileName(market, date, key.SecurityType);

                // Fetch a stream for our zip from our data provider
                var stream = _dataProvider.Fetch(factorFilePath);

                // If the file was found we can read the file
                if (stream != null)
                {
                    var mapFileResolver = _mapFileProvider.Get(key);
                    foreach (var keyValuePair in FactorFileZipHelper.ReadFactorFileZip(stream, mapFileResolver, market, key.SecurityType))
                    {
                        // we merge with existing, this will allow to hold multiple markets
                        _factorFiles[keyValuePair.Key] = keyValuePair.Value;
                    }
                    stream.DisposeSafely();
                    Log.Trace($"LocalZipFactorFileProvider.Get({market}): Fetched factor files for: {date.ToShortDateString()} NY");

                    return;
                }

                // Otherwise we will search back another day
                Log.Debug($"LocalZipFactorFileProvider.Get({market}): No factor file found for date {date.ToShortDateString()}");

                // prevent infinite recursion if something is wrong
                if (count++ > 7)
                {
                    throw new InvalidOperationException($"LocalZipFactorFileProvider.Get(): Could not find any factor files going all the way back to {date} for {market}");
                }

                date = date.AddDays(-1);
            }
            while (true);
        }
    }
}
