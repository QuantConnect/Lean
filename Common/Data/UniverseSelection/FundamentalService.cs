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
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Fundamental data provider service
    /// </summary>
    public static class FundamentalService
    {
        private static IFundamentalDataProvider _fundamentalDataProvider;

        /// <summary>
        /// Initializes the service
        /// </summary>
        /// <param name="dataProvider">The data provider instance to use</param>
        /// <param name="liveMode">True if running in live mode</param>
        public static void Initialize(IDataProvider dataProvider, bool liveMode)
        {
            Initialize(dataProvider, Config.Get("fundamental-data-provider", nameof(CoarseFundamentalDataProvider)), liveMode);
        }

        /// <summary>
        /// Initializes the service
        /// </summary>
        /// <param name="dataProvider">The data provider instance to use</param>
        /// <param name="fundamentalDataProvider">The fundamental data provider</param>
        /// <param name="liveMode">True if running in live mode</param>
        public static void Initialize(IDataProvider dataProvider, string fundamentalDataProvider, bool liveMode)
        {
            Initialize(dataProvider, Composer.Instance.GetExportedValueByTypeName<IFundamentalDataProvider>(fundamentalDataProvider), liveMode);
        }

        /// <summary>
        /// Initializes the service
        /// </summary>
        /// <param name="dataProvider">The data provider instance to use</param>
        /// <param name="fundamentalDataProvider">The fundamental data provider</param>
        /// <param name="liveMode">True if running in live mode</param>
        public static void Initialize(IDataProvider dataProvider, IFundamentalDataProvider fundamentalDataProvider, bool liveMode)
        {
            _fundamentalDataProvider = fundamentalDataProvider;
            _fundamentalDataProvider.Initialize(dataProvider, liveMode);
        }

        /// <summary>
        /// Will fetch the requested fundamental information for the requested time and symbol
        /// </summary>
        /// <typeparam name="T">The expected data type</typeparam>
        /// <param name="time">The time to request this data for</param>
        /// <param name="symbol">The symbol instance</param>
        /// <param name="name">The name of the fundamental property</param>
        /// <returns>The fundamental information</returns>
        public static T Get<T>(DateTime time, Symbol symbol, FundamentalProperty name) => Get<T>(time, symbol.ID, name);

        /// <summary>
        /// Will fetch the requested fundamental information for the requested time and symbol
        /// </summary>
        /// <typeparam name="T">The expected data type</typeparam>
        /// <param name="time">The time to request this data for</param>
        /// <param name="securityIdentifier">The security identifier</param>
        /// <param name="name">The name of the fundamental property</param>
        /// <returns>The fundamental information</returns>
        public static T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty name)
        {
            return _fundamentalDataProvider.Get<T>(time.Date, securityIdentifier, name);
        }
    }
}
