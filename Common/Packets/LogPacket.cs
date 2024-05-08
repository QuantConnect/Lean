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

using Newtonsoft.Json;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Simple log message instruction from the lean engine.
    /// </summary>
    public class LogPacket : Packet
    {
        /// <summary>
        /// Log message to the users console:
        /// </summary>
        public string Message;

        /// <summary>
        /// Algorithm Id requesting this logging
        /// </summary>
        public string AlgorithmId;

        /// <summary>
        /// Default constructor for JSON
        /// </summary>
        public LogPacket()
            : base (PacketType.Log)
        { }

        /// <summary>
        /// Create a new instance of the notify Log packet:
        /// </summary>
        public LogPacket(string algorithmId, string message)
            : base(PacketType.Log)
        {
            Message = message;
            AlgorithmId = algorithmId;
        }
    
    } // End Work Packet:

} // End of Namespace:
