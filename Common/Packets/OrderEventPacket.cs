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
using QuantConnect.Orders;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Order event packet for passing updates on the state of an order to the portfolio. 
    /// </summary>
    /// <remarks>As an order is updated in pieces/partial fills the order fill price is passed back to the Algorithm Portfolio method</remarks>
    public class OrderEventPacket : Packet
    {
        /// <summary>
        /// Order event object
        /// </summary>
        public OrderEvent Event;

        /// <summary>
        /// Algorithm id for this order event
        /// </summary>
        public string AlgorithmId;

        /// <summary>
        /// Default constructor for JSON
        /// </summary>
        public OrderEventPacket()
            : base (PacketType.OrderEvent)
        { }

        /// <summary>
        /// Create a new instance of the order event packet
        /// </summary>
        public OrderEventPacket(string algorithmId, OrderEvent eventOrder)
            : base(PacketType.OrderEvent)
        {
            AlgorithmId = algorithmId;
            Event = eventOrder;
        }
    
    } // End Order Event Packet:

} // End of Namespace:
