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
using System.IO;

namespace QuantConnect.Logging
{
    /// <summary>
    /// ILogHandler implementation that writes log output to console.
    /// </summary>
    public class ConsoleLogHandler : ILogHandler
    {
        private const string DateFormat = "yyyyMMdd HH:mm:ss";
        private readonly TextWriter _console;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Logging.ConsoleLogHandler"/> class.
        /// </summary>
        public ConsoleLogHandler()
        {
            // saves references to the real console text writer since in a deployed state we may overwrite this in order
            // to redirect messages from algorithm to result handler
            _console = Console.Out;
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public void Error(string text)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine(DateTime.Now.ToString(DateFormat) + " ERROR:: " + text);
            Console.ForegroundColor = original;
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public void Debug(string text)
        {
            _console.WriteLine(DateTime.Now.ToString(DateFormat) + " DEBUGGING :: " + text);
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public void Trace(string text)
        {
            _console.WriteLine(DateTime.Now.ToString(DateFormat) + " Trace:: " + text);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }
    }
}