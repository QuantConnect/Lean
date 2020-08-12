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

using System.Collections.Concurrent;
using System.Threading;

namespace QuantConnect.Lean.Engine.DataFeeds.WorkScheduling
{
    /// <summary>
    /// Work queue abstraction
    /// </summary>
    public interface IWorkQueue
    {
        /// <summary>
        /// This is the worker thread loop.
        /// It will first try to take a work item from the new work queue else will check his own queue.
        /// </summary>
        void WorkerThread(ConcurrentQueue<WorkItem> newWork, AutoResetEvent newWorkEvent);

        /// <summary>
        /// Sorts the work queue
        /// </summary>
        void Sort();

        /// <summary>
        /// Returns the thread priority to use for this work queue
        /// </summary>
        ThreadPriority ThreadPriority { get; }
    }
}
