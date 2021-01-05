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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Event args for <see cref="SecurityCache.DataStored"/> event
    /// </summary>
    public class SecurityCacheDataStoredEventArgs : EventArgs
    {
        /// <summary>
        /// The type of data that was stored, such as <see cref="TradeBar"/>
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// The list of data points stored
        /// </summary>
        public IReadOnlyList<BaseData> Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityCacheDataStoredEventArgs"/> class
        /// </summary>
        /// <param name="dataType">The type of data</param>
        /// <param name="data">The list of data points</param>
        public SecurityCacheDataStoredEventArgs(Type dataType, IReadOnlyList<BaseData> data)
        {
            Data = data;
            DataType = dataType;
        }
    }
}