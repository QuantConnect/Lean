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
using QuantConnect.Scheduling;
using System.Collections.Generic;

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
        /// If configured, will be used to determine universe selection schedule and filter or skip selection data
        /// that does not fit the schedule
        /// </summary>
        public Schedule Schedule;

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
        /// Defines how universe data is mapped together
        /// </summary>
        /// <remarks>This is particular useful when generating continuous futures</remarks>
        public DataMappingMode DataMappingMode;

        /// <summary>
        /// The continuous contract desired offset from the current front month.
        /// For example, 0 (default) will use the front month, 1 will use the back month contra
        /// </summary>
        public int ContractDepthOffset;

        /// <summary>
        /// Allows a universe to specify which data types to add for a selected symbol
        /// </summary>
        public List<Tuple<Type, TickType>> SubscriptionDataTypes;

        /// <summary>
        /// True if universe selection can run asynchronous
        /// </summary>
        public bool? Asynchronous;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseSettings"/> class
        /// </summary>
        /// <param name="resolution">The resolution</param>
        /// <param name="leverage">The leverage to be used</param>
        /// <param name="fillForward">True to fill data forward, false otherwise</param>
        /// <param name="extendedMarketHours">True to allow extended market hours data, false otherwise</param>
        /// <param name="minimumTimeInUniverse">Defines the minimum amount of time a security must remain in the universe before being removed</param>
        /// <param name="dataNormalizationMode">Defines how universe data is normalized before being send into the algorithm</param>
        /// <param name="dataMappingMode">The contract mapping mode to use for the security</param>
        /// <param name="contractDepthOffset">The continuous contract desired offset from the current front month.
        /// For example, 0 (default) will use the front month, 1 will use the back month contract</param>
        /// <param name="asynchronous">True if universe selection can run asynchronous</param>
        /// <param name="selectionDateRule">If provided, will be used to determine universe selection schedule</param>
        public UniverseSettings(Resolution resolution, decimal leverage, bool fillForward, bool extendedMarketHours, TimeSpan minimumTimeInUniverse, DataNormalizationMode dataNormalizationMode = DataNormalizationMode.Adjusted,
            DataMappingMode dataMappingMode = DataMappingMode.OpenInterest, int contractDepthOffset = 0, bool? asynchronous = null, IDateRule selectionDateRule = null)
        {
            Resolution = resolution;
            Leverage = leverage;
            FillForward = fillForward;
            DataMappingMode = dataMappingMode;
            ContractDepthOffset = contractDepthOffset;
            ExtendedMarketHours = extendedMarketHours;
            MinimumTimeInUniverse = minimumTimeInUniverse;
            DataNormalizationMode = dataNormalizationMode;
            Asynchronous = asynchronous;
            Schedule = new Schedule();
            if (selectionDateRule != null)
            {
                Schedule.On(selectionDateRule);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseSettings"/> class
        /// </summary>
        public UniverseSettings(UniverseSettings universeSettings)
        {
            Resolution = universeSettings.Resolution;
            Leverage = universeSettings.Leverage;
            FillForward = universeSettings.FillForward;
            DataMappingMode = universeSettings.DataMappingMode;
            ContractDepthOffset = universeSettings.ContractDepthOffset;
            ExtendedMarketHours = universeSettings.ExtendedMarketHours;
            MinimumTimeInUniverse = universeSettings.MinimumTimeInUniverse;
            DataNormalizationMode = universeSettings.DataNormalizationMode;
            SubscriptionDataTypes = universeSettings.SubscriptionDataTypes;
            Asynchronous = universeSettings.Asynchronous;
            Schedule = universeSettings.Schedule.Clone();
        }
    }
}
