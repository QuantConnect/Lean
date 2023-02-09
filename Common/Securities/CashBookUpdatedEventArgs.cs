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
 *
*/

using System;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Event fired when the cash book is updated
    /// </summary>
    public class CashBookUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// The update type
        /// </summary>
        public CashBookUpdateType UpdateType { get; }

        /// <summary>
        /// The updated cash instance.
        /// </summary>
        /// <remarks>This will be null for <see cref="CashBookUpdateType.Removed"/> events that clear the whole cash book</remarks>
        public Cash Cash { get; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public CashBookUpdatedEventArgs(CashBookUpdateType type, Cash cash)
        {
            UpdateType = type;
            Cash = cash;
        }
    }
}
