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
using QuantConnect.Configuration;
using QuantConnect.Util;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Provides an implementation of <see cref="IMapFileProvider"/> that reads from a local zip file
    /// </summary>
    public class LocalZipMapFileProvider : IMapFileProvider
    {
        // To prevent infinite recursion if something is wrong with the zip files, we look back a maximum of 30 days by default
        private readonly int _lookback = Config.GetInt("map-file-zip-lookback-days", 30);
        private Dictionary<AuxiliaryDataKey, MapFileResolver> _cache;
        private IDataProvider _dataProvider;
        private object _lock;

        /// <summary>
        /// The cached refresh period for the map files
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
        /// Creates a new instance of the <see cref="LocalZipMapFileProvider"/>
        /// </summary>
        public LocalZipMapFileProvider()
        {
            _lock = new object();
            _cache = new Dictionary<AuxiliaryDataKey, MapFileResolver>();
        }

        /// <summary>
        /// Initializes our MapFileProvider by supplying our dataProvider
        /// </summary>
        /// <param name="dataProvider">DataProvider to use</param>
        public void Initialize(IDataProvider dataProvider)
        {
            if (_dataProvider != null)
            {
                return;
            }

            _dataProvider = dataProvider;
            StartExpirationTask();
        }

        /// <summary>
        /// Gets a <see cref="MapFileResolver"/> representing all the map files for the specified market
        /// </summary>
        /// <param name="auxiliaryDataKey">Key used to fetch a map file resolver. Specifying market and security type</param>
        /// <returns>A <see cref="MapFileResolver"/> containing all map files for the specified market</returns>
        public MapFileResolver Get(AuxiliaryDataKey auxiliaryDataKey)
        {
            MapFileResolver result;
            // we use a lock so that only 1 thread loads the map file resolver while the rest wait
            // else we could have multiple threads loading the map file resolver at the same time!
            lock (_lock)
            {
                if (!_cache.TryGetValue(auxiliaryDataKey, out result))
                {
                    _cache[auxiliaryDataKey] = result = GetMapFileResolver(auxiliaryDataKey);
                }
            }
            return result;
        }

        /// <summary>
        /// Helper method that will clear any cached map files on a daily basis, this is useful for live trading
        /// </summary>
        protected virtual void StartExpirationTask()
        {
            lock (_lock)
            {
                // we clear the seeded markets so they are reloaded
                _cache = new Dictionary<AuxiliaryDataKey, MapFileResolver>();
            }
            _ = Task.Delay(CacheRefreshPeriod).ContinueWith(_ => StartExpirationTask());
        }

        private MapFileResolver GetMapFileResolver(AuxiliaryDataKey auxiliaryDataKey)
        {
            var market = auxiliaryDataKey.Market;
            var timestamp = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork);
            var todayNewYork = timestamp.Date;
            var yesterdayNewYork = todayNewYork.AddDays(-1);
            var endDate = yesterdayNewYork.AddDays(-_lookback);
            
            // start the search with yesterday, today's file will be available tomorrow
            for (var date = yesterdayNewYork; date >= endDate; date = date.AddDays(-1))
            {
                var zipFileName = MapFileZipHelper.GetMapFileZipFileName(market, date, auxiliaryDataKey.SecurityType);

                // Fetch a stream for our zip from our data provider
                var stream = _dataProvider.Fetch(zipFileName);

                // If we didn't find a file, continue to the next date
                if (stream == null) continue;

                Log.Trace($"LocalZipMapFileProvider.Get({market}): Fetched map files for: {date.ToShortDateString()} NY ({(date - todayNewYork).Days} days ago).");
                var result = new MapFileResolver(MapFileZipHelper.ReadMapFileZip(stream, market, auxiliaryDataKey.SecurityType));
                stream.DisposeSafely();
                return result;
            }

            throw new InvalidOperationException($"LocalZipMapFileProvider couldn't find any map files going all the way back to {endDate.ToShortDateString()} for {market}, lookback limit is {_lookback} days");
        }
    }
}
