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
using System.Threading;
using QuantConnect.Logging;

namespace QuantConnect.Util
{
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
}