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
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;

namespace QuantConnect.Util
{
    /// <summary>
    /// Controller type used to schedule <see cref="IParallelRunnerWorkItem"/> instances
    /// to run on dedicated runner threads
    /// </summary>
    public class ParallelRunnerController : IDisposable
    {
        private Thread _processQueueThread;

        private readonly int _threadCount;
        private readonly object _sync = new object();
        private readonly ManualResetEvent _waitHandle;
        private readonly ParallelRunnerWorker[] _workers;
        private readonly BlockingCollection<IParallelRunnerWorkItem> _holdQueue;
        private readonly BlockingCollection<IParallelRunnerWorkItem> _processQueue;

        /// <summary>
        /// Gets a wait handle that can be used to wait for this controller
        /// to finish all scheduled work
        /// </summary>
        public WaitHandle WaitHandle 
        {
            get { return _waitHandle; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelRunnerController"/> class
        /// </summary>
        /// <param name="threadCount">The number of dedicated threads to spin up</param>
        public ParallelRunnerController(int threadCount)
        {
            _threadCount = threadCount;
            _waitHandle = new ManualResetEvent(false);
            _workers = new ParallelRunnerWorker[threadCount];
            _holdQueue = new BlockingCollection<IParallelRunnerWorkItem>();
            _processQueue = new BlockingCollection<IParallelRunnerWorkItem>();
        }

        /// <summary>
        /// Schedules the specified work item to run
        /// </summary>
        /// <param name="workItem">The work item to schedule</param>
        public void Schedule(IParallelRunnerWorkItem workItem)
        {
            if (workItem.IsReady) _processQueue.Add(workItem);
            else _holdQueue.Add(workItem);
        }
        /// <summary>
        /// Starts this instance of <see cref="ParallelRunnerController"/>.
        /// This method is indempotent
        /// </summary>
        /// <param name="token">The cancellation token</param>
        public void Start(CancellationToken token)
        {
            WaitHandle[] waitHandles;
            lock (_sync)
            {
                if (_workers[0] != null) return;
                for (int i = 0; i < _threadCount; i++)
                {
                    var worker = new ParallelRunnerWorker(this, _processQueue);
                    worker.Start(token);
                    _workers[i] = worker;
                }

                waitHandles = _workers.Select(x => x.WaitHandle).ToArray();
            }

            Task.Run(() =>
            {
                WaitHandle.WaitAll(waitHandles);
                _waitHandle.Set();

                foreach (var worker in _workers)
                {
                    worker.Dispose();
                }

            }, CancellationToken.None);

            _processQueueThread = new Thread(() => ProcessHoldQueue(token));
            _processQueueThread.Start();
        }
        /// <summary>
        /// Processes the internal hold queue checking to see if work
        /// items are ready to run
        /// </summary>
        /// <param name="token">The cancellation token</param>
        private void ProcessHoldQueue(CancellationToken token)
        {
            try
            {
                int count = 0;
                foreach (var workItem in _holdQueue.GetConsumingEnumerable(token))
                {
                    if (workItem.IsReady)
                    {
                        _processQueue.Add(workItem, token);
                        count = 0;
                    }
                    else
                    {
                        _holdQueue.Add(workItem, token);
                        if (count++ > _holdQueue.Count)
                        {
                            count = 0;
                            Thread.Sleep(1);
                        }
                    }
                }
            }
            catch (OperationCanceledException err)
            {
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            lock (_sync)
            {
                if (_holdQueue != null) _holdQueue.Dispose();
                if (_processQueue != null) _processQueue.Dispose();
                if (_processQueueThread != null && _processQueueThread.IsAlive) _processQueueThread.Abort();

                foreach (var worker in _workers)
                {
                    worker.Dispose();
                }

                if (_waitHandle != null)
                {
                    _waitHandle.Set();
                    _waitHandle.Dispose();
                }
            }
        }
    }
    /// <summary>
    /// Runner type used to run <see cref="IParallelRunnerWorkItem"/>
    /// </summary>
    public class ParallelRunnerWorker : IDisposable
    {
        private Thread _thread;
        private readonly object _sync = new object();
        private readonly ManualResetEvent _waitHandle;
        private readonly ParallelRunnerController _controller;
        private readonly BlockingCollection<IParallelRunnerWorkItem> _queue;
        /// <summary>
        /// Gets a wait handle that can be used to wait for this worker
        /// to finished all work in the queue, that is, when <see cref="BlockingCollection{T}.IsAddingCompleted"/> equals true.
        /// </summary>
        public WaitHandle WaitHandle
        {
            get { return _waitHandle; }
        }
        /// <summary>
        /// Initialzies a new instance of the <see cref="ParallelRunnerWorker"/> class
        /// </summary>
        /// <param name="controller">The controller instance used to reschedule work items</param>
        /// <param name="queue">The work queue where this worker will source the work items</param>
        public ParallelRunnerWorker(ParallelRunnerController controller, BlockingCollection<IParallelRunnerWorkItem> queue)
        {
            _queue = queue;
            _controller = controller;
            _waitHandle = new ManualResetEvent(false);
        }
        /// <summary>
        /// Starts a new thread to process the work queue.
        /// This method is indempotent.
        /// </summary>
        /// <param name="token">The cancellation token</param>
        public void Start(CancellationToken token)
        {
            lock (_sync)
            {
                if (_thread != null) return;
                _thread = new Thread(() => ThreadEntry(token));
                _thread.Start();
            }
        }
        /// <summary>
        /// Main entry point for the worker thread
        /// </summary>
        private void ThreadEntry(CancellationToken token)
        {
            try
            {
                foreach (var workItem in _queue.GetConsumingEnumerable(token))
                {
                    try
                    {
                        workItem.Execute();
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                    }
                }
            }
            catch (OperationCanceledException err)
            {
                if (!token.IsCancellationRequested)
                {
                    Log.Error(err);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            lock (_sync)
            {
                if (_waitHandle != null) _waitHandle.Dispose();
                if (_thread != null && _thread.IsAlive) _thread.Abort();
            }
        }
    }

    /// <summary>
    /// Result type class used to denote what to do with finished work items.
    /// </summary>
    public class ParallelRunnerWorkResult
    {
        public static readonly ParallelRunnerWorkResult Reenqueue = new ParallelRunnerWorkResult(true);
        public static readonly ParallelRunnerWorkResult Finalized = new ParallelRunnerWorkResult(false);

        public readonly bool ShouldReenqueue;

        protected ParallelRunnerWorkResult(bool reenqueue)
        {
            ShouldReenqueue = reenqueue;
        }
    }

    public interface IParallelRunnerWorkItem
    {
        /// <summary>
        /// Determines if this work item is ready to be processed
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Executes this work item
        /// </summary>
        /// <returns>The result of execution</returns>
        void Execute();
    }

    public sealed class FuncParallelRunnerWorkItem : IParallelRunnerWorkItem
    {
        private readonly Func<bool> _isReady;
        private readonly Action _execute;

        public bool IsReady
        {
            get { return _isReady(); }
        }

        public FuncParallelRunnerWorkItem(Func<bool> isReady, Action execute)
        {
            _isReady = isReady;
            _execute = execute;
        }

        public void Execute()
        {
            _execute();
        }
    }
}