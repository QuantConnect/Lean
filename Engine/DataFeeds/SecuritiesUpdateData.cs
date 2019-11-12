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

using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Transport type for securities update data. This derived class is provided to maintain
    /// separate lists for all data and non-fill forward data for storage in the security cache.
    /// </summary>
    public class SecuritiesUpdateData : UpdateData<ISecurityPrice>
    {
        /// <summary>
        /// A list of securities update data where all entries are not <see cref="BaseData.IsFillForward"/>
        /// </summary>
        public IReadOnlyList<BaseData> NonFillForwardData { get; }

        public SecuritiesUpdateData(
            ISecurityPrice target,
            Type dataType,
            IReadOnlyList<BaseData> data,
            IReadOnlyList<BaseData> nonFillForwardData,
            bool isInternalConfig,
            bool? containsFillForwardData = null)
            : base(target, dataType, data, isInternalConfig, containsFillForwardData)
        {
            NonFillForwardData = nonFillForwardData;
        }
    }
}