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

namespace QuantConnect.Data
{

    /// <summary>
    /// Represents a subscription channel
    /// </summary>
    public class Channel
    {

        /// <summary>
        /// Represents an internal channel name for all brokerage channels in case we don't differentiate them
        /// </summary>
        public static string Single = "common";

        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The ticker symbol of the channel
        /// </summary>
        public Symbol Symbol { get; private set; }

        /// <summary>
        /// Creates an instance of subscription channel
        /// </summary>
        /// <param name="channelName">Socket channel name</param>
        /// <param name="symbol">Associated symbol</param>
        public Channel(string channelName, Symbol symbol)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException(nameof(channelName), "Channel Name can't be null or empty");
            }

            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol), "Symbol can't be null or empty");
            }

            Name = channelName;
            Symbol = symbol;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Channel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other?.Name) && Symbol.Equals(other.Symbol);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Channel);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ Name.GetHashCode();
                hash = (hash * 16777619) ^ Symbol.GetHashCode();
                return hash;
            }
        }
    }
}
