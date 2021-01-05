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
using System.ComponentModel.Composition;
using System.Globalization;

namespace QuantConnect.Logging
{
    /// <summary>
    /// ILogHandler implementation that writes log output to result handler
    /// </summary>
    [PartNotDiscoverable]
    public class FunctionalLogHandler : ILogHandler
    {
        private const string DateFormat = "yyyyMMdd HH:mm:ss";
        private readonly Action<string> _debug;
        private readonly Action<string> _trace;
        private readonly Action<string> _error;

        /// <summary>
        /// Default constructor to handle MEF.
        /// </summary>
        public FunctionalLogHandler()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Logging.FunctionalLogHandler"/> class.
        /// </summary>
        public FunctionalLogHandler(Action<string> debug, Action<string> trace, Action<string> error)
        {
            // saves references to the real console text writer since in a deployed state we may overwrite this in order
            // to redirect messages from algorithm to result handler
            _debug = debug;
            _trace = trace;
            _error = error;
        }

        /// <summary>
        /// Write error message to log
        /// </summary>
        /// <param name="text">The error text to log</param>
        public void Error(string text)
        {
            if (_error != null)
            {
                _error(DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture) + " ERROR " + text);
            }
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The debug text to log</param>
        public void Debug(string text)
        {
            if (_debug != null)
            {
                _debug(DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture) + " DEBUG " + text);
            }
        }

        /// <summary>
        /// Write debug message to log
        /// </summary>
        /// <param name="text">The trace text to log</param>
        public void Trace(string text)
        {
            if (_trace != null)
            {
                _trace(DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture) + " TRACE " + text);
            }
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