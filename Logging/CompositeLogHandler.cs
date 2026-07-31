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
using System.Linq;

namespace QuantConnect.Logging
{
    /// <summary>
    /// Provides an <see cref="ILogHandler"/> implementation that composes multiple handlers
    /// </summary>
    public class CompositeLogHandler : ILogHandler
    {
        private readonly object _locker = new object();
        private ILogHandler[] _handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeLogHandler"/> that pipes log messages to the console and log.txt
        /// </summary>
        public CompositeLogHandler()
            : this(new ConsoleLogHandler(), new FileLogHandler())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeLogHandler"/> class from the specified handlers
        /// </summary>
        /// <param name="handlers">The implementations to compose</param>
        public CompositeLogHandler(params ILogHandler[] handlers)
        {
            if (handlers == null || handlers.Length == 0)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            _handlers = handlers;
        }

        /// <summary>
        /// Adds the given handler to the composition if a handler of the same type is not already present
        /// </summary>
        /// <param name="handler">The handler to add</param>
        /// <returns>True if the handler was added, false if a handler of the same type already exists</returns>
        public bool TryAddHandler(ILogHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            lock (_locker)
            {
                if (_handlers.Any(existing => existing.GetType() == handler.GetType()))
                {
                    return false;
                }

                // copy on write so concurrent log calls keep iterating a stable array
                var handlers = new ILogHandler[_handlers.Length + 1];
                _handlers.CopyTo(handlers, 0);
                handlers[_handlers.Length] = handler;
                _handlers = handlers;
                return true;
            }
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text"></param>
        public void Error(string text)
        {
            foreach (var handler in _handlers)
            {
                handler.Error(text);
            }
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text"></param>
        public void Debug(string text)
        {
            foreach (var handler in _handlers)
            {
                handler.Debug(text);
            }
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text"></param>
        public void Trace(string text)
        {
            foreach (var handler in _handlers)
            {
                handler.Trace(text);
            }
        }

        /// <summary>
        /// Write report message to log
        /// </summary>
        /// <param name="text">The report text to log</param>
        public void Report(string text)
        {
            foreach (var handler in _handlers)
            {
                handler.Report(text);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            foreach (var handler in _handlers)
            {
                handler.Dispose();
            }
        }
    }
}
