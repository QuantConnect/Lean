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
using System.Threading.Tasks;

namespace QuantConnect.Lean.Engine.DataFeeds.WorkScheduling
{
    /// <summary>
    /// This singleton class will create a thread pool to processes work
    /// that will be prioritized based on it's weight
    /// </summary>
    /// <remarks>The threads in the pool will take ownership of the
    /// <see cref="WorkItem"/> and not share it with another thread.
    /// This is required because the data enumerator stack yields, which state
    /// depends on the thread id</remarks>
    public class WeightedWorkScheduler : WorkScheduler
    {
        private static readonly Lazy<WeightedWorkScheduler> _instance =
            new Lazy<WeightedWorkScheduler>(() => new WeightedWorkScheduler());

        /// <summary>
        /// This is the size of each work sprint
        /// </summary>
        public const int WorkBatchSize = 50;

        /// <summary>
        /// This is the maximum size a work item can weigh,
        /// if reached, it will be ignored and not executed until its less
        /// </summary>
        /// <remarks>This is useful to limit RAM and CPU usage</remarks>
        public static int MaxWorkWeight;

        private readonly ConcurrentQueue<WorkItem> _newWork;
        private readonly AutoResetEvent _newWorkEvent;
        private Task _initializationTask;
        private readonly List<WeightedWorkQueue> _workerQueues;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static WeightedWorkScheduler Instance => _instance.Value;

        private WeightedWorkScheduler()
        {
            _newWork = new ConcurrentQueue<WorkItem>();
            _newWorkEvent = new AutoResetEvent(false);
            _workerQueues = new List<WeightedWorkQueue>(WorkersCount);

            _initializationTask = Task.Run(() =>
            {
                MaxWorkWeight = Configuration.Config.GetInt("data-feed-max-work-weight", 400);
                Logging.Log.Trace(
                    $"WeightedWorkScheduler(): will use {WorkersCount} workers and MaxWorkWeight is {MaxWorkWeight}"
                );

                for (var i = 0; i < WorkersCount; i++)
                {
                    var workQueue = new WeightedWorkQueue();
                    _workerQueues.Add(workQueue);
                    var thread = new Thread(() => workQueue.WorkerThread(_newWork, _newWorkEvent))
                    {
                        IsBackground = true,
                        Priority = workQueue.ThreadPriority,
                        Name = $"WeightedWorkThread{i}"
                    };
                    thread.Start();
                }
            });
        }

        /// <summary>
        /// Add a new work item to the queue
        /// </summary>
        /// <param name="symbol">The symbol associated with this work</param>
        /// <param name="workFunc">The work function to run</param>
        /// <param name="weightFunc">The weight function.
        /// Work will be sorted in ascending order based on this weight</param>
        public override void QueueWork(
            Symbol symbol,
            Func<int, bool> workFunc,
            Func<int> weightFunc
        )
        {
            _newWork.Enqueue(new WorkItem(workFunc, weightFunc));
            _newWorkEvent.Set();
        }

        /// <summary>
        /// Execute the given action in all workers once
        /// </summary>
        public void AddSingleCallForAll(Action action)
        {
            if (!_initializationTask.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Timeout waiting for worker threads to start");
            }

            for (var i = 0; i < _workerQueues.Count; i++)
            {
                _workerQueues[i].AddSingleCall(action);
            }
        }
    }
}
