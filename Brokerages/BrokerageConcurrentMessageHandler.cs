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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Brokerage helper class to lock message stream while executing an action, for example placing an order
    /// </summary>
    /// <remarks>A thin wrapper around <see cref="BrokerageMessageQueue"/>: the lock, buffer and dispatch all
    /// live there now, so a second source with its own message type can be handled through the exact same
    /// lock this handler uses - see <see cref="RegisterMessageType{TMessage}"/>.</remarks>
    public class BrokerageConcurrentMessageHandler<T> : IDisposable
        where T : class
    {
        private readonly BrokerageMessageQueue _queue;

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
            _queue = new BrokerageMessageQueue(concurrencyEnabled);
            RegisterMessageType(processMessages);
        }

        /// <summary>
        /// Registers another message type this handler also processes, alongside <typeparamref name="T"/>.
        /// A source with its own message type - for example the order polling service - registers here once,
        /// then calls <see cref="HandleNewMessage{TMessage}"/> like anything else, and its messages queue
        /// behind the exact same lock as <typeparamref name="T"/> instead of getting a second, independent
        /// lock that would not actually serialize against this one.
        /// </summary>
        /// <param name="processMessages">The action to call for each new message of this type</param>
        public void RegisterMessageType<TMessage>(Action<TMessage> processMessages)
            where TMessage : class
        {
            _queue.MessageReceived += message =>
            {
                if (message is TMessage typed)
                {
                    processMessages(typed);
                }
            };
        }

        /// <summary>
        /// Disposes of the resources used by this instance
        /// </summary>
        public void Dispose()
        {
            _queue.Dispose();
        }

        /// <summary>
        /// Will process or enqueue a message for later processing it. Works for <typeparamref name="T"/> and
        /// for any other type registered through <see cref="RegisterMessageType{TMessage}"/> - the type is
        /// inferred from the argument, so the call looks the same either way.
        /// </summary>
        /// <param name="message">The new message</param>
        public void HandleNewMessage<TMessage>(TMessage message)
            where TMessage : class
        {
            if (message != null)
            {
                _queue.Enqueue(message);
            }
        }

        /// <summary>
        /// Lock the streaming processing while we're sending orders as sometimes they fill before the call returns.
        /// </summary>
        public void WithLockedStream(Action code)
        {
            _queue.WithLockedStream(code);
        }
    }
}
