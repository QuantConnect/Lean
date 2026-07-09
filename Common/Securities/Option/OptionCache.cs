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

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Option specific caching support
    /// </summary>
    /// <seealso cref="SecurityCache"/>
    public class OptionCache : SecurityCache
    {
        /// <summary>
        /// Stores the specified data list in the cache, updating the open interest from any chain universe data
        /// </summary>
        /// <param name="data">The collection of data to store in this cache</param>
        /// <param name="dataType">The data type</param>
        public override void StoreData(IReadOnlyList<BaseData> data, Type dataType)
        {
            UpdateOpenInterest(data);
            base.StoreData(data, dataType);
        }
    }
}
