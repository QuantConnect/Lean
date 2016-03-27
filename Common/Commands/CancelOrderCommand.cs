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

using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Represents a command to cancel a specific order by id
    /// </summary>
    public sealed class CancelOrderCommand : ICommand
    {
        /// <summary>
        /// Gets or sets the order id to be cancelled
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Runs this command against the specified algorithm instance
        /// </summary>
        /// <param name="algorithm">The algorithm to run this command against</param>
        public CommandResultPacket Run(IAlgorithm algorithm)
        {
            var ticket = algorithm.Transactions.CancelOrder(OrderId);
            return ticket.CancelRequest != null 
                ? new Result(this, true, ticket.QuantityFilled) 
                : new Result(this, false, ticket.QuantityFilled);
        }

        /// <summary>
        /// Result packet type for the <see cref="CancelOrderCommand"/> command
        /// </summary>
        public class Result : CommandResultPacket
        {
            /// <summary>
            /// Gets or sets the quantity filled on the cancelled order
            /// </summary>
            public decimal QuantityFilled { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Result"/> class
            /// </summary>
            public Result(ICommand command, bool success, decimal quantityFilled)
                : base(command, success)
            {
                QuantityFilled = quantityFilled;
            }
        }
    }
}
