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
                    var worker = new ParallelRunnerWorker(token, _processQueue);
                    worker.Start();
                    _workers[i] = worker;
                }

                waitHandles = _workers.Select(x => x.WaitHandle).ToArray();
            }

            Task.Run(() =>
            {
                WaitHandle.WaitAll(waitHandles);
                lock (_sync)
                {
                    // if the handle is already closed means we already were disposed
                    // and so were the workers
                    if (_waitHandle != null && !_waitHandle.SafeWaitHandle.IsClosed)
                    {
                        for (int i = 0; i < _threadCount; i++)
                        {
                            _workers[i].DisposeSafely();
                            _workers[i] = null;
                        }
                        _waitHandle.Set();
                    }
                }
            }, CancellationToken.None);

            _processQueueThread = new Thread(() => ProcessHoldQueue(token)) { IsBackground = true };
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
                foreach (var workItem in _holdQueue.GetConsumingEnumerable(token))
                {
                    if (workItem.IsReady)
                    {
                        _processQueue.Add(workItem, token);
                    }
                    else
                    {
                        _holdQueue.Add(workItem, token);
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
                if (_processQueueThread != null && _processQueueThread.IsAlive)
                {
                    try
                    {
                        if (!_processQueueThread.Join(TimeSpan.FromSeconds(1)))
                        {
                            _processQueueThread.Abort();
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                    }
                }

                foreach (var worker in _workers)
                {
                    worker?.DisposeSafely();
                }

                _holdQueue?.DisposeSafely();
                _processQueue?.DisposeSafely();

                // if IsClosed means its already disposed
                if (_waitHandle != null && !_waitHandle.SafeWaitHandle.IsClosed)
                {
                    _waitHandle.Set();
                    _waitHandle.DisposeSafely();
                }
            }
        }
    }
}