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

namespace QuantConnect.Packets
{
    /// <summary>
    /// Represents a research node packet
    /// </summary>
    public class ResearchNodePacket : AlgorithmNodePacket
    {
        /// <summary>
        /// The research id
        /// </summary>
        public string ResearchId { get; set; }

        /// <summary>
        /// Associated research token
        /// </summary>
        public string ResearchToken { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public ResearchNodePacket() : base(PacketType.ResearchNode)
        {
        }
    }
}
