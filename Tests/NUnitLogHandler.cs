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
using NUnit.Framework;
using QuantConnect.Logging;

namespace QuantConnect.Tests
{
    /// <summary>
    /// ILogHandler implementation for NUnit tests that displays log output immediately.
    /// </summary>
    public class NUnitLogHandler : ILogHandler
    {
        private const string DefaultDateFormat = "yyyyMMdd HH:mm:ss.fff";
        private readonly TextWriter _trace;
        private readonly TextWriter _error;
        private readonly string _dateFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitLogHandler"/> class.
        /// </summary>
        public NUnitLogHandler()
            : this(DefaultDateFormat) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitLogHandler"/> class.
        /// </summary>
        /// <param name="dateFormat">Specifies the date format to use when writing log messages to the console window</param>
        public NUnitLogHandler(string dateFormat = DefaultDateFormat)
        {
            _trace = TestContext.Progress;
            _error = TestContext.Progress;
            _dateFormat = dateFormat;
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public virtual void Error(string text)
        {
            _error.WriteLine(
                $"{DateTime.UtcNow.ToString(_dateFormat, CultureInfo.InvariantCulture)} ERROR:: {text}"
            );
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public virtual void Debug(string text)
        {
            _trace.WriteLine(
                $"{DateTime.UtcNow.ToString(_dateFormat, CultureInfo.InvariantCulture)} DEBUG:: {text}"
            );
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public virtual void Trace(string text)
        {
            _trace.WriteLine(
                $"{DateTime.UtcNow.ToString(_dateFormat, CultureInfo.InvariantCulture)} TRACE:: {text}"
            );
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose() { }
    }
}
