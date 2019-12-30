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

namespace QuantConnect.Lean.Engine.DataFeeds.WorkScheduling
{
    internal class WorkQueue
    {
        private readonly List<WorkItem> _workQueue;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public WorkQueue()
        {
            _workQueue = new List<WorkItem>();
        }

        /// <summary>
        /// Adds a new item to this work queue
        /// </summary>
        /// <param name="work">The work to add</param>
        public void Add(WorkItem work)
        {
            lock (_workQueue)
            {
                _workQueue.Add(work);
            }
        }

        /// <summary>
        /// Updates the weights and sorts the work in the queue
        /// </summary>
        public void Sort()
        {
            lock (_workQueue)
            {
                foreach (var item in _workQueue)
                {
                    item.UpdateWeight();
                }
                _workQueue.Sort(WorkItem.Compare);
            }
        }

        /// <summary>
        /// Removes an item from the work queue
        /// </summary>
        /// <param name="workItem">The work item to remove</param>
        public void Remove(WorkItem workItem)
        {
            lock (_workQueue)
            {
                _workQueue.Remove(workItem);
            }
        }

        /// <summary>
        /// Gets the next work item to execute, null if none is available
        /// </summary>
        public WorkItem Get()
        {
            WorkItem potentialWorkItem = null;
            lock (_workQueue)
            {
                if (_workQueue.Count != 0)
                {
                    potentialWorkItem = _workQueue[0];

                    // if the weight is at its maximum value return null
                    // this is useful to space out in time this work
                    if (potentialWorkItem.Weight == WeightedWorkScheduler.MaxWorkWeight)
                    {
                        potentialWorkItem = null;
                    }
                }
            }
            return potentialWorkItem;
        }
    }
}
