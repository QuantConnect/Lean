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
using QuantConnect.Algorithm;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    internal class DataManagerStub : DataManager
    {
        public ISecurityService SecurityService { get; }
        public IAlgorithm Algorithm { get; }
        public IDataFeed DataFeed { get; }

        public DataManagerStub()
            : this(new QCAlgorithm())
        {

        }

        public DataManagerStub(ITimeKeeper timeKeeper)
            : this(new QCAlgorithm(), timeKeeper)
        {

        }

        public DataManagerStub(IAlgorithm algorithm, IDataFeed dataFeed, bool liveMode = false)
            : this(dataFeed, algorithm, new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork), liveMode)
        {

        }

        public DataManagerStub(IAlgorithm algorithm)
            : this(new NullDataFeed(), algorithm, new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
        {

        }

        public DataManagerStub(IDataFeed dataFeed, IAlgorithm algorithm)
            : this(dataFeed, algorithm, new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
        {

        }

        public DataManagerStub(IAlgorithm algorithm, ITimeKeeper timeKeeper)
            : this(new NullDataFeed(), algorithm, timeKeeper)
        {

        }

        public DataManagerStub(IDataFeed dataFeed, IAlgorithm algorithm, ITimeKeeper timeKeeper, bool liveMode = false)
            : this(dataFeed, algorithm, timeKeeper, MarketHoursDatabase.FromDataFolder(), SymbolPropertiesDatabase.FromDataFolder(), liveMode)
        {

        }

        public DataManagerStub(IDataFeed dataFeed, IAlgorithm algorithm, ITimeKeeper timeKeeper, MarketHoursDatabase marketHoursDatabase, SymbolPropertiesDatabase symbolPropertiesDatabase, bool liveMode = false)
            : this(dataFeed, algorithm, timeKeeper, marketHoursDatabase,
                new SecurityService(algorithm.Portfolio.CashBook,
                    marketHoursDatabase,
                    symbolPropertiesDatabase,
                    algorithm,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCacheProvider(algorithm.Portfolio),
                    algorithm: algorithm),
                liveMode)
        {
        }

        public DataManagerStub(IDataFeed dataFeed, IAlgorithm algorithm, ITimeKeeper timeKeeper, MarketHoursDatabase marketHoursDatabase, SecurityService securityService, bool liveMode = false)
            : this(dataFeed,
                algorithm,
                timeKeeper,
                marketHoursDatabase,
                securityService,
                new DataPermissionManager(),
                liveMode)
        {
        }

        public DataManagerStub(IDataFeed dataFeed, IAlgorithm algorithm, ITimeKeeper timeKeeper, MarketHoursDatabase marketHoursDatabase, SecurityService securityService, DataPermissionManager dataPermissionManager, bool liveMode = false)
            : base(dataFeed,
                new UniverseSelection(algorithm, securityService, dataPermissionManager, TestGlobals.DataProvider),
                algorithm,
                timeKeeper,
                marketHoursDatabase,
                liveMode,
                RegisteredSecurityDataTypesProvider.Null,
                dataPermissionManager)
        {
            SecurityService = securityService;
            algorithm.Securities.SetSecurityService(securityService);
            Algorithm = algorithm;
            DataFeed = dataFeed;
        }
    }
}
