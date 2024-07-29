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
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Orders"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.CancelOrderRequest"/> class and its consumers or related classes
        /// </summary>
        public static class CancelOrderRequest
        {
            /// <summary>
            /// Parses the given CancelOrderRequest into a string message containing basic information about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.CancelOrderRequest request)
            {
                return Invariant($@"{request.Time.ToStringInvariant()} UTC: Cancel Order: ({request.OrderId}) - {
                    request.Tag} Status: {request.Status}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.GroupOrderExtensions"/> class and its consumers or related classes
        /// </summary>
        public static class GroupOrderExtensions
        {
            /// <summary>
            /// Returns a string message saying there is insufficient buying power to complete the given orders
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InsufficientBuyingPowerForOrders(Dictionary<Orders.Order, Securities.Security> securities,
                HasSufficientBuyingPowerForOrderResult hasSufficientBuyingPowerResult)
            {
                var ids = string.Join(",", securities.Keys.Select(o => o.Id));
                var values = string.Join(",", securities.Select(o => o.Key.GetValue(o.Value).SmartRounding()));
                return $@"Order Error: ids: [{ids}], Insufficient buying power to complete orders (Value:[{values}]), Reason: {
                    hasSufficientBuyingPowerResult.Reason}.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.LimitIfTouchedOrder"/> class and its consumers or related classes
        /// </summary>
        public static class LimitIfTouchedOrder
        {
            /// <summary>
            /// Returns an empty string tag
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string Tag(Orders.LimitIfTouchedOrder order)
            {
                // No additional information to display
                return string.Empty;
            }

            /// <summary>
            /// Parses the given LimitIfTouched order to a string message containing basic information
            /// about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.LimitIfTouchedOrder order)
            {
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency);
                return Invariant($@"{Order.ToString(order)} at trigger {currencySymbol}{order.TriggerPrice.SmartRounding()
                    } limit {currencySymbol}{order.LimitPrice.SmartRounding()}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.LimitOrder"/> class and its consumers or related classes
        /// </summary>
        public static class LimitOrder
        {
            /// <summary>
            /// Returns an empty string tag
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string Tag(Orders.LimitOrder order)
            {
                // No additional information to display
                return string.Empty;
            }

            /// <summary>
            /// Parses a Limit order to a string message with basic information about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.LimitOrder order)
            {
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency);
                return Invariant($"{Order.ToString(order)} at limit {currencySymbol}{order.LimitPrice.SmartRounding()}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.Order"/> class and its consumers or related classes
        /// </summary>
        public static class Order
        {
            /// <summary>
            /// Parses the given order into a string message with basic information about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.Order order)
            {
                var tag = string.IsNullOrEmpty(order.Tag) ? string.Empty : $": {order.Tag}";
                return Invariant($@"OrderId: {order.Id} (BrokerId: {string.Join(",", order.BrokerId)}) {order.Status} {
                    order.Type} order for {order.Quantity} unit{(order.Quantity == 1 ? "" : "s")} of {order.Symbol}{tag}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.OrderEvent"/> class and its consumers or related classes
        /// </summary>
        public static class OrderEvent
        {
            /// <summary>
            /// Parses the given order event into a string message containing basic information about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.OrderEvent orderEvent)
            {
                var message = Invariant($@"Time: {orderEvent.UtcTime} OrderID: {orderEvent.OrderId} EventID: {
                    orderEvent.Id} Symbol: {orderEvent.Symbol.Value} Status: {orderEvent.Status} Quantity: {orderEvent.Quantity}");
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(orderEvent.FillPriceCurrency);

                if (orderEvent.FillQuantity != 0)
                {
                    message += Invariant($@" FillQuantity: {orderEvent.FillQuantity
                        } FillPrice: {currencySymbol}{orderEvent.FillPrice.SmartRounding()}");
                }

                if (orderEvent.LimitPrice.HasValue)
                {
                    message += Invariant($" LimitPrice: {currencySymbol}{orderEvent.LimitPrice.Value.SmartRounding()}");
                }

                if (orderEvent.StopPrice.HasValue)
                {
                    message += Invariant($" StopPrice: {currencySymbol}{orderEvent.StopPrice.Value.SmartRounding()}");
                }

                if (orderEvent.TrailingAmount.HasValue)
                {
                    var trailingAmountString = TrailingStopOrder.TrailingAmount(orderEvent.TrailingAmount.Value,
                        orderEvent.TrailingAsPercentage ?? false, currencySymbol);
                    message += $" TrailingAmount: {trailingAmountString}";
                }

                if (orderEvent.LimitOffset.HasValue)
                {
                    var limitOffsetString = TrailingStopLimitOrder.LimitOffset(orderEvent.LimitOffset.Value, currencySymbol);
                    message += $" LimitOffset: {limitOffsetString}";
                }

                if (orderEvent.TriggerPrice.HasValue)
                {
                    message += Invariant($" TriggerPrice: {currencySymbol}{orderEvent.TriggerPrice.Value.SmartRounding()}");
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

            /// <summary>
            /// Parses the given order event into a string message which summarizes the basic information about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ShortToString(Orders.OrderEvent orderEvent)
            {
                var message = Invariant($"{orderEvent.UtcTime} OID:{orderEvent.OrderId} {orderEvent.Symbol.Value} {orderEvent.Status} Q:{orderEvent.Quantity}");
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(orderEvent.FillPriceCurrency);

                if (orderEvent.FillQuantity != 0)
                {
                    message += Invariant($" FQ:{orderEvent.FillQuantity} FP:{currencySymbol}{orderEvent.FillPrice.SmartRounding()}");
                }

                if (orderEvent.LimitPrice.HasValue)
                {
                    message += Invariant($" LP:{currencySymbol}{orderEvent.LimitPrice.Value.SmartRounding()}");
                }

                if (orderEvent.StopPrice.HasValue)
                {
                    message += Invariant($" SP:{currencySymbol}{orderEvent.StopPrice.Value.SmartRounding()}");
                }

                if (orderEvent.TrailingAmount.HasValue)
                {
                    var trailingAmountString = TrailingStopOrder.TrailingAmount(orderEvent.TrailingAmount.Value,
                        orderEvent.TrailingAsPercentage ?? false, currencySymbol);
                    message += $" TA: {trailingAmountString}";
                }

                if (orderEvent.LimitOffset.HasValue)
                {
                    var limitOffsetString = TrailingStopLimitOrder.LimitOffset(orderEvent.LimitOffset.Value, currencySymbol);
                    message += $" LimitOffset: {limitOffsetString}";
                }

                if (orderEvent.TriggerPrice.HasValue)
                {
                    message += Invariant($" TP:{currencySymbol}{orderEvent.TriggerPrice.Value.SmartRounding()}");
                }

                // attach the order fee so it ends up in logs properly.
                if (orderEvent.OrderFee.Value.Amount != 0m)
                {
                    message += Invariant($" OF:{currencySymbol}{orderEvent.OrderFee}");
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
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.OrderRequest"/> class and its consumers or related classes
        /// </summary>
        public static class OrderRequest
        {
            /// <summary>
            /// Parses the given order request into a string message containing basic information about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.OrderRequest request)
            {
                return Invariant($"{request.Time} UTC: Order: ({request.OrderId}) - {request.Tag} Status: {request.Status}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.OrderResponse"/> class and its consumers or related classes
        /// </summary>
        public static class OrderResponse
        {
            /// <summary>
            /// String message saying: An unexpected error occurred
            /// </summary>
            public static string DefaultErrorMessage = "An unexpected error occurred.";

            /// <summary>
            /// String message saying: The request has not yet been processed
            /// </summary>
            public static string UnprocessedOrderResponseErrorMessage = "The request has not yet been processed.";

            /// <summary>
            /// Parses the given order response into a string message containing basic information about it 
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.OrderResponse response)
            {
                if (response == Orders.OrderResponse.Unprocessed)
                {
                    return "Unprocessed";
                }

                if (response.IsError)
                {
                    return Invariant($"Error: {response.ErrorCode} - {response.ErrorMessage}");
                }

                return "Success";
            }

            /// <summary>
            /// Returns a string message saying it was impossible to udpate the order with the id
            /// from the given request because it already had the status of the given order
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidStatus(Orders.OrderRequest request, Orders.Order order)
            {
                return Invariant($"Unable to update order with id {request.OrderId} because it already has {order.Status} status.");
            }

            /// <summary>
            /// Returns a string message saying it was impossible to update or cancel the order with the
            /// id from the given request because the submit confirmation had not been received yet
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidNewStatus(Orders.OrderRequest request, Orders.Order order)
            {
                return Invariant($@"Unable to update or cancel order with id {
                    request.OrderId} and status {order.Status} because the submit confirmation has not been received yet.");
            }

            /// <summary>
            /// Returns a string message saying it was impossible to locate the order with the id from the
            /// given request
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToFindOrder(Orders.OrderRequest request)
            {
                return Invariant($"Unable to locate order with id {request.OrderId}.");
            }

            /// <summary>
            /// Returns a string message saying it was impossible to process the given order request
            /// that has zero quantity
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ZeroQuantity(Orders.OrderRequest request)
            {
                return Invariant($"Unable to {request.OrderRequestType.ToLower()} order with id {request.OrderId} that has zero quantity.");
            }

            /// <summary>
            /// Returns a string message saying the user has not requested data for the symbol of the given
            /// request. It also advises the user on how to add this data
            /// </summary>
            /// <param name="request"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string MissingSecurity(Orders.SubmitOrderRequest request)
            {
                return Invariant($"You haven't requested {request.Symbol} data. Add this with AddSecurity() in the Initialize() Method.");
            }

            /// <summary>
            /// Returns a string message saying the given order request operation is not allowed
            /// in Initialize or during warm up. It also advises the user on where it is allowed
            /// to make it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string WarmingUp(Orders.OrderRequest request)
            {
                return Invariant($@"This operation is not allowed in Initialize or during warm up: OrderRequest.{
                    request.OrderRequestType}. Please move this code to the OnWarmupFinished() method.");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.OrderTicket"/> class and its consumers or related classes
        /// </summary>
        public static class OrderTicket
        {
            /// <summary>
            /// Returns a string message saying it was impossible to get the given field on the order type from the given
            /// ticket
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string GetFieldError(Orders.OrderTicket ticket, OrderField field)
            {
                return Invariant($"Unable to get field {field} on order of type {ticket.SubmitRequest.OrderType}");
            }

            /// <summary>
            /// Returns a string message saying the order associated with the given ticket has already received a
            /// cancellation request
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string CancelRequestAlreadySubmitted(Orders.OrderTicket ticket)
            {
                return Invariant($"Order {ticket.OrderId} has already received a cancellation request.");
            }

            /// <summary>
            /// Parses the given order ticket into a string message containing basic information about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.OrderTicket ticket, Orders.Order order, int requestCount, int responseCount)
            {
                var counts = Invariant($"Request Count: {requestCount} Response Count: {responseCount}");
                if (order != null)
                {
                    return Invariant($"{ticket.OrderId}: {order} {counts}");
                }

                return Invariant($"{ticket.OrderId}: {counts}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.StopLimitOrder"/> class and its consumers or related classes
        /// </summary>
        public static class StopLimitOrder
        {
            /// <summary>
            /// Returns an empty string as a tag
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string Tag(Orders.StopLimitOrder order)
            {
                // No additional information to display
                return string.Empty;
            }

            /// <summary>
            /// Parses the given StopLimitOrder object into a string message
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.StopLimitOrder order)
            {
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency);
                return Invariant($@"{Order.ToString(order)} at stop {currencySymbol}{order.StopPrice.SmartRounding()
                    } limit {currencySymbol}{order.LimitPrice.SmartRounding()}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.StopMarketOrder"/> class and its consumers or related classes
        /// </summary>
        public static class StopMarketOrder
        {
            /// <summary>
            /// Returns an empty string as a tag
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string Tag(Orders.StopMarketOrder order)
            {
                // No additional information to display
                return string.Empty;
            }

            /// <summary>
            /// Parses a given StopMarketOrder object into a string message
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.StopMarketOrder order)
            {
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency);
                return Invariant($"{Order.ToString(order)} at stop {currencySymbol}{order.StopPrice.SmartRounding()}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.TrailingStopOrder"/> class and its consumers or related classes
        /// </summary>
        public static class TrailingStopOrder
        {
            /// <summary>
            /// Returns a tag message for the given TrailingStopOrder
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string Tag(Orders.TrailingStopOrder order)
            {
                return Invariant($"Trailing Amount: {TrailingAmount(order)}");
            }

            /// <summary>
            /// Parses a TrailingStopOrder into a string
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.TrailingStopOrder order)
            {
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency);
                return Invariant($@"{Order.ToString(order)} at stop {currencySymbol}{order.StopPrice.SmartRounding()}. Trailing amount: {
                    TrailingAmountImpl(order, currencySymbol)}");
            }

            /// <summary>
            /// Returns a TrailingAmount string representation for the given TrailingStopOrder
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TrailingAmount(Orders.TrailingStopOrder order)
            {
                return TrailingAmountImpl(order, QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency));
            }

            /// <summary>
            /// Returns a message for the given TrailingAmount and PriceCurrency values taking into account if the trailing is as percentage
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TrailingAmount(decimal trailingAmount, bool trailingAsPercentage, string priceCurrency)
            {
                return trailingAsPercentage ? Invariant($"{trailingAmount * 100}%") : Invariant($"{priceCurrency}{trailingAmount}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string TrailingAmountImpl(Orders.TrailingStopOrder order, string currencySymbol)
            {
                return TrailingAmount(order.TrailingAmount, order.TrailingAsPercentage, currencySymbol);
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.TrailingStopLimitOrder"/> class and its consumers or related classes
        /// </summary>
        public static class TrailingStopLimitOrder
        {
            /// <summary>
            /// Returns a tag message for the given TrailingStopLimitOrder
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string Tag(Orders.TrailingStopLimitOrder order)
            {
                return Invariant($"Trailing Amount: {TrailingAmount(order)}. Limit Offset: {LimitOffset(order)}.");
            }

            /// <summary>
            /// Parses a TrailingStopLimitOrder into a string
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.TrailingStopLimitOrder order)
            {
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency);
                return Invariant($@"{Order.ToString(order)} at stop {currencySymbol}{order.StopPrice.SmartRounding()} and limit {
                    currencySymbol}{order.LimitPrice.SmartRounding()}. Trailing amount: {
                    TrailingAmount(order, currencySymbol)}. Limit offset: {LimitOffset(order, currencySymbol)}.");
            }

            /// <summary>
            /// Returns a TrailingAmount string representation for the given TrailingStopLimitOrder
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TrailingAmount(Orders.TrailingStopLimitOrder order)
            {
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency);
                return TrailingAmount(order.TrailingAmount, order.TrailingAsPercentage, currencySymbol);
            }

            /// <summary>
            /// Returns a TrailingAmount string representation for the given TrailingStopLimitOrder
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TrailingAmount(Orders.TrailingStopLimitOrder order, string priceCurrency)
            {
                return TrailingAmount(order.TrailingAmount, order.TrailingAsPercentage, priceCurrency);
            }

            /// <summary>
            /// Returns a message for the given TrailingAmount and PriceCurrency values taking into account if the trailing is as percentage
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string TrailingAmount(decimal trailingAmount, bool trailingAsPercentage, string priceCurrency)
            {
                return trailingAsPercentage ? Invariant($"{trailingAmount * 100}%") : Invariant($"{priceCurrency}{trailingAmount}");
            }

            /// <summary>
            /// Returns a LimitOffset string representation for the given TrailingStopLimitOrder
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string LimitOffset(Orders.TrailingStopLimitOrder order)
            {
                var currencySymbol = QuantConnect.Currencies.GetCurrencySymbol(order.PriceCurrency);
                return LimitOffset(order, currencySymbol);
            }

            /// <summary>
            /// Returns a LimitOffset string representation for the given TrailingStopLimitOrder
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string LimitOffset(Orders.TrailingStopLimitOrder order, string priceCurrency)
            {
                return LimitOffset(order.LimitOffset, priceCurrency);
            }

            /// <summary>
            /// Returns a LimitOffset string representation for the given TrailingStopLimitOrder
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string LimitOffset(decimal limitOffset, string priceCurrency)
            {
                return Invariant($"{priceCurrency}{limitOffset}");
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.SubmitOrderRequest"/> class and its consumers or related classes
        /// </summary>
        public static class SubmitOrderRequest
        {
            /// <summary>
            /// Parses a given SubmitOrderRequest object to a string message
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.SubmitOrderRequest request)
            {
                // create a proxy order object to steal its ToString method
                var proxy = Orders.Order.CreateOrder(request);
                return Invariant($"{request.Time} UTC: Submit Order: ({request.OrderId}) - {proxy} {request.Tag} Status: {request.Status}");
            }
        }
        
        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.UpdateOrderRequest"/> class and its consumers or related classes
        /// </summary>
        public static class UpdateOrderRequest
        {
            /// <summary>
            /// Parses an UpdateOrderRequest to a string
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Orders.UpdateOrderRequest request)
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
                if (request.TrailingAmount.HasValue)
                {
                    updates.Add(Invariant($"TrailingAmount: {request.TrailingAmount.Value.SmartRounding()}"));
                }
                if (request.TriggerPrice.HasValue)
                {
                    updates.Add(Invariant($"TriggerPrice: {request.TriggerPrice.Value.SmartRounding()}"));
                }
                if (request.LimitOffset.HasValue)
                {
                    updates.Add(Invariant($"LimitOffset: {request.LimitOffset.Value.SmartRounding()}"));
                }

                return Invariant($@"{request.Time} UTC: Update Order: ({request.OrderId}) - {string.Join(", ", updates)} {
                    request.Tag} Status: {request.Status}");
            }
        }
    }
}
