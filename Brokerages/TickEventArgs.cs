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
using QuantConnect.Data.Market;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides data for an event that is triggered when a new <see cref="Tick"/> is received.
    /// </summary>
    public sealed class TickEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="Tick"/> data associated with the event.
        /// </summary>
        public Tick Tick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickEventArgs"/> class with the specified <see cref="Tick"/>.
        /// </summary>
        /// <param name="tick">The <see cref="Tick"/> data associated with the event.</param>
        public TickEventArgs(Tick tick)
        {
            Tick = tick;
        }
    }
}
