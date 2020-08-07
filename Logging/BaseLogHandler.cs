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
using System.Threading;

namespace QuantConnect.Logging
{
    /// <summary>
    /// BaseLogHandler that collect messages in a queue and then writes them with a dedicated thread
    /// </summary>
    public abstract class BaseLogHandler : ILogHandler
    {
        private readonly ConcurrentQueue<LogEntry> _logsQueue;
        private const string DateFormat = "yyyyMMdd HH:mm:ss";
        private readonly Thread _logThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AutoResetEvent _messageEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Logging.BaseLogHandler"/> class.
        /// </summary>
        public BaseLogHandler()
        {
            _logsQueue = new ConcurrentQueue<LogEntry>();
            _messageEvent = new AutoResetEvent(false);
            _cancellationTokenSource = new CancellationTokenSource();
            _logThread = new Thread(WriteThread) { IsBackground = true, Name = "Logging Thread" };
            _logThread.Start();
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public void Error(string text)
        {
            var log = new LogEntry(text, DateTime.UtcNow, LogType.Error);
            _logsQueue.Enqueue(log);
            _messageEvent.Set();
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public void Debug(string text)
        {
            var log = new LogEntry(text, DateTime.UtcNow, LogType.Debug);
            _logsQueue.Enqueue(log);
            _messageEvent.Set();
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public void Trace(string text)
        {
            var log = new LogEntry(text, DateTime.UtcNow, LogType.Trace);
            _logsQueue.Enqueue(log);
            _messageEvent.Set();
        }

        /// <summary>
        /// Thread function in charge of dispersing log messages
        /// </summary>
        public void WriteThread()
        {
            try
            {
                while(!_cancellationTokenSource.IsCancellationRequested && _messageEvent.WaitOne())
                {
                    LogEntry entry;

                    while (_logsQueue.TryDequeue(out entry))
                    {
                        switch(entry.MessageType)
                        {
                            case LogType.Trace:
                                WriteTrace(entry.Message);
                                break;
                            case LogType.Debug:
                                WriteDebug(entry.Message);
                                break;
                            case LogType.Error:
                                WriteError(entry.Message);
                                break;
                        }
                    }
                }
            } catch (Exception exception)
            {
                Console.WriteLine("Logging thread has failed: " + exception);
            }
        }

        /// <summary>
        /// Function to write a trace statement
        /// </summary>
        public abstract void WriteTrace(string message);

        /// <summary>
        /// Function to write a debug statement
        /// </summary>
        public abstract void WriteDebug(string message);

        /// <summary>
        /// Function to write a error statement
        /// </summary>
        public abstract void WriteError(string message);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public virtual void Dispose()
        {
            try
            {   
                _cancellationTokenSource.Cancel();
                _messageEvent.Set();
                _logThread.Join(500);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Time out on closing logging thread: " + exception);
            }
        }

    }
}