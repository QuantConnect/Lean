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

using System.Collections.Generic;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// This type exists for transport of fundamental data as a single packet
    /// </summary>
    public class CoarseFundamentalList : BaseData
    {
        /// <summary>
        /// Gets the coarse fundamental instances in this packet
        /// </summary>
        public readonly List<CoarseFundamental> Data = new List<CoarseFundamental>(); 
    }
}
