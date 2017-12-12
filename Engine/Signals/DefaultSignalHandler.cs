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
using QuantConnect.Algorithm.Framework.Signals;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Signals
{
    /// <summary>
    /// Base signal handler that supports sending signals to the messaging handler
    /// </summary>
    public class DefaultSignalHandler : ISignalHandler
    {
        /// <inheritdoc />
        public bool IsActive => !_cancellationTokenSource.IsCancellationRequested;

        /// <summary>
        /// Gets the algorithm's unique identifier
        /// </summary>
        protected string AlgorithmId { get; private set; }

        /// <summary>
        /// Gets the algorithm instance
        /// </summary>
        protected IAlgorithm Algorithm { get; private set; }

        private IMessagingHandler _messagingHandler;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentQueue<Packet> _messages = new ConcurrentQueue<Packet>();

        /// <inheritdoc />
        public virtual void Initialize(AlgorithmNodePacket job, IAlgorithm algorithm, IMessagingHandler messagingHandler)
        {
            Algorithm = algorithm;
            AlgorithmId = job.AlgorithmId;
            _messagingHandler = messagingHandler;

            algorithm.SignalsGenerated += (algo, collection) => OnSignalsGenerated(collection);
        }

        /// <inheritdoc />
        public virtual void ProcessSynchronousEvents()
        {
        }

        /// <inheritdoc />
        public virtual void Run()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // run until cancelled AND we're processing messages
            while (!_cancellationTokenSource.IsCancellationRequested || !_messages.IsEmpty)
            {
                try
                {
                    ProcessAsynchronousEvents();
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    throw;
                }

                Thread.Sleep(50);
            }
        }

        /// <inheritdoc />
        public void Exit()
        {
            _cancellationTokenSource.Cancel(false);
        }

        /// <summary>
        /// Performs asynchronous processing, including broadcasting of signals to messaging handler
        /// </summary>
        protected virtual void ProcessAsynchronousEvents()
        {
            Packet packet;
            while (_messages.TryDequeue(out packet))
            {
                _messagingHandler.Send(packet);
            }
        }

        /// <summary>
        /// Enqueues a packet to be processed asynchronously
        /// </summary>
        /// <param name="packet">The packet</param>
        protected virtual void Enqueue(Packet packet)
        {
            _messages.Enqueue(packet);
        }

        /// <summary>
        /// Handles the algorithm's <see cref="IAlgorithm.SignalsGenerated"/> event
        /// and broadcasts the new signal using the messaging handler
        /// </summary>
        protected virtual void OnSignalsGenerated(SignalCollection collection)
        {
            Enqueue(new SignalPacket(AlgorithmId, collection));
        }
    }
}