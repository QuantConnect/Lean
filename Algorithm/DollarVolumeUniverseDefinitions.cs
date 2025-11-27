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
using MathNet.Numerics.Statistics;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Provides helpers for defining universes based on the daily dollar volume
    /// </summary>
    public class DollarVolumeUniverseDefinitions
    {
        private readonly QCAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="DollarVolumeUniverseDefinitions"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance, used for obtaining the default <see cref="UniverseSettings"/></param>
        public DollarVolumeUniverseDefinitions(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Creates a new coarse <see cref="Universe"/> that contains the top count of stocks
        /// by daily dollar volume
        /// </summary>
        /// <param name="count">The number of stock to select</param>
        /// <param name="universeSettings">The settings for stocks added by this universe.
        /// Defaults to <see cref="QCAlgorithm.UniverseSettings"/></param>
        /// <returns>A new coarse universe for the top count of stocks by dollar volume</returns>
        [Obsolete("This method is deprecated. Use method `Universe.Top(...)` instead")]
        public Universe Top(int count, UniverseSettings universeSettings = null)
        {
            return _algorithm.Universe.Top(count, universeSettings);
        }
    }
}
