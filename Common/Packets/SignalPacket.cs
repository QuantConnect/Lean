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

/*
* QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
* Lean Algorithmic Trading Engine v2.2 Copyright 2015 QuantConnect Corporation.
*/

using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Signals;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Provides a packet type for transmitting signal data
    /// </summary>
    public class SignalPacket : Packet
    {
        /// <summary>
        /// The algorithm's unique identifier
        /// </summary>
        public string AlgorithmId { get; set; }

        /// <summary>
        /// The utc date time the signals were generated
        /// </summary>
        public DateTime DateTimeUtc { get; set; }

        /// <summary>
        /// The generated signals
        /// </summary>
        public List<ISignal> Signals { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalPacket"/> class
        /// </summary>
        public SignalPacket()
            : base(PacketType.Signal)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalPacket"/> class
        /// </summary>
        /// <param name="algorithmId">The algorithm's unique identifier</param>
        /// <param name="collection">Signal collection emitted from the algorithm</param>
        public SignalPacket(string algorithmId, SignalCollection collection)
            : base(PacketType.Signal)
        {
            AlgorithmId = algorithmId;
            Signals = collection.Signals;
            DateTimeUtc = collection.DateTimeUtc;
        }
    }
}
