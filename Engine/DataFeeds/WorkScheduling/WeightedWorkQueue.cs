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
using System.Runtime.CompilerServices;
using System.Threading;

namespace QuantConnect.Lean.Engine.DataFeeds.WorkScheduling
{
    internal class WeightedWorkQueue
    {
        private int _pointer;
        private bool _removed;
        private Action _singleCallWork;
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
        public WeightedWorkQueue()
        {
            _workQueue = new List<WorkItem>();
            _workAvailableEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// This is the worker thread loop.
        /// It will first try to take a work item from the new work queue else will check his own queue.
        /// </summary>
        public void WorkerThread(ConcurrentQueue<WorkItem> newWork, AutoResetEvent newWorkEvent)
        {
            var waitHandles = new WaitHandle[] { _workAvailableEvent, newWorkEvent };
            var waitedPreviousLoop = 0;
            while (true)
            {
                WorkItem workItem;
                if (!newWork.TryDequeue(out workItem))
                {
                    workItem = Get();
                    if (workItem == null)
                    {
                        if (_singleCallWork != null)
                        {
                            try
                            {
                                _singleCallWork();
                            }
                            catch (Exception exception)
                            {
                                // this shouldn't happen but just in case
                                Logging.Log.Error(exception);
                            }
                            // we execute this once only and clear it's reference
                            _singleCallWork = null;
                        }

                        // no work to do, lets sleep and try again
                        WaitHandle.WaitAny(
                            waitHandles,
                            Math.Min(1 + (waitedPreviousLoop * 10), 250)
                        );
                        waitedPreviousLoop++;
                        continue;
                    }
                }
                else
                {
                    Add(workItem);
                }

                try
                {
                    waitedPreviousLoop = 0;
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
            _workQueue.Add(work);
        }

        /// <summary>
        /// Adds a new item to this work queue
        /// </summary>
        /// <param name="work">The work to add</param>
        public void AddSingleCall(Action work)
        {
            _singleCallWork = work;
            _workAvailableEvent.Set();
        }

        /// <summary>
        /// Removes an item from the work queue
        /// </summary>
        /// <param name="workItem">The work item to remove</param>
        private void Remove(WorkItem workItem)
        {
            _workQueue.Remove(workItem);
            _removed = true;
        }

        /// <summary>
        /// Gets the next work item to process
        /// </summary>
        /// <returns>The work item to process, null if none available</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected WorkItem Get()
        {
            var count = _workQueue.Count;
            if (count == 0)
            {
                return null;
            }
            var countFactor = (10 + 10 / count) / 10;

            if (_removed)
            {
                // if we removed an item don't really trust the pointer any more
                _removed = false;
                _pointer = Math.Min(_pointer, count - 1);
            }

            var initial = _pointer;
            do
            {
                var item = _workQueue[_pointer++];
                if (_pointer >= count)
                {
                    _pointer = 0;

                    // this will only really make a difference if there are many work items
                    if (25 > count)
                    {
                        // if we looped around let's sort the queue leave the jobs with less points at the start
                        _workQueue.Sort(WorkItem.Compare);
                    }
                }

                if (item.UpdateWeight() < WeightedWorkScheduler.MaxWorkWeight * countFactor)
                {
                    return item;
                }
            } while (initial != _pointer);

            // no work item is ready, pointer still will keep it's same value
            return null;
        }
    }
}
