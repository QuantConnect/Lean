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

using System.Collections.Concurrent;
using System.IO;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Provides an implementation of <see cref="IFactorFileProvider"/> that searches the local disk
    /// </summary>
    public class LocalDiskFactorFileProvider : IFactorFileProvider
    {
        private IMapFileProvider _mapFileProvider;
        private IDataProvider _dataProvider;
        private readonly ConcurrentDictionary<Symbol, FactorFile> _cache;

        /// <summary>
        /// Creates a new instance of the <see cref="LocalDiskFactorFileProvider"/>
        /// </summary>
        public LocalDiskFactorFileProvider()
        {
            _cache = new ConcurrentDictionary<Symbol, FactorFile>();
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
            FactorFile factorFile;
            if (_cache.TryGetValue(symbol, out factorFile))
            {
                return factorFile;
            }

            var market = symbol.ID.Market;

            // we first need to resolve the map file to get a permtick, that's how the factor files are stored
            var mapFileResolver = _mapFileProvider.Get(market);
            if (mapFileResolver == null)
            {
                return GetFactorFile(symbol, symbol.Value, market);
            }

            var mapFile = mapFileResolver.ResolveMapFile(symbol.ID.Symbol, symbol.ID.Date);
            if (mapFile.IsNullOrEmpty())
            {
                return GetFactorFile(symbol, symbol.Value, market);
            }

            return GetFactorFile(symbol, mapFile.Permtick, market);
        }

        /// <summary>
        /// Checks that the factor file exists on disk, and if it does, loads it into memory
        /// </summary>
        private FactorFile GetFactorFile(Symbol symbol, string permtick, string market)
        {
            FactorFile factorFile = null;

            var path = Path.Combine(Globals.DataFolder, "equity", market, "factor_files", permtick.ToLowerInvariant() + ".csv");

            var factorFileStream = _dataProvider.Fetch(path);
            if (factorFileStream != null)
            {
                factorFile = FactorFile.Read(permtick, factorFileStream);
                factorFileStream.DisposeSafely();
                _cache.AddOrUpdate(symbol, factorFile, (s, c) => factorFile);
            }
            else
            {
                // add null value to the cache, we don't want to check the disk multiple times
                // but keep existing value if it exists, just in case
                _cache.AddOrUpdate(symbol, factorFile, (s, oldValue) => oldValue);
            }
            return factorFile;
        }
    }
}
