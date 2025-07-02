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
*/

using System;
using System.Threading;
using QuantConnect.Logging;
using System.Collections.Generic;
using QuantConnect.Configuration;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Brokerage helper class to lock message stream while executing an action, for example placing an order
    /// </summary>
    public class BrokerageConcurrentMessageHandler<T> where T : class
    {
        private readonly Action<T> _processMessages;
        private readonly Queue<T> _messageBuffer;
        private readonly object _producersLock;
        private readonly int _maxConcurrentThreads;
        private readonly Semaphore _semaphore;
        private int _currentProducers;
        private readonly AutoResetEvent _messagesProcessedEvent;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="processMessages">The action to call for each new message</param>
        public BrokerageConcurrentMessageHandler(Action<T> processMessages)
            : this(processMessages, false)
        {
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="processMessages">The action to call for each new message</param>
        /// <param name="concurrentSubmissionEnabled">Whether to enable concurrent order submission</param>
        public BrokerageConcurrentMessageHandler(Action<T> processMessages, bool concurrentSubmissionEnabled)
        {
            _processMessages = processMessages;
            _messageBuffer = new Queue<T>();
            _producersLock = new object();
            _maxConcurrentThreads = concurrentSubmissionEnabled
                ? Config.GetInt("maximum-transaction-threads", 4)
                : 1;
            _semaphore = new Semaphore(_maxConcurrentThreads, _maxConcurrentThreads);
            _messagesProcessedEvent = new AutoResetEvent(false);
        }

        /// <summary>
        /// Will process or enqueue a message for later processing it
        /// </summary>
        /// <param name="message">The new message</param>
        public void HandleNewMessage(T message)
        {
            var processedMessages = false;
            lock (_messageBuffer)
            {
                if (_semaphore.WaitOne(0))
                {
                    try
                    {
                        lock (_producersLock)
                        {
                            if (_currentProducers == 0)
                            {
                                ProcessMessages(message);
                                processedMessages = true;
                            }
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                if (!processedMessages && message != default)
                {
                    // if someone has the lock just enqueue the new message they will process any remaining messages
                    // if by chance they are about to free the lock, no worries, we will always process first any remaining message first see 'ProcessMessages'
                    _messageBuffer.Enqueue(message);
                }
            }
        }

        /// <summary>
        /// Lock the streaming processing while we're sending orders as sometimes they fill before the call returns.
        /// </summary>
        public void WithLockedStream(Action code)
        {
            // Do not allow adding more than 20 messages to the buffer
            while (_messageBuffer.Count >= 20)
            {
                _messagesProcessedEvent.WaitOne(1000);
            }

            _semaphore.WaitOne();
            lock(_producersLock)
            {
                _currentProducers++;
            }

            try
            {
                code();
            }
            finally
            {
                // once we finish our 'code' we will process any message that come through,
                // to make sure no message get's left behind (race condition between us finishing 'ProcessMessages'
                // and some message being enqueued to it, we just take a lock on the buffer
                lock (_messageBuffer)
                {
                    lock (_producersLock)
                    {
                        _currentProducers--;
                    }

                    // we release the semaphore first so by the time we release '_messageBuffer' any new message is processed immediately and not enqueued
                    var currentLocks = _semaphore.Release();
                    // only process if no other threads will process them after us
                    if (currentLocks == _maxConcurrentThreads - 1 && _currentProducers == 0)
                    {
                        ProcessMessages();
                    }
                }
            }
        }

        /// <summary>
        /// Process any pending message and the provided one if any
        /// </summary>
        /// <remarks>To be called owing the stream lock</remarks>
        private void ProcessMessages(T message = null)
        {
            try
            {
                // double check there isn't any pending message
                while (_messageBuffer.TryDequeue(out var e))
                {
                    _processMessages(e);
                }

                if (message != null)
                {
                    _processMessages(message);
                }

                _messagesProcessedEvent.Set();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
