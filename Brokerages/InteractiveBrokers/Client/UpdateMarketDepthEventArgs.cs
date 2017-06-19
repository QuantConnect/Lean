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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.UpdateMarketDepth"/> event
    /// </summary>
    public sealed class UpdateMarketDepthEventArgs : EventArgs
    {
        /// <summary>
        /// The request's identifier.
        /// </summary>
        public int TickerId { get; private set; }

        /// <summary>
        /// Specifies the row Id of this market depth entry.
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// Identifies how this order should be applied to the market depth.
        /// </summary>
        public int Operation { get; private set; }

        /// <summary>
        /// Identifies the side of the book that this order belongs to.
        /// </summary>
        public int Side { get; private set; }

        /// <summary>
        /// The order price.
        /// </summary>
        public double Price { get; private set; }

        /// <summary>
        /// The order size.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateMarketDepthEventArgs"/> class
        /// </summary>
        public UpdateMarketDepthEventArgs(int tickerId, int position, int operation, int side, double price, int size)
        {
            TickerId = tickerId;
            Position = position;
            Operation = operation;
            Side = side;
            Price = price;
            Size = size;
        }
    }
}