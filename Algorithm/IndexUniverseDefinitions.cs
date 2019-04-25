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

using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Provides helpers for defining universes based on index definitions
    /// </summary>
    public class IndexUniverseDefinitions
    {
        private readonly QCAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexUniverseDefinitions"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance, used for obtaining the default <see cref="UniverseSettings"/></param>
        public IndexUniverseDefinitions(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Creates a new <see cref="ConstituentsUniverse"/> universe that contains the
        /// constituents of QC500 index based on the company fundamentals.
        /// The algorithm creates a default tradable and liquid universe containing 500 US equities
        /// which are chosen at the first trading day of each month.
        /// </summary>
        /// <returns>A new <see cref="ConstituentsUniverse"/> universe for the top 500 stocks by dollar volume</returns>
        public Universe QC500 => new ConstituentsUniverse(
            Symbol.Create("constituents-universe-qc500", SecurityType.Equity, Market.USA),
            _algorithm.UniverseSettings);
    }
}