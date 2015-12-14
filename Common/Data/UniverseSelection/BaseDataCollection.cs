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
using System.Linq;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// This type exists for transport of data as a single packet
    /// </summary>
    public class BaseDataCollection : BaseData
    {
        /// <summary>
        /// Gets the data list
        /// </summary>
        public readonly List<BaseData> Data = new List<BaseData>();

        /// <summary>
        /// Initializes a new default instance of the <see cref="BaseDataCollection"/> c;ass
        /// </summary>
        public BaseDataCollection()
            : this(DateTime.MinValue, Symbol.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataCollection"/> class
        /// </summary>
        /// <param name="time">The time of this data</param>
        /// <param name="symbol">A common identifier for all data in this packet</param>
        /// <param name="data">The data to add to this collection</param>
        public BaseDataCollection(DateTime time, Symbol symbol, IEnumerable<BaseData> data = null)
        {
            Symbol = symbol;
            Time = time;
            if (data != null) Data = data.ToList();
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <remarks>
        /// This base implementation uses reflection to copy all public fields and properties
        /// </remarks>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new BaseDataCollection(Time, Symbol, Data);
        }
    }
}
