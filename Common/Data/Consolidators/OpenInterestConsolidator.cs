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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Type capable of consolidating open interest 
    /// </summary>
    public class OpenInterestConsolidator : PeriodCountConsolidatorBase<Tick, OpenInterest>
    {
        /// <summary>
        /// Create a new OpenInterestConsolidator for the desired resolution
        /// </summary>
        /// <param name="resolution">The resolution desired</param>
        /// <returns>A consolidator that produces data on the resolution interval</returns>
        public static OpenInterestConsolidator FromResolution(Resolution resolution)
        {
            return new OpenInterestConsolidator(resolution.ToTimeSpan());
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'OpenInterest' representing the period
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public OpenInterestConsolidator(TimeSpan period)
            : base(period)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'OpenInterest' representing the last count pieces of data
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        public OpenInterestConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'OpenInterest' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public OpenInterestConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        /// <summary>
        /// Determines whether or not the specified data should be processd
        /// </summary>
        /// <param name="tick">The data to check</param>
        /// <returns>True if the consolidator should process this data, false otherwise</returns>
        protected override bool ShouldProcess(Tick tick)
        {
            return tick.TickType == TickType.OpenInterest;
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new OI bar</param>
        /// <param name="tick">The new data</param>
        protected override void AggregateBar(ref OpenInterest workingBar, Tick tick)
        {
            if (workingBar == null)
            {
                workingBar = new OpenInterest
                {
                    Symbol = tick.Symbol,
                    Time = GetRoundedBarTime(tick.Time),
                    Value = tick.Value
                };

            }
            else
            {
                //Update the working bar
                workingBar.Value = tick.Value;
            }
        }
    }
}