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

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using QuantConnect.Orders;
using QuantConnect.Securities;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    public static partial class Messages
    {
        #region CancelOrderRequest Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string CancelOrderRequestToString(CancelOrderRequest request)
        {
            return Invariant($"{request.Time.ToStringInvariant()} UTC: Cancel Order: ({request.Tag}) - {request.OrderId} Status: {request.Status}");
        }

        #endregion

        #region GroupOrderManagerExtensions Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string InsufficientBuyingPowerForOrders(Dictionary<Order, Security> securities,
            HasSufficientBuyingPowerForOrderResult hasSufficientBuyingPowerResult)
        {
            var ids = string.Join(",", securities.Keys.Select(o => o.Id));
            var values = string.Join(",", securities.Select(o => o.Key.GetValue(o.Value).SmartRounding()));
            return $"Order Error: ids: [{ids}], Insufficient buying power to complete orders (Value:[{values}]), " +
                $"Reason: {hasSufficientBuyingPowerResult.Reason}.";
        }

        #endregion

        #region LimitIfTouchedOrder Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string LimitIfTouchedOrderTriggerPriceTag(LimitIfTouchedOrder order)
        {
            return Invariant($"Trigger Price: {order.TriggerPrice:C} Limit Price: {order.LimitPrice:C}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string LimitIfTouchedOrderToString(LimitIfTouchedOrder order)
        {
            return Invariant($"{OrderToString(order)} at trigger {order.TriggerPrice.SmartRounding()} limit {order.LimitPrice.SmartRounding()}");
        }

        #endregion

        #region LimitOrder Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string LimitOrderLimitPriceTag(LimitOrder order)
        {
            return Invariant($"Limit Price: {order.LimitPrice:C}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string LimitOrderToString(LimitOrder order)
        {
            return Invariant($"{OrderToString(order)} at limit {order.LimitPrice.SmartRounding()}");
        }

        #endregion

        #region Order Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderToString(Order order)
        {
            var tag = string.IsNullOrEmpty(order.Tag) ? string.Empty : $": {order.Tag}";
            return Invariant($"OrderId: {order.Id} (BrokerId: {string.Join(",", order.BrokerId)}) {order.Status} " +
                $"{order.Type} order for {order.Quantity} unit{(order.Quantity == 1 ? "" : "s")} of {order.Symbol}{tag}");
        }

        #endregion

        #region OrderEvent Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderEventToString(OrderEvent orderEvent)
        {
            var message = Invariant($"Time: {orderEvent.UtcTime} OrderID: {orderEvent.OrderId} EventID: {orderEvent.Id} Symbol: {orderEvent.Symbol.Value} Status: {orderEvent.Status} Quantity: {orderEvent.Quantity}");
            if (orderEvent.FillQuantity != 0)
            {
                message += Invariant($" FillQuantity: {orderEvent.FillQuantity} FillPrice: {orderEvent.FillPrice.SmartRounding()} {orderEvent.FillPriceCurrency}");
            }

            if (orderEvent.LimitPrice.HasValue)
            {
                message += Invariant($" LimitPrice: {orderEvent.LimitPrice.Value.SmartRounding()}");
            }

            if (orderEvent.StopPrice.HasValue)
            {
                message += Invariant($" StopPrice: {orderEvent.StopPrice.Value.SmartRounding()}");
            }

            if (orderEvent.TriggerPrice.HasValue)
            {
                message += Invariant($" TriggerPrice: {orderEvent.TriggerPrice.Value.SmartRounding()}");
            }

            // attach the order fee so it ends up in logs properly.
            if (orderEvent.OrderFee.Value.Amount != 0m)
            {
                message += Invariant($" OrderFee: {orderEvent.OrderFee}");
            }

            // add message from brokerage
            if (!string.IsNullOrEmpty(orderEvent.Message))
            {
                message += Invariant($" Message: {orderEvent.Message}");
            }

            if (orderEvent.Symbol.SecurityType.IsOption())
            {
                message += Invariant($" IsAssignment: {orderEvent.IsAssignment}");
            }

            return message;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderEventShortToString(OrderEvent orderEvent)
        {
            var message = Invariant($"{orderEvent.UtcTime} OID:{orderEvent.OrderId} {orderEvent.Symbol.Value} {orderEvent.Status} Q:{orderEvent.Quantity}");
            if (orderEvent.FillQuantity != 0)
            {
                message += Invariant($" FQ:{orderEvent.FillQuantity} FP:{orderEvent.FillPrice.SmartRounding()} {orderEvent.FillPriceCurrency}");
            }

            if (orderEvent.LimitPrice.HasValue)
            {
                message += Invariant($" LP:{orderEvent.LimitPrice.Value.SmartRounding()}");
            }

            if (orderEvent.StopPrice.HasValue)
            {
                message += Invariant($" SP:{orderEvent.StopPrice.Value.SmartRounding()}");
            }

            if (orderEvent.TriggerPrice.HasValue)
            {
                message += Invariant($" TP:{orderEvent.TriggerPrice.Value.SmartRounding()}");
            }

            // attach the order fee so it ends up in logs properly.
            if (orderEvent.OrderFee.Value.Amount != 0m)
            {
                message += Invariant($" OF:{orderEvent.OrderFee}");
            }

            // add message from brokerage
            if (!string.IsNullOrEmpty(orderEvent.Message))
            {
                message += Invariant($" M:{orderEvent.Message}");
            }

            if (orderEvent.Symbol.SecurityType.IsOption())
            {
                message += Invariant($" IA:{orderEvent.IsAssignment}");
            }

            return message;
        }

        #endregion

        #region OrderRequest Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderRequestToString(OrderRequest request)
        {
            return Invariant($"{request.Time} UTC: Order: ({request.OrderId.ToStringInvariant()}) - {request.Tag} Status: {request.Status}");
        }

        #endregion

        #region OrderResponse Messages

        public static string OrderResponseDefaultErrorMessage = "An unexpected error occurred.";

        public static string UnprocessedOrderResponseErrorMessage = "The request has not yet been processed.";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderResponseToString(OrderResponse response)
        {
            if (response == OrderResponse.Unprocessed)
            {
                return "Unprocessed";
            }

            if (response.IsError)
            {
                return Invariant($"Error: {response.ErrorCode} - {response.ErrorMessage}");
            }

            return "Success";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderResponseInvalidStatusErrorMessage(OrderRequest request, Order order)
        {
            return Invariant($"Unable to update order with id {request.OrderId} because it already has {order.Status} status.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderResponseInvalidNewStatusErrorMessage(OrderRequest request, Order order)
        {
            return Invariant($"Unable to update or cancel order with id {request.OrderId} and status {order.Status} " +
                $"because the submit confirmation has not been received yet.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderResponseUnableToFindOrderErrorMessage(OrderRequest request)
        {
            return Invariant($"Unable to locate order with id {request.OrderId}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderResponseZeroQuantityErrorMessage(OrderRequest request)
        {
            return Invariant($"Unable to {request.OrderRequestType.ToLower()} order with id {request.OrderId} that has zero quantity.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderResponseMissingSecurityErrorMessage(SubmitOrderRequest request)
        {
            return Invariant($"You haven't requested {request.Symbol} data. Add this with AddSecurity() in the Initialize() Method.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderResponseWarmingUpErrorMessage(OrderRequest request)
        {
            return Invariant($"This operation is not allowed in Initialize or during warm up: OrderRequest.{request.OrderRequestType}. ") +
                "Please move this code to the OnWarmupFinished() method.";
        }

        #endregion

        #region OrderTicket Messages

        public static string OrderTicketNullCancelRequest = "CancelRequest is null.";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderTicketGetFieldError(OrderTicket ticket, OrderField field)
        {
            return Invariant($"Unable to get field {field} on order of type {ticket.SubmitRequest.OrderType}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderTicketCancelRequestAlreadySubmitted(OrderTicket ticket)
        {
            return Invariant($"Order {ticket.OrderId} has already received a cancellation request.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string OrderTicketToString(OrderTicket ticket, Order order, int requestCount, int responseCount)
        {
            var counts = Invariant($"Request Count: {requestCount} Response Count: {responseCount}");
            if (order != null)
            {
                return Invariant($"{ticket.OrderId}: {order} {counts}");
            }

            return Invariant($"{ticket.OrderId}: {counts}");
        }

        #endregion

        #region StopLimitOrder Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string StopLimitOrderTag(StopLimitOrder order)
        {
            return Invariant($"Stop Price: {order.StopPrice:C} Limit Price: {order.LimitPrice:C}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string StopLimitOrderToString(StopLimitOrder order)
        {
            return Invariant($"{OrderToString(order)} at stop {order.StopPrice.SmartRounding()} limit {order.LimitPrice.SmartRounding()}");
        }

        #endregion

        #region StopMarketOrder Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string StopMarketOrderTag(StopMarketOrder order)
        {
            return Invariant($"Stop Price: {order.StopPrice:C}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string StopMarketOrderToString(StopMarketOrder order)
        {
            return Invariant($"{OrderToString(order)} at stop {order.StopPrice.SmartRounding()}");
        }

        #endregion

        #region SubmitOrderRequest Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SubmitOrderRequestToString(SubmitOrderRequest request)
        {
            // create a proxy order object to steal its ToString method
            var proxy = Order.CreateOrder(request);
            return Invariant($"{request.Time} UTC: Submit Order: ({request.OrderId}) - {proxy} {request.Tag} Status: {request.Status}");
        }

        #endregion

        #region UpdateOrderRequest Messages

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string UpdateOrderRequestToString(UpdateOrderRequest request)
        {
            var updates = new List<string>(4);
            if (request.Quantity.HasValue)
            {
                updates.Add(Invariant($"Quantity: {request.Quantity.Value}"));
            }
            if (request.LimitPrice.HasValue)
            {
                updates.Add(Invariant($"LimitPrice: {request.LimitPrice.Value.SmartRounding()}"));
            }
            if (request.StopPrice.HasValue)
            {
                updates.Add(Invariant($"StopPrice: {request.StopPrice.Value.SmartRounding()}"));
            }
            if (request.TriggerPrice.HasValue)
            {
                updates.Add(Invariant($"TriggerPrice: {request.TriggerPrice.Value.SmartRounding()}"));
            }

            return Invariant($"{request.Time} UTC: Update Order: ({request.OrderId}) - {string.Join(", ", updates)} {request.Tag} Status: {request.Status}");
        }

        #endregion
    }
}
