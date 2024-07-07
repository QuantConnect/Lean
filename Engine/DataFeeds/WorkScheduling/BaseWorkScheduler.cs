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

namespace QuantConnect.Lean.Engine.DataFeeds.WorkScheduling
{
    /// <summary>
    /// Base work scheduler abstraction
    /// </summary>
    public abstract class WorkScheduler
    {
        /// <summary>
        /// The quantity of workers to be used
        /// </summary>
        public static int WorkersCount = Configuration.Config.GetInt(
            "data-feed-workers-count",
            Environment.ProcessorCount
        );

        /// <summary>
        /// Add a new work item to the queue
        /// </summary>
        /// <param name="symbol">The symbol associated with this work</param>
        /// <param name="workFunc">The work function to run</param>
        /// <param name="weightFunc">The weight function.
        /// Work will be sorted in ascending order based on this weight</param>
        public abstract void QueueWork(
            Symbol symbol,
            Func<int, bool> workFunc,
            Func<int> weightFunc
        );
    }
}
