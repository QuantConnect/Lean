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

using NLog;

namespace QuantConnect.Logging
{
    /// <summary>
    /// Provides an implementation of <see cref="ILogHandler"/> that writes all log messages to a file on disk.
    /// </summary>
    public class NLogHandler : ILogHandler
    {
        private bool _disposed;
        private Logger _logger = LogManager.GetLogger("QuantConnect");

        // we need to control synchronization to our stream writer since it's not inherently thread-safe
        private readonly object _lock = new object();


        /// <summary>
        /// Initializes a new instance of the <see cref="NLogHandler"/> class to write messages to the specified file path.
        /// </summary>
        public NLogHandler()
        {

        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public void Error(string text)
        {
            _logger.Error(text);
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public void Debug(string text)
        {
            _logger.Debug(text);
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public void Trace(string text)
        {
            _logger.Trace(text);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Creates the message to be logged
        /// </summary>
        /// <param name="text">The text to be logged</param>
        /// <param name="level">The logging leel</param>
        /// <returns></returns>
        protected virtual string CreateMessage(string text, string level)
        {
            return $"{level}:: {text}";
        }

        /// <summary>
        /// Writes the message to the writer
        /// </summary>
        private void WriteMessage(string text, string level)
        {
            lock (_lock)
            {
                if (_disposed) return;
                switch (level.ToUpperInvariant())
                {
                    case "DEBUG":
                        Debug(CreateMessage(text, level));
                        return;
                    case "TRACE":
                        Trace(CreateMessage(text, level));
                        return;
                    case "ERROR":
                        Error(CreateMessage(text, level));
                        return;
                    default:
                        _logger.Trace(CreateMessage(text, level));
                        return;
                }
            }
        }
    }
}
