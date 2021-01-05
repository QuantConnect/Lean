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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace QuantConnect.Lean.Engine.DataFeeds.WorkScheduling
{
    internal class WorkQueue : IWorkQueue
    {
        private readonly List<WorkItem> _workQueue;

        /// <summary>
        /// Event used to notify there is work ready to execute in this queue
        /// </summary>
        private AutoResetEvent _workAvailableEvent;

        /// <summary>
        /// Returns the thread priority to use for this work queue
        /// </summary>
        public ThreadPriority ThreadPriority => ThreadPriority.Lowest;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public WorkQueue()
        {
            _workQueue = new List<WorkItem>();
            _workAvailableEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Updates the weights and sorts the work in the queue
        /// </summary>
        public void Sort()
        {
            var notifyWork = false;
            lock (_workQueue)
            {
                for (var i = 0; i < _workQueue.Count; i++)
                {
                    _workQueue[i].UpdateWeight();
                    if (_workQueue[i].Weight < WeightedWorkScheduler.MaxWorkWeight)
                    {
                        notifyWork = true;
                    }
                }
                _workQueue.Sort(WorkItem.Compare);
            }
            if (notifyWork)
            {
                _workAvailableEvent.Set();
            }
        }

        /// <summary>
        /// This is the worker thread loop.
        /// It will first try to take a work item from the new work queue else will check his own queue.
        /// </summary>
        public void WorkerThread(ConcurrentQueue<WorkItem> newWork, AutoResetEvent newWorkEvent)
        {
            var waitHandles = new WaitHandle[] { _workAvailableEvent, newWorkEvent };
            while (true)
            {
                WorkItem workItem;
                if (!newWork.TryDequeue(out workItem))
                {
                    workItem = Get();
                    if (workItem == null)
                    {
                        // no work to do, lets sleep and try again
                        WaitHandle.WaitAny(waitHandles, 100);
                        continue;
                    }
                }
                else
                {
                    Add(workItem);
                }

                try
                {
                    if (!workItem.Work(WeightedWorkScheduler.WorkBatchSize))
                    {
                        Remove(workItem);
                    }
                }
                catch (Exception exception)
                {
                    Remove(workItem);
                    Logging.Log.Error(exception);
                }
            }
        }

        /// <summary>
        /// Adds a new item to this work queue
        /// </summary>
        /// <param name="work">The work to add</param>
        private void Add(WorkItem work)
        {
            lock (_workQueue)
            {
                _workQueue.Add(work);
            }
        }

        /// <summary>
        /// Removes an item from the work queue
        /// </summary>
        /// <param name="workItem">The work item to remove</param>
        private void Remove(WorkItem workItem)
        {
            lock (_workQueue)
            {
                _workQueue.Remove(workItem);
            }
        }

        /// <summary>
        /// Gets the next work item to execute, null if none is available
        /// </summary>
        private WorkItem Get()
        {
            WorkItem potentialWorkItem = null;
            lock (_workQueue)
            {
                if (_workQueue.Count != 0)
                {
                    potentialWorkItem = _workQueue[_workQueue.Count - 1];

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
