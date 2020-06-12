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
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Data
{
    /// <summary>
    /// Represents the set of parameters for the <see cref="IHistoryProvider.Initialize"/> method
    /// </summary>
    public class HistoryProviderInitializeParameters
    {
        /// <summary>
        /// The job
        /// </summary>
        public AlgorithmNodePacket Job { get; }

        /// <summary>
        /// The API instance
        /// </summary>
        public IApi Api { get; }

        /// <summary>
        /// The provider used to get data when it is not present on disk
        /// </summary>
        public IDataProvider DataProvider { get; }

        /// <summary>
        /// The provider used to cache history data files
        /// </summary>
        public IDataCacheProvider DataCacheProvider { get; }

        /// <summary>
        /// The provider used to get a map file resolver to handle equity mapping
        /// </summary>
        public IMapFileProvider MapFileProvider { get; }

        /// <summary>
        /// The provider used to get factor files to handle equity price scaling
        /// </summary>
        public IFactorFileProvider FactorFileProvider { get; }

        /// <summary>
        /// A function used to send status updates
        /// </summary>
        public Action<int> StatusUpdateAction { get; }

        /// <summary>
        /// True if parallel history requests are enabled
        /// </summary>
        /// <remarks>Parallel history requests are faster but require more ram and cpu usage
        /// and are not compatible with some <see cref="IDataCacheProvider"/></remarks>
        public bool ParallelHistoryRequestsEnabled { get; }

        /// <summary>
        /// The data permission manager
        /// </summary>
        public IDataPermissionManager DataPermissionManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryProviderInitializeParameters"/> class from the specified parameters
        /// </summary>
        /// <param name="job">The job</param>
        /// <param name="api">The API instance</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <param name="dataCacheProvider">Provider used to cache history data files</param>
        /// <param name="mapFileProvider">Provider used to get a map file resolver to handle equity mapping</param>
        /// <param name="factorFileProvider">Provider used to get factor files to handle equity price scaling</param>
        /// <param name="statusUpdateAction">Function used to send status updates</param>
        /// <param name="parallelHistoryRequestsEnabled">True if parallel history requests are enabled</param>
        /// <param name="dataPermissionManager">The data permission manager to use</param>
        public HistoryProviderInitializeParameters(
            AlgorithmNodePacket job,
            IApi api,
            IDataProvider dataProvider,
            IDataCacheProvider dataCacheProvider,
            IMapFileProvider mapFileProvider,
            IFactorFileProvider factorFileProvider,
            Action<int> statusUpdateAction,
            bool parallelHistoryRequestsEnabled,
            IDataPermissionManager dataPermissionManager)
        {
            Job = job;
            Api = api;
            DataProvider = dataProvider;
            DataCacheProvider = dataCacheProvider;
            MapFileProvider = mapFileProvider;
            FactorFileProvider = factorFileProvider;
            StatusUpdateAction = statusUpdateAction;
            ParallelHistoryRequestsEnabled = parallelHistoryRequestsEnabled;
            DataPermissionManager = dataPermissionManager;
        }
    }
}
