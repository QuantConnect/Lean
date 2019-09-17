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
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace QuantConnect.Logging
{
    /// <summary>
    /// ILogHandler implementation that queues all logs and writes them when instructed.
    /// </summary>
    public class QueueLogHandler : ILogHandler
    {
        private readonly ConcurrentQueue<LogEntry> _logs;
        private const string DateFormat = "yyyyMMdd HH:mm:ss";
        private readonly TextWriter _trace;
        private readonly TextWriter _error;

        /// <summary>
        /// Public access to the queue for log processing.
        /// </summary>
        public ConcurrentQueue<LogEntry> Logs
        {
            get { return _logs; }
        }

        /// <summary>
        /// LOgging event delegate
        /// </summary>
        public delegate void LogEventRaised(LogEntry log);

        /// <summary>
        /// Logging Event Handler
        /// </summary>
        public event LogEventRaised LogEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueLogHandler"/> class.
        /// </summary>
        public QueueLogHandler()
        {
            _logs = new ConcurrentQueue<LogEntry>();
            _trace = Console.Out;
            _error = Console.Error;
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public void Error(string text)
        {
            var log = new LogEntry(text, DateTime.Now, LogType.Error);
            _logs.Enqueue(log);
            OnLogEvent(log);

            Console.ForegroundColor = ConsoleColor.Red;
            _error.WriteLine(DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture) + " Error:: " + text);
            Console.ResetColor();
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public void Debug(string text)
        {
            var log = new LogEntry(text, DateTime.Now, LogType.Debug);
            _logs.Enqueue(log);
            OnLogEvent(log);

            _trace.WriteLine(DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture) + " Debug:: " + text);
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public void Trace(string text)
        {
            var log = new LogEntry(text, DateTime.Now, LogType.Trace);
            _logs.Enqueue(log);
            OnLogEvent(log);

            _trace.WriteLine(DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture) + " Trace:: " + text);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

        /// <summary>
        /// Raise a log event safely
        /// </summary>
        protected virtual void OnLogEvent(LogEntry log)
        {
            var handler = LogEvent;

            if (handler != null)
            {
                handler(log);
            }
        }
    }
}