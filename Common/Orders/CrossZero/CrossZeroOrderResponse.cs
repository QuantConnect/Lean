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

namespace QuantConnect.Orders.CrossZero
{
    /// <summary>
    /// Represents a response for a cross zero order request.
    /// </summary>
    public readonly struct CrossZeroOrderResponse
    {
        /// <summary>
        /// Gets the brokerage order ID.
        /// </summary>
        public string BrokerageOrderId { get; }

        /// <summary>
        /// Gets a value indicating whether the order was placed successfully.
        /// </summary>
        public bool IsOrderPlacedSuccessfully { get; }

        /// <summary>
        /// Gets the message of the order.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossZeroOrderResponse"/> struct.
        /// </summary>
        /// <param name="brokerageOrderId">The brokerage order ID.</param>
        /// <param name="isOrderPlacedSuccessfully">if set to <c>true</c> [is order placed successfully].</param>
        /// <param name="message">The message of the order. This parameter is optional and defaults to <c>null</c>.</param>
        public CrossZeroOrderResponse(string brokerageOrderId, bool isOrderPlacedSuccessfully, string message = "")
        {
            BrokerageOrderId = brokerageOrderId;
            IsOrderPlacedSuccessfully = isOrderPlacedSuccessfully;
            Message = message;
        }
    }
}
