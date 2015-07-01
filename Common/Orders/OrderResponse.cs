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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Base class for order requests.
    /// </summary>
    public class OrderResponse
    {
        /// <summary>
        /// Request id.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// Id of the order to process
        /// </summary>
        public int OrderId;

        /// <summary>
        /// Response type
        /// </summary>
        public OrderResponseType Type;

        /// <summary>
        /// Response error code
        /// </summary>
        public OrderResponseErrorCode ErrorCode;

        /// <summary>
        /// Response error message
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// New OrderResponse Constructor
        /// </summary>
        public OrderResponse() { }

        /// <summary>
        /// New OrderResponse constructor
        /// </summary>
        /// <param name="request">Order request to process</param>
        public OrderResponse(OrderRequest request)
        {
            this.Id = request.Id;
            this.OrderId = request.OrderId;
        }

        /// <summary>
        /// Shortcut method for error check
        /// </summary>
        /// <returns></returns>
        public bool IsError()
        {
            return Type == OrderResponseType.Error;
        }

        /// <summary>
        /// Shortcut method for success check
        /// </summary>
        /// <returns></returns>
        public bool IsProcessed()
        {
            return Type == OrderResponseType.Processed;
        }

        /// <summary>
        /// Set response to error state
        /// </summary>
        /// <param name="errorCode">Error code</param>
        /// <param name="errorMessage">Error message</param>
        public void Error(OrderResponseErrorCode errorCode, string errorMessage = "")
        {
            this.Type = OrderResponseType.Error;
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Set response to processed state
        /// </summary>
        public void Processed()
        {
            this.Type = OrderResponseType.Processed;
            this.ErrorCode = OrderResponseErrorCode.None;
            this.ErrorMessage = "";
        }

        /// <summary>
        /// Order response description.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[{0}] Response for order [{1}] Type: {2} ErrorCode: {3} ErrorMessage: {4}", Id, OrderId, Type, ErrorCode, ErrorMessage);
        }
    }
}
