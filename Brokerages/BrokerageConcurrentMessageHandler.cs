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
        private readonly ILock _lock;
        private readonly ManualResetEventSlim _messagesProcessedEvent;
        private readonly int _maxMessageBufferSize;

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
        /// <param name="concurrencyEnabled">Whether to enable concurrent order submission</param>
        public BrokerageConcurrentMessageHandler(Action<T> processMessages, bool concurrencyEnabled)
        {
            _processMessages = processMessages;
            _messageBuffer = new Queue<T>();
            _lock = concurrencyEnabled ? new ReaderWriterLockWrapper() : new MonitorWrapper();
            _messagesProcessedEvent = new ManualResetEventSlim(false);
            _maxMessageBufferSize = Config.GetInt("brokerage-concurrent-message-handler-buffer-size", 20);
        }

        /// <summary>
        /// Will process or enqueue a message for later processing it
        /// </summary>
        /// <param name="message">The new message</param>
        public void HandleNewMessage(T message)
        {
            lock (_messageBuffer)
            {
                if (_lock.TryEnterReadLockImmediately())
                {
                    try
                    {
                        ProcessMessages(message);
                    }
                    finally
                    {
                        _lock.ExitReadLock();
                    }
                }
                else if (message != default)
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
            var queueIsFull = false;
            lock (_messageBuffer)
            {
                if (_messageBuffer.Count >= _maxMessageBufferSize)
                {
                    queueIsFull = true;
                }
            }
            if (queueIsFull)
            {
                _messagesProcessedEvent.Wait();
                _messagesProcessedEvent.Reset();
            }

            _lock.EnterWriteLock();
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
                    var lockedStreams = _lock.CurrentWriteCount;

                    // we release the semaphore first so by the time we release '_messageBuffer' any new message is processed immediately and not enqueued
                    _lock.ExitWriteLock();
                    // only process if no other threads will process them after us
                    if (lockedStreams == 1)
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
                if (message != null)
                {
                    _messageBuffer.Enqueue(message);
                }

                // double check there isn't any pending message
                while (_messageBuffer.TryDequeue(out var e))
                {
                    try
                    {
                        _processMessages(e);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            }
            finally
            {
                _messagesProcessedEvent.Set();
            }
        }

        private interface ILock
        {
            int CurrentWriteCount { get; }

            void EnterReadLock();

            void ExitReadLock();

            bool TryEnterReadLockImmediately();

            void EnterWriteLock();

            void ExitWriteLock();
        }

        /// <summary>
        /// A simple reader/writer lock implementation that allows us to switch the meaning of read and write locks
        /// so that it can be used for single reader and multiple writers scenario.
        /// </summary>
        private class ReaderWriterLockWrapper : ILock
        {
            private readonly ReaderWriterLockSlim _lock;

            public int CurrentWriteCount => _lock.CurrentReadCount;

            public ReaderWriterLockWrapper()
            {
                _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            }

            public void EnterReadLock() => _lock.EnterWriteLock();
            public void ExitReadLock() => _lock.ExitWriteLock();
            public bool TryEnterReadLockImmediately() => _lock.TryEnterWriteLock(0);
            public void EnterWriteLock() => _lock.EnterReadLock();
            public void ExitWriteLock() => _lock.ExitReadLock();
        }

        private class MonitorWrapper : ILock
        {
            private readonly object _lockObject;
            private bool _takenForWriting;

            public int CurrentWriteCount => _takenForWriting ? 1 : 0;

            public MonitorWrapper()
            {
                _lockObject = new object();
            }

            public void EnterReadLock() => Monitor.Enter(_lockObject);

            public void ExitReadLock() => Monitor.Exit(_lockObject);

            public bool TryEnterReadLockImmediately() => Monitor.TryEnter(_lockObject);

            public void EnterWriteLock()
            {
                Monitor.Enter(_lockObject);
                _takenForWriting = true;
            }

            public void ExitWriteLock()
            {
                Monitor.Exit(_lockObject);
                _takenForWriting = false;
            }
        }
    }
}
