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

using System.Collections.Generic;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Packet to communicate updates to the algorithm tags
    /// </summary>
    public class AlgorithmTagsUpdatePacket : Packet
    {
        /// <summary>
        /// Algorithm id for this order event
        /// </summary>
        public string AlgorithmId;

        /// <summary>
        /// The new tags
        /// </summary>
        public HashSet<string> Tags = new();

        /// <summary>
        /// Default constructor for JSON
        /// </summary>
        public AlgorithmTagsUpdatePacket()
            : base(PacketType.AlgorithmTagsUpdate)
        { }

        /// <summary>
        /// Create a new instance of the algorithm tags up[date packet
        /// </summary>
        public AlgorithmTagsUpdatePacket(string algorithmId, HashSet<string> tags)
            : base(PacketType.AlgorithmTagsUpdate)
        {
            AlgorithmId = algorithmId;
            Tags = tags;
        }

    }
}
