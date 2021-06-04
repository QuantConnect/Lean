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
using System.IO;
using QuantConnect.Logging;
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
        private bool _seededFactorFile;
        private IMapFileProvider _mapFileProvider;
        private IDataProvider _dataProvider;
        private Dictionary<Symbol, FactorFile> _factorFiles;

        /// <summary>
        /// Creates a new instance of the <see cref="LocalZipFactorFileProvider"/> class.
        /// </summary>
        public LocalZipFactorFileProvider()
        {
            _factorFiles = new Dictionary<Symbol, FactorFile>();
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
            _mapFileProvider = mapFileProvider;
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Gets a <see cref="FactorFile"/> instance for the specified symbol, or null if not found
        /// </summary>
        /// <param name="symbol">The security's symbol whose factor file we seek</param>
        /// <returns>The resolved factor file, or null if not found</returns>
        public FactorFile Get(Symbol symbol)
        {
            if (!_seededFactorFile)
            {
                lock (_lock)
                {
                    if (!_seededFactorFile)
                    {
                        HydrateFactorFileFromLatestZip();
                        _seededFactorFile = true;
                    }
                }
            }

            FactorFile factorFile;
            if (_factorFiles.TryGetValue(symbol, out factorFile))
            {
                return factorFile;
            }

            // Could not find factor file for symbol
            Log.Error($"LocalZipFactorFileProvider.Get({symbol}): No factor file found.");
            return null;
        }

        /// Hydrate the <see cref="_factorFiles"/> from the latest zipped factor file on disk
        private void HydrateFactorFileFromLatestZip()
        {
            // Todo: assume for only USA market for now
            var market = QuantConnect.Market.USA;

            // start the search with yesterday, today's file will be available tomorrow
            var todayNewYork = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork).Date;
            var date = todayNewYork.AddDays(-1);

            var count = 0;

            do
            {
                var zipFileName = $"equity/{market}/factor_files/factor_files_{date:yyyyMMdd}.zip";
                var factorFilePath = Path.Combine(Globals.DataFolder, zipFileName);

                // Fetch a stream for our zip from our data provider
                var stream = _dataProvider.Fetch(factorFilePath);

                // If the file was found we can read the file
                if (stream != null)
                {
                    var mapFileResolver = _mapFileProvider.Get(market);
                    _factorFiles = FactorFileZipHelper.ReadFactorFileZip(stream, mapFileResolver, market);

                    Log.Trace($"LocalZipFactorFileProvider.Get({market}): Fetched factor files for: {date.ToShortDateString()} NY");

                    return;
                }

                // Otherwise we will search back another day
                Log.Error($"LocalZipFactorFileProvider.Get(): No factor file found for date {date.ToShortDateString()}");

                // prevent infinite recursion if something is wrong
                if (count++ > 7)
                {
                    throw new InvalidOperationException($"LocalZipFactorFileProvider.Get(): Could not find any map files going all the way back to {date}");
                }

                date = date.AddDays(-1);
            }
            while (true);
        }
    }
}
