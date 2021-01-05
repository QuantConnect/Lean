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
using System.Globalization;
using System.IO;

namespace QuantConnect.Logging
{
    /// <summary>
    /// ILogHandler implementation that writes log output to console.
    /// </summary>
    public class ConsoleLogHandler : ILogHandler
    {
        private const string DefaultDateFormat = "yyyyMMdd HH:mm:ss.fff";
        private readonly TextWriter _trace;
        private readonly TextWriter _error;
        private readonly string _dateFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Logging.ConsoleLogHandler"/> class.
        /// </summary>
        public ConsoleLogHandler()
            : this(DefaultDateFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Logging.ConsoleLogHandler"/> class.
        /// </summary>
        /// <param name="dateFormat">Specifies the date format to use when writing log messages to the console window</param>
        public ConsoleLogHandler(string dateFormat = DefaultDateFormat)
        {
            // saves references to the real console text writer since in a deployed state we may overwrite this in order
            // to redirect messages from algorithm to result handler
            _trace = Console.Out;
            _error = Console.Error;
            _dateFormat = dateFormat;
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public virtual void Error(string text)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
#endif
            _error.WriteLine(DateTime.Now.ToString(_dateFormat, CultureInfo.InvariantCulture) + " ERROR:: " + text);
#if DEBUG
            Console.ResetColor();
#endif
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public virtual void Debug(string text)
        {
            _trace.WriteLine(DateTime.Now.ToString(_dateFormat, CultureInfo.InvariantCulture) + " DEBUG:: " + text);
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public virtual void Trace(string text)
        {
            _trace.WriteLine(DateTime.Now.ToString(_dateFormat, CultureInfo.InvariantCulture) + " Trace:: " + text);
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