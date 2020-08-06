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
    /// BaseLogHandler that collect messages to later write to the result handler
    /// </summary>
    public abstract class BaseLogHandler : ILogHandler
    {
        private ConcurrentQueue<LogEntry> LogsQueue;
        private const string DateFormat = "yyyyMMdd HH:mm:ss";
        private Thread LogThread;

        /// <summary>
        /// Logging event delegate
        /// </summary>
        private static AutoResetEvent MessageEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Logging.BaseLogHandler"/> class.
        /// </summary>
        public BaseLogHandler()
        {
            LogsQueue = new ConcurrentQueue<LogEntry>();
            MessageEvent = new AutoResetEvent(false);
            LogThread = new Thread(WriteThread) { IsBackground = true, Name = "Logging Thread" };
            LogThread.Start();
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public void Error(string text)
        {
            var log = new LogEntry(text, DateTime.UtcNow, LogType.Error);
            LogsQueue.Enqueue(log);
            MessageEvent.Set();
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public void Debug(string text)
        {
            var log = new LogEntry(text, DateTime.UtcNow, LogType.Debug);
            LogsQueue.Enqueue(log);
            MessageEvent.Set();
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public void Trace(string text)
        {
            var log = new LogEntry(text, DateTime.UtcNow, LogType.Trace);
            LogsQueue.Enqueue(log);
            MessageEvent.Set();
        }

        /// <summary>
        /// Thread function in charge of dispersing log messages
        /// </summary>
        public void WriteThread()
        {
            try{
                while(true){
                    while (!LogsQueue.IsEmpty){
                        LogEntry entry;
                        bool success = LogsQueue.TryDequeue(out entry);

                        switch(entry.MessageType){
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

                    MessageEvent.Reset();
                }
            } catch {
                Console.WriteLine("Dead Thread me Boi");
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
            LogThread.Join(50);
        }

    }
}