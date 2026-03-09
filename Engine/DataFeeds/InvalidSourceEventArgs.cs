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
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Event arguments for the <see cref="ISubscriptionDataSourceReader.InvalidSource"/> event
    /// </summary>
    public sealed class InvalidSourceEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the source that was considered invalid
        /// </summary>
        public SubscriptionDataSource Source
        {
            get; private set;
        }

        /// <summary>
        /// Gets the exception that was encountered
        /// </summary>
        public Exception Exception
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSourceEventArgs"/> class
        /// </summary>
        /// <param name="source">The source that was considered invalid</param>
        /// <param name="exception">The exception that was encountered</param>
        public InvalidSourceEventArgs(SubscriptionDataSource source, Exception exception)
        {
            Source = source;
            Exception = exception;
        }
    }
}