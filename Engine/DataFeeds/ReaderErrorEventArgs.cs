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

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Event arguments for the <see cref="TextSubscriptionDataSourceReader.ReaderError"/> event.
    /// </summary>
    public sealed class ReaderErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the line that caused the error
        /// </summary>
        public string Line
        {
            get; private set;
        }

        /// <summary>
        /// Gets the exception that was caught
        /// </summary>
        public Exception Exception
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderErrorEventArgs"/> class
        /// </summary>
        /// <param name="line">The line that caused the error</param>
        /// <param name="exception">The exception that was caught during the read</param>
        public ReaderErrorEventArgs(string line, Exception exception)
        {
            Line = line;
            Exception = exception;
        }
    }
}