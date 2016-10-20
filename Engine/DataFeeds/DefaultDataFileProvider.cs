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

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Default file provider functionality that does not attempt to retrieve any data
    /// </summary>
    public class DefaultDataFileProvider : IDataFileProvider
    {
        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        /// <param name="symbol">Symbol of the security</param>
        /// <param name="resolution">Resolution of the data requested</param>
        /// <param name="date">DateTime of the data requested</param>
        /// <returns>False</returns>
        public bool Fetch(Symbol symbol, Resolution resolution, DateTime date)
        {
            return false;
        }
    }
}
