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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;

namespace QuantConnect.Util
{
    /// <summary>
    /// Starts a set number of threads that work can be scheduled to
    /// </summary>
    public class ParallelRunner : IDisposable
    {
        private int _skipped;
        private readonly BlockingCollection<Func<WorkResult>> _queue = new BlockingCollection<Func<WorkResult>>();
        private ManualResetEvent _waitHandle;

        /// <summary>
        /// Gets the wait handle for the execution
        /// </summary>
        public WaitHandle WaitHandle
        {
            get { return _waitHandle; }
        }

        /// <summary>
        /// Get true if the operation has completed, false if it is still running.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Schedules the specified delegate to run
        /// </summary>
        /// <param name="work">The work item to be run</param>
        public void Schedule(Func<WorkResult> work)
        {
            _queue.Add(work);
        }

        /// <summary>
        /// Schedules the specified delegates to run
        /// </summary>
        /// <param name="work">The work items to be scheduled</param>
        public void Schedule(IEnumerable<Func<WorkResult>> work)
        {
            foreach (var func in work)
            {
                Schedule(func);
            }
        }

        /// <summary>
        /// Starts the specified number of threads and returns an instance of <see cref="ParallelRunner"/>
        /// that can be used to schedule delegates to run
        /// </summary>
        /// <param name="threadCount">The number of threads to run on</param>
        /// <param name="token">Use for cancellation support</param>
        /// <returns>A new instance of <see cref="ParallelRunner"/> that can be used to schedule work to run</returns>
        public static ParallelRunner Run(int threadCount, CancellationToken token)
        {
            var parallel = new ParallelRunner();
            parallel._waitHandle = new ManualResetEvent(false);

            Log.Trace(typeof(ParallelRunner).Name + ".Execute(): Launching " + threadCount + " threads.");

            var handles = new WaitHandle[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                var manualResetEvent = new ManualResetEvent(false);
                handles[i] = manualResetEvent;
                var thread = new Thread(() =>
                {
                    try
                    {
                        parallel.ThreadEntry(token);
                    }
                    catch (OperationCanceledException err)
                    {
                        // do nothing if the token cancelled
                        if (!token.IsCancellationRequested)
                        {
                            Log.Error(err);
                        }
                        else
                        {
                            Log.Trace(typeof(ParallelRunner).Name + ".Execute(): Cancellation requested.");
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                    }
                    finally
                    {
                        manualResetEvent.Set();
                    }
                });
                thread.Name = "DedicatedThreadParallel " + (i + 1);
                thread.Start();
            }

            Task.Run(() =>
            {
                WaitHandle.WaitAll(handles);
                parallel._waitHandle.Set();

                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i].Dispose();
                }
            }, CancellationToken.None);

            return parallel;
        }

        private void ThreadEntry(CancellationToken token)
        {
            foreach (var workItem in _queue.GetConsumingEnumerable(token))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var workResult = workItem();
                    switch (workResult)
                    {
                        case WorkResult.Skipped:
                            // throttle if we've skipped more than queue.Count
                            if (Interlocked.Increment(ref _skipped) > _queue.Count) Thread.Sleep(1);
                            _queue.Add(workItem, token);
                            break;
                        
                        case WorkResult.Executed:
                            if (_skipped != 0) Interlocked.Decrement(ref _skipped);_queue.Add(workItem, token);
                            break;
                        
                        case WorkResult.Finalized:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (OperationCanceledException err)
                {
                    // do nothing if the token cancelled
                    if (!token.IsCancellationRequested)
                    {
                        Log.Error(err);
                    }
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_waitHandle != null) _waitHandle.Dispose();
        }


        /// <summary>
        /// Specifies the result from an operation
        /// </summary>
        public enum WorkResult
        {
            /// <summary>
            /// The operation was skipped
            /// </summary>
            Skipped,
            /// <summary>
            /// The operation completed execution
            /// </summary>
            Executed,
            /// <summary>
            /// The operation is finalized and will not be reenqueued
            /// </summary>
            Finalized
        }
    }
}