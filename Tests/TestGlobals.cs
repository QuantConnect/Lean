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

using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Common.Data.Fundamental;
using QuantConnect.Data;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Global static class for some core providers used by various tests
    /// </summary>
    public static class TestGlobals
    {
        private static bool _initialized;

        public static IDataProvider DataProvider
            = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider", "DefaultDataProvider"));
        public static IMapFileProvider MapFileProvider
            = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
        public static IFactorFileProvider FactorFileProvider
            = Composer.Instance.GetExportedValueByTypeName<IFactorFileProvider>(Config.Get("factor-file-provider", "LocalDiskFactorFileProvider"));

        public static IDataCacheProvider DataCacheProvider = new ZipDataCacheProvider(DataProvider);
        public static IHistoryProvider HistoryProvider
            = Composer.Instance.GetExportedValueByTypeName<IHistoryProvider>("SubscriptionDataReaderHistoryProvider");

        /// <summary>
        /// Initialize our providers, called by AssemblyInitialize.cs so all tests
        /// can access initialized providers
        /// </summary>
        public static void Initialize()
        {
            lock (DataProvider)
            {
                if (_initialized)
                {
                    return;
                }
                _initialized = true;

                var initializeParameters = new HistoryProviderInitializeParameters(null, null, DataProvider, DataCacheProvider,
                    MapFileProvider, FactorFileProvider, (_) => { }, true, new DataPermissionManager(), null, new AlgorithmSettings());
                try
                {
                    HistoryProvider.Initialize(initializeParameters);
                }
                catch
                {
                    // Already initialized
                }
                MapFileProvider.Initialize(DataProvider);
                FactorFileProvider.Initialize(MapFileProvider, DataProvider);
                FundamentalService.Initialize(DataProvider, new NullFundamentalDataProvider(), false);
            }
        }
    }
}
