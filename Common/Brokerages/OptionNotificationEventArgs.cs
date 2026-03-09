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
using QuantConnect.Interfaces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Event arguments class for the <see cref="IBrokerage.OptionNotification"/> event
    /// </summary>
    public sealed class OptionNotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the option symbol which has received a notification
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the new option position (positive for long, zero for flat, negative for short)
        /// </summary>
        public decimal Position { get; }

        /// <summary>
        /// The tag that will be used in the order
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionNotificationEventArgs"/> class
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="position">The new option position</param>
        public OptionNotificationEventArgs(Symbol symbol, decimal position)
        {
            Symbol = symbol;
            Position = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionNotificationEventArgs"/> class
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="position">The new option position</param>
        /// <param name="tag">The tag to be used for the order</param>
        public OptionNotificationEventArgs(Symbol symbol, decimal position, string tag)
            : this(symbol, position)
        {
            Tag = tag;
        }

        /// <summary>
        /// Returns the string representation of this event
        /// </summary>
        public override string ToString()
        {
            var str = $"{Symbol} position: {Position}";
            if (!string.IsNullOrEmpty(Tag))
            {
                str += $", tag: {Tag}";
            }

            return str;
        }
    }
}
