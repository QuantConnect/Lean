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
using QuantConnect.Orders;
using QuantConnect.Packets;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Represents a command to submit an order to the algorithm
    /// </summary>
    public sealed class OrderCommand : ICommand
    {
        /// <summary>
        /// Gets or sets the security type for the symbol
        /// </summary>
        public SecurityType SecurityType { get; set; }

        /// <summary>
        /// Gets or sets the symbol to be ordered
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Gets or sets the order type to be submted
        /// </summary>
        public OrderType OrderType { get; set; }

        /// <summary>
        /// Gets or sets the number of units to be ordered (directional)
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the limit price. Only applies to <see cref="QuantConnect.Orders.OrderType.Limit"/> and <see cref="QuantConnect.Orders.OrderType.StopLimit"/>
        /// </summary>
        public decimal LimitPrice { get; set; }

        /// <summary>
        /// Gets or sets the stop price. Only applies to <see cref="QuantConnect.Orders.OrderType.StopLimit"/> and <see cref="QuantConnect.Orders.OrderType.StopMarket"/>
        /// </summary>
        public decimal StopPrice { get; set; }

        /// <summary>
        /// Gets or sets an arbitrary tag to be attached to the order
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Runs this command against the specified algorithm instance
        /// </summary>
        /// <param name="algorithm">The algorithm to run this command against</param>
        public CommandResultPacket Run(IAlgorithm algorithm)
        {
            var request = new SubmitOrderRequest(OrderType, SecurityType, Symbol, Quantity, StopPrice, LimitPrice, DateTime.UtcNow, Tag);
            var ticket = algorithm.Transactions.ProcessRequest(request);
            var response = ticket.GetMostRecentOrderResponse();
            var message = string.Format("{0} for {1} units of {2}: {3}", OrderType, Quantity, Symbol, response);
            
            if (response.IsSuccess)
            {
                algorithm.Debug(message);
            }
            else
            {
                algorithm.Error(message);
            }

            return new CommandResultPacket(this, response.IsSuccess);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            // delegate to the order request
            return new SubmitOrderRequest(OrderType, SecurityType, Symbol, Quantity, StopPrice, LimitPrice, DateTime.UtcNow, Tag).ToString();
        }
    }
}