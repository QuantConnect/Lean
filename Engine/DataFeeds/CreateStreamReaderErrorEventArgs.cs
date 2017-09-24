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
    /// Event arguments for the <see cref="TextSubscriptionDataSourceReader.CreateStreamReader"/> event
    /// </summary>
    public sealed class CreateStreamReaderErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the date of the source
        /// </summary>
        public DateTime Date
        {
            get; private set;
        }

        /// <summary>
        /// Gets the source that caused the error
        /// </summary>
        public SubscriptionDataSource Source
        {
            get; private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStreamReaderErrorEventArgs"/> class
        /// </summary>
        /// <param name="date">The date of the source</param>
        /// <param name="source">The source that cause the error</param>
        public CreateStreamReaderErrorEventArgs(DateTime date, SubscriptionDataSource source)
        {
            Date = date;
            Source = source;
        }
    }
}