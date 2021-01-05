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

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Defines settings required when adding a subscription
    /// </summary>
    public class UniverseSettings
    {
        /// <summary>
        /// The resolution to be used
        /// </summary>
        public Resolution Resolution;

        /// <summary>
        /// The leverage to be used
        /// </summary>
        public decimal Leverage;

        /// <summary>
        /// True to fill data forward, false otherwise
        /// </summary>
        public bool FillForward;

        /// <summary>
        /// True to allow extended market hours data, false otherwise
        /// </summary>
        public bool ExtendedMarketHours;

        /// <summary>
        /// Defines the minimum amount of time a security must be in
        /// the universe before being removed.
        /// </summary>
        /// <remarks>When selection takes place, the actual members time in the universe
        /// will be rounded based on this TimeSpan, so that relative small differences do not
        /// cause an unexpected behavior <see cref="Universe.CanRemoveMember"/></remarks>
        public TimeSpan MinimumTimeInUniverse;

        /// <summary>
        /// Defines how universe data is normalized before being send into the algorithm
        /// </summary>
        public DataNormalizationMode DataNormalizationMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseSettings"/> class
        /// </summary>
        /// <param name="resolution">The resolution</param>
        /// <param name="leverage">The leverage to be used</param>
        /// <param name="fillForward">True to fill data forward, false otherwise</param>
        /// <param name="extendedMarketHours">True to allow extended market hours data, false otherwise</param>
        /// <param name="minimumTimeInUniverse">Defines the minimum amount of time a security must remain in the universe before being removed</param>
        /// <param name="dataNormalizationMode">Defines how universe data is normalized before being send into the algorithm</param>
        public UniverseSettings(Resolution resolution, decimal leverage, bool fillForward, bool extendedMarketHours, TimeSpan minimumTimeInUniverse, DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted)
        {
            Resolution = resolution;
            Leverage = leverage;
            FillForward = fillForward;
            ExtendedMarketHours = extendedMarketHours;
            MinimumTimeInUniverse = minimumTimeInUniverse;
            DataNormalizationMode = dataNormalizationMode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseSettings"/> class
        /// </summary>
        public UniverseSettings(UniverseSettings universeSettings)
        {
            Resolution = universeSettings.Resolution;
            Leverage = universeSettings.Leverage;
            FillForward = universeSettings.FillForward;
            ExtendedMarketHours = universeSettings.ExtendedMarketHours;
            MinimumTimeInUniverse = universeSettings.MinimumTimeInUniverse;
            DataNormalizationMode = universeSettings.DataNormalizationMode;
        }
    }
}