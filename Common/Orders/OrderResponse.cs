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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Represents a response to an <see cref="OrderRequest"/>. See <see cref="OrderRequest.Response"/> property for
    /// a specific request's response value
    /// </summary>
    public class OrderResponse
    {
        /// <summary>
        /// Gets the order id
        /// </summary>
        public int OrderId
        {
            get; private set;
        }

        /// <summary>
        /// Gets the error message if the <see cref="ErrorCode"/> does not equal <see cref="OrderResponseErrorCode.None"/>, otherwise
        /// gets <see cref="string.Empty"/>
        /// </summary>
        public string ErrorMessage
        {
            get; private set;
        }

        /// <summary>
        /// Gets the error code for this response.
        /// </summary>
        public OrderResponseErrorCode ErrorCode
        {
            get; private set;
        }

        /// <summary>
        /// Gets true if this response represents a successful request, false otherwise
        /// If this is an unprocessed response, IsSuccess will return false.
        /// </summary>
        public bool IsSuccess
        {
            get { return IsProcessed && !IsError; }
        }

        /// <summary>
        /// Gets true if this response represents an error, false otherwise
        /// </summary>
        public bool IsError
        {
            get { return IsProcessed && ErrorCode != OrderResponseErrorCode.None; }
        }

        /// <summary>
        /// Gets true if this response has been processed, false otherwise
        /// </summary>
        public bool IsProcessed
        {
            get { return this != Unprocessed; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderResponse"/> class
        /// </summary>
        /// <param name="orderId">The order id</param>
        /// <param name="errorCode">The error code of the response, specify <see cref="OrderResponseErrorCode.None"/> for no error</param>
        /// <param name="errorMessage">The error message, applies only if the <paramref name="errorCode"/> does not equal <see cref="OrderResponseErrorCode.None"/></param>
        private OrderResponse(int orderId, OrderResponseErrorCode errorCode, string errorMessage)
        {
            OrderId = orderId;
            ErrorCode = errorCode;
            if (errorCode != OrderResponseErrorCode.None)
            {
                ErrorMessage = errorMessage ?? Messages.OrderResponse.DefaultErrorMessage;
            }
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
            return Messages.OrderResponse.ToString(this);
        }

        #region Statics - implicit(int), Unprocessed constant, response factory methods

        /// <summary>
        /// Gets an <see cref="OrderResponse"/> for a request that has not yet been processed
        /// </summary>
        public static readonly OrderResponse Unprocessed = new OrderResponse(int.MinValue, OrderResponseErrorCode.None,
            Messages.OrderResponse.UnprocessedOrderResponseErrorMessage);

        /// <summary>
        /// Helper method to create a successful response from a request
        /// </summary>
        public static OrderResponse Success(OrderRequest request)
        {
            return new OrderResponse(request.OrderId, OrderResponseErrorCode.None, null);
        }

        /// <summary>
        /// Helper method to create an error response from a request
        /// </summary>
        public static OrderResponse Error(OrderRequest request, OrderResponseErrorCode errorCode, string errorMessage)
        {
            return new OrderResponse(request.OrderId, errorCode, errorMessage);
        }

        /// <summary>
        /// Helper method to create an error response due to an invalid order status
        /// </summary>
        public static OrderResponse InvalidStatus(OrderRequest request, Order order)
        {
            return Error(request, OrderResponseErrorCode.InvalidOrderStatus, Messages.OrderResponse.InvalidStatus(request, order));
        }

        /// <summary>
        /// Helper method to create an error response due to the "New" order status
        /// </summary>
        public static OrderResponse InvalidNewStatus(OrderRequest request, Order order)
        {
            return Error(request, OrderResponseErrorCode.InvalidNewOrderStatus, Messages.OrderResponse.InvalidNewStatus(request, order));
        }

        /// <summary>
        /// Helper method to create an error response due to a bad order id
        /// </summary>
        public static OrderResponse UnableToFindOrder(OrderRequest request)
        {
            return Error(request, OrderResponseErrorCode.UnableToFindOrder, Messages.OrderResponse.UnableToFindOrder(request));
        }

        /// <summary>
        /// Helper method to create an error response due to a zero order quantity
        /// </summary>
        public static OrderResponse ZeroQuantity(OrderRequest request)
        {
            return Error(request, OrderResponseErrorCode.OrderQuantityZero, Messages.OrderResponse.ZeroQuantity(request));
        }

        /// <summary>
        /// Helper method to create an error response due to a missing security
        /// </summary>
        public static OrderResponse MissingSecurity(SubmitOrderRequest request)
        {
            return Error(request, OrderResponseErrorCode.MissingSecurity, Messages.OrderResponse.MissingSecurity(request));
        }

        /// <summary>
        /// Helper method to create an error response due to algorithm still in warmup mode
        /// </summary>
        public static OrderResponse WarmingUp(OrderRequest request)
        {
            return Error(request, OrderResponseErrorCode.AlgorithmWarmingUp, Messages.OrderResponse.WarmingUp(request));
        }

        #endregion
    }
}
