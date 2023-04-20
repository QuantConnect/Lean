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

using System.IO;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using System.Collections.Concurrent;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Provides an implementation of <see cref="IFactorFileProvider"/> that searches the local disk
    /// </summary>
    public class LocalDiskFactorFileProvider : IFactorFileProvider
    {
        private IMapFileProvider _mapFileProvider;
        private IDataProvider _dataProvider;
        private readonly ConcurrentDictionary<Symbol, IFactorProvider> _cache;

        /// <summary>
        /// Creates a new instance of the <see cref="LocalDiskFactorFileProvider"/>
        /// </summary>
        public LocalDiskFactorFileProvider()
        {
            _cache = new ConcurrentDictionary<Symbol, IFactorProvider>();
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
        public IFactorProvider Get(Symbol symbol)
        {
            symbol = symbol.GetFactorFileSymbol();
            IFactorProvider factorFile;
            if (_cache.TryGetValue(symbol, out factorFile))
            {
                return factorFile;
            }

            // we first need to resolve the map file to get a permtick, that's how the factor files are stored
            var mapFileResolver = _mapFileProvider.Get(AuxiliaryDataKey.Create(symbol));
            if (mapFileResolver == null)
            {
                return GetFactorFile(symbol, symbol.Value);
            }

            var mapFile = mapFileResolver.ResolveMapFile(symbol);
            if (mapFile.IsNullOrEmpty())
            {
                return GetFactorFile(symbol, symbol.Value);
            }

            return GetFactorFile(symbol, mapFile.Permtick);
        }

        /// <summary>
        /// Checks that the factor file exists on disk, and if it does, loads it into memory
        /// </summary>
        private IFactorProvider GetFactorFile(Symbol symbol, string permtick)
        {
            var basePath = Globals.GetDataFolderPath(FactorFileZipHelper.GetRelativeFactorFilePath(symbol.ID.Market, symbol.SecurityType));
            var path = Path.Combine(basePath, permtick.ToLowerInvariant() + ".csv");

            var factorFile = PriceScalingExtensions.SafeRead(permtick, _dataProvider.ReadLines(path), symbol.SecurityType);
            _cache.AddOrUpdate(symbol, factorFile, (s, c) => factorFile);
            return factorFile;
        }
    }
}
