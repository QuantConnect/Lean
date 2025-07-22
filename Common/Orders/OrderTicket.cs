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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides a single reference to an order for the algorithm to maintain. As the order gets
    /// updated this ticket will also get updated
    /// </summary>
    public sealed class OrderTicket
    {
        private readonly object _lock = new object();

        private Order _order;
        private OrderStatus? _orderStatusOverride;
        private CancelOrderRequest _cancelRequest;

        private FillState _fillState;
        private List<OrderEvent> _orderEventsImpl;
        private List<UpdateOrderRequest> _updateRequestsImpl;
        private readonly SubmitOrderRequest _submitRequest;
        private readonly ManualResetEvent _orderStatusClosedEvent;
        private readonly ManualResetEvent _orderSetEvent;

        // we pull this in to provide some behavior/simplicity to the ticket API
        private readonly SecurityTransactionManager _transactionManager;

        private List<OrderEvent> _orderEvents { get => _orderEventsImpl ??= new List<OrderEvent>(); }
        private List<UpdateOrderRequest> _updateRequests { get => _updateRequestsImpl ??= new List<UpdateOrderRequest>(); }

        /// <summary>
        /// Gets the order id of this ticket
        /// </summary>
        public int OrderId
        {
            get { return _submitRequest.OrderId; }
        }

        /// <summary>
        /// Gets the current status of this order ticket
        /// </summary>
        public OrderStatus Status
        {
            get
            {
                if (_orderStatusOverride.HasValue) return _orderStatusOverride.Value;
                return _order == null ? OrderStatus.New : _order.Status;
            }
        }

        /// <summary>
        /// Gets the symbol being ordered
        /// </summary>
        public Symbol Symbol
        {
            get { return _submitRequest.Symbol; }
        }

        /// <summary>
        /// Gets the <see cref="Symbol"/>'s <see cref="SecurityType"/>
        /// </summary>
        public SecurityType SecurityType
        {
            get { return _submitRequest.SecurityType; }
        }

        /// <summary>
        /// Gets the number of units ordered
        /// </summary>
        public decimal Quantity
        {
            get { return _order == null ? _submitRequest.Quantity : _order.Quantity; }
        }

        /// <summary>
        /// Gets the average fill price for this ticket. If no fills have been processed
        /// then this will return a value of zero.
        /// </summary>
        public decimal AverageFillPrice
        {
            get
            {
                return _fillState.AverageFillPrice;
            }
        }

        /// <summary>
        /// Gets the total qantity filled for this ticket. If no fills have been processed
        /// then this will return a value of zero.
        /// </summary>
        public decimal QuantityFilled
        {
            get
            {
                return _fillState.QuantityFilled;
            }
        }

        /// <summary>
        /// Gets the remaining quantity for this order ticket.
        /// This is the difference between the total quantity ordered and the total quantity filled.
        /// </summary>
        public decimal QuantityRemaining
        {
            get
            {
                var currentState = _fillState;
                return Quantity - currentState.QuantityFilled;
            }
        }

        /// <summary>
        /// Gets the time this order was last updated
        /// </summary>
        public DateTime Time
        {
            get { return GetMostRecentOrderRequest().Time; }
        }

        /// <summary>
        /// Gets the type of order
        /// </summary>
        public OrderType OrderType
        {
            get { return _submitRequest.OrderType; }
        }

        /// <summary>
        /// Gets the order's current tag
        /// </summary>
        public string Tag
        {
            get { return _order == null ? _submitRequest.Tag : _order.Tag; }
        }

        /// <summary>
        /// Gets the <see cref="SubmitOrderRequest"/> that initiated this order
        /// </summary>
        public SubmitOrderRequest SubmitRequest
        {
            get { return _submitRequest; }
        }

        /// <summary>
        /// Gets a list of <see cref="UpdateOrderRequest"/> containing an item for each
        /// <see cref="UpdateOrderRequest"/> that was sent for this order id
        /// </summary>
        public IReadOnlyList<UpdateOrderRequest> UpdateRequests
        {
            get
            {
                lock (_lock)
                {
                    // Avoid creating the update requests list if not necessary
                    if (_updateRequestsImpl == null)
                    {
                        return Array.Empty<UpdateOrderRequest>();
                    }
                    return _updateRequestsImpl.ToList();
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="CancelOrderRequest"/> if this order was canceled. If this order
        /// was not canceled, this will return null
        /// </summary>
        public CancelOrderRequest CancelRequest
        {
            get
            {
                lock (_lock)
                {
                    return _cancelRequest;
                }
            }
        }

        /// <summary>
        /// Gets a list of all order events for this ticket
        /// </summary>
        public IReadOnlyList<OrderEvent> OrderEvents
        {
            get
            {
                lock (_lock)
                {
                    return _orderEvents.ToList();
                }
            }
        }

        /// <summary>
        /// Gets a wait handle that can be used to wait until this order has filled
        /// </summary>
        public WaitHandle OrderClosed
        {
            get { return _orderStatusClosedEvent; }
        }

        /// <summary>
        /// Returns true if the order has been set for this ticket
        /// </summary>
        public bool HasOrder => _order != null;

        /// <summary>
        /// Gets a wait handle that can be used to wait until the order has been set
        /// </summary>
        public WaitHandle OrderSet => _orderSetEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderTicket"/> class
        /// </summary>
        /// <param name="transactionManager">The transaction manager used for submitting updates and cancels for this ticket</param>
        /// <param name="submitRequest">The order request that initiated this order ticket</param>
        public OrderTicket(SecurityTransactionManager transactionManager, SubmitOrderRequest submitRequest)
        {
            _submitRequest = submitRequest;
            _transactionManager = transactionManager;

            _orderStatusClosedEvent = new ManualResetEvent(false);
            _orderSetEvent = new ManualResetEvent(false);
            _fillState = new FillState(0m, 0m);
        }

        /// <summary>
        /// Gets the specified field from the ticket
        /// </summary>
        /// <param name="field">The order field to get</param>
        /// <returns>The value of the field</returns>
        /// <exception cref="ArgumentException">Field out of range</exception>
        /// <exception cref="ArgumentOutOfRangeException">Field out of range for order type</exception>
        public decimal Get(OrderField field)
        {
            return Get<decimal>(field);
        }

        /// <summary>
        /// Gets the specified field from the ticket and tries to convert it to the specified type
        /// </summary>
        /// <param name="field">The order field to get</param>
        /// <returns>The value of the field</returns>
        /// <exception cref="ArgumentException">Field out of range</exception>
        /// <exception cref="ArgumentOutOfRangeException">Field out of range for order type</exception>
        public T Get<T>(OrderField field)
        {
            object fieldValue = null;

            switch (field)
            {
                case OrderField.LimitPrice:
                    if (_submitRequest.OrderType == OrderType.ComboLimit)
                    {
                        fieldValue = AccessOrder<ComboLimitOrder, decimal>(this, field, o => o.GroupOrderManager.LimitPrice, r => r.LimitPrice);
                    }
                    else if (_submitRequest.OrderType == OrderType.ComboLegLimit)
                    {
                        fieldValue = AccessOrder<ComboLegLimitOrder, decimal>(this, field, o => o.LimitPrice, r => r.LimitPrice);
                    }
                    else if (_submitRequest.OrderType == OrderType.Limit)
                    {
                        fieldValue = AccessOrder<LimitOrder, decimal>(this, field, o => o.LimitPrice, r => r.LimitPrice);
                    }
                    else if (_submitRequest.OrderType == OrderType.StopLimit)
                    {
                        fieldValue = AccessOrder<StopLimitOrder, decimal>(this, field, o => o.LimitPrice, r => r.LimitPrice);
                    }
                    else if (_submitRequest.OrderType == OrderType.LimitIfTouched)
                    {
                        fieldValue = AccessOrder<LimitIfTouchedOrder, decimal>(this, field, o => o.LimitPrice, r => r.LimitPrice);
                    }
                    break;

                case OrderField.StopPrice:
                    if (_submitRequest.OrderType == OrderType.StopLimit)
                    {
                        fieldValue = AccessOrder<StopLimitOrder, decimal>(this, field, o => o.StopPrice, r => r.StopPrice);
                    }
                    else if (_submitRequest.OrderType == OrderType.StopMarket)
                    {
                        fieldValue = AccessOrder<StopMarketOrder, decimal>(this, field, o => o.StopPrice, r => r.StopPrice);
                    }
                    else if (_submitRequest.OrderType == OrderType.TrailingStop)
                    {
                        fieldValue = AccessOrder<TrailingStopOrder, decimal>(this, field, o => o.StopPrice, r => r.StopPrice);
                    }
                    break;

                case OrderField.TriggerPrice:
                    fieldValue = AccessOrder<LimitIfTouchedOrder, decimal>(this, field, o => o.TriggerPrice, r => r.TriggerPrice);
                    break;

                case OrderField.TrailingAmount:
                    fieldValue = AccessOrder<TrailingStopOrder, decimal>(this, field, o => o.TrailingAmount, r => r.TrailingAmount);
                    break;

                case OrderField.TrailingAsPercentage:
                    fieldValue = AccessOrder<TrailingStopOrder, bool>(this, field, o => o.TrailingAsPercentage, r => r.TrailingAsPercentage);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(field), field, null);
            }

            if (fieldValue == null)
            {
                throw new ArgumentException(Messages.OrderTicket.GetFieldError(this, field));
            }

            return (T)fieldValue;
        }

        /// <summary>
        /// Submits an <see cref="UpdateOrderRequest"/> with the <see cref="SecurityTransactionManager"/> to update
        /// the ticket with data specified in <paramref name="fields"/>
        /// </summary>
        /// <param name="fields">Defines what properties of the order should be updated</param>
        /// <returns>The <see cref="OrderResponse"/> from updating the order</returns>
        public OrderResponse Update(UpdateOrderFields fields)
        {
            var ticket = _transactionManager.UpdateOrder(new UpdateOrderRequest(_transactionManager.UtcTime, SubmitRequest.OrderId, fields));
            return ticket.UpdateRequests.Last().Response;
        }

        /// <summary>
        /// Submits an <see cref="UpdateOrderRequest"/> with the <see cref="SecurityTransactionManager"/> to update
        /// the ticket with tag specified in <paramref name="tag"/>
        /// </summary>
        /// <param name="tag">The new tag for this order ticket</param>
        /// <returns><see cref="OrderResponse"/> from updating the order</returns>
        public OrderResponse UpdateTag(string tag)
        {
            var fields = new UpdateOrderFields()
            {
                Tag = tag
            };
            return Update(fields);
        }

        /// <summary>
        /// Submits an <see cref="UpdateOrderRequest"/> with the <see cref="SecurityTransactionManager"/> to update
        /// the ticket with quantity specified in <paramref name="quantity"/> and with tag specified in <paramref name="quantity"/>
        /// </summary>
        /// <param name="quantity">The new quantity for this order ticket</param>
        /// <param name="tag">The new tag for this order ticket</param>
        /// <returns><see cref="OrderResponse"/> from updating the order</returns>
        public OrderResponse UpdateQuantity(decimal quantity, string tag = null)
        {
            var fields = new UpdateOrderFields()
            {
                Quantity = quantity,
                Tag = tag
            };
            return Update(fields);
        }

        /// <summary>
        /// Submits an <see cref="UpdateOrderRequest"/> with the <see cref="SecurityTransactionManager"/> to update
        /// the ticker with limit price specified in <paramref name="limitPrice"/> and with tag specified in <paramref name="tag"/>
        /// </summary>
        /// <param name="limitPrice">The new limit price for this order ticket</param>
        /// <param name="tag">The new tag for this order ticket</param>
        /// <returns><see cref="OrderResponse"/> from updating the order</returns>
        public OrderResponse UpdateLimitPrice(decimal limitPrice, string tag = null)
        {
            var fields = new UpdateOrderFields()
            {
                LimitPrice = limitPrice,
                Tag = tag
            };
            return Update(fields);
        }

        /// <summary>
        /// Submits an <see cref="UpdateOrderRequest"/> with the <see cref="SecurityTransactionManager"/> to update
        /// the ticker with stop price specified in <paramref name="stopPrice"/> and with tag specified in <paramref name="tag"/>
        /// </summary>
        /// <param name="stopPrice">The new stop price  for this order ticket</param>
        /// <param name="tag">The new tag for this order ticket</param>
        /// <returns><see cref="OrderResponse"/> from updating the order</returns>
        public OrderResponse UpdateStopPrice(decimal stopPrice, string tag = null)
        {
            var fields = new UpdateOrderFields()
            {
                StopPrice = stopPrice,
                Tag = tag
            };
            return Update(fields);
        }

        /// <summary>
        /// Submits an <see cref="UpdateOrderRequest"/> with the <see cref="SecurityTransactionManager"/> to update
        /// the ticker with trigger price specified in <paramref name="triggerPrice"/> and with tag specified in <paramref name="tag"/>
        /// </summary>
        /// <param name="triggerPrice">The new price which, when touched, will trigger the setting of a limit order.</param>
        /// <param name="tag">The new tag for this order ticket</param>
        /// <returns><see cref="OrderResponse"/> from updating the order</returns>
        public OrderResponse UpdateTriggerPrice(decimal triggerPrice, string tag = null)
        {
            var fields = new UpdateOrderFields()
            {
                TriggerPrice = triggerPrice,
                Tag = tag
            };
            return Update(fields);
        }

        /// <summary>
        /// Submits an <see cref="UpdateOrderRequest"/> with the <see cref="SecurityTransactionManager"/> to update
        /// the ticker with stop trailing amount specified in <paramref name="trailingAmount"/> and with tag specified in <paramref name="tag"/>
        /// </summary>
        /// <param name="trailingAmount">The new trailing amount for this order ticket</param>
        /// <param name="tag">The new tag for this order ticket</param>
        /// <returns><see cref="OrderResponse"/> from updating the order</returns>
        public OrderResponse UpdateStopTrailingAmount(decimal trailingAmount, string tag = null)
        {
            var fields = new UpdateOrderFields()
            {
                TrailingAmount = trailingAmount,
                Tag = tag
            };
            return Update(fields);
        }

        /// <summary>
        /// Submits a new request to cancel this order
        /// </summary>
        public OrderResponse Cancel(string tag = null)
        {
            var request = new CancelOrderRequest(_transactionManager.UtcTime, OrderId, tag);
            lock (_lock)
            {
                // don't submit duplicate cancel requests, if the cancel request wasn't flagged as error
                // this could happen when trying to cancel an order which status is still new and hasn't even been submitted to the brokerage
                if (_cancelRequest != null && _cancelRequest.Status != OrderRequestStatus.Error)
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.RequestCanceled,
                        Messages.OrderTicket.CancelRequestAlreadySubmitted(this));
                }
            }

            var ticket = _transactionManager.ProcessRequest(request);
            return ticket.CancelRequest.Response;
        }

        /// <summary>
        /// Gets the most recent <see cref="OrderResponse"/> for this ticket
        /// </summary>
        /// <returns>The most recent <see cref="OrderResponse"/> for this ticket</returns>
        public OrderResponse GetMostRecentOrderResponse()
        {
            return GetMostRecentOrderRequest().Response;
        }

        /// <summary>
        /// Gets the most recent <see cref="OrderRequest"/> for this ticket
        /// </summary>
        /// <returns>The most recent <see cref="OrderRequest"/> for this ticket</returns>
        public OrderRequest GetMostRecentOrderRequest()
        {
            lock (_lock)
            {
                if (_cancelRequest != null)
                {
                    return _cancelRequest;
                }

                // Avoid creating the update requests list if not necessary
                if (_updateRequestsImpl != null)
                {
                    var lastUpdate = _updateRequestsImpl.LastOrDefault();
                    if (lastUpdate != null)
                    {
                        return lastUpdate;
                    }
                }
            }
            return SubmitRequest;
        }

        /// <summary>
        /// Adds an order event to this ticket
        /// </summary>
        /// <param name="orderEvent">The order event to be added</param>
        internal void AddOrderEvent(OrderEvent orderEvent)
        {
            lock (_lock)
            {
                _orderEvents.Add(orderEvent);

                // Update the ticket and order
                if (orderEvent.FillQuantity != 0)
                {
                    var filledQuantity = _fillState.QuantityFilled;
                    var averageFillPrice = _fillState.AverageFillPrice;

                    if (_order.Type != OrderType.OptionExercise)
                    {
                        // keep running totals of quantity filled and the average fill price so we
                        // don't need to compute these on demand
                        filledQuantity += orderEvent.FillQuantity;
                        var quantityWeightedFillPrice = _orderEvents.Where(x => x.Status.IsFill())
                            .Aggregate(0m, (d, x) => d + x.AbsoluteFillQuantity * x.FillPrice);
                        averageFillPrice = quantityWeightedFillPrice / Math.Abs(filledQuantity);

                        _order.Price = averageFillPrice;
                    }
                    // For ITM option exercise orders we set the order price to the strike price.
                    // For OTM the fill price should be zero, which is the default for OptionExerciseOrders
                    else if (orderEvent.IsInTheMoney)
                    {
                        _order.Price = Symbol.ID.StrikePrice;

                        // We update the ticket only if the fill price is not zero (this fixes issue #2846 where average price
                        // is skewed by the removal of the option).
                        if (orderEvent.FillPrice != 0)
                        {
                            filledQuantity += orderEvent.FillQuantity;
                            averageFillPrice = _order.Price;
                        }
                    }

                    _fillState = new FillState(averageFillPrice, filledQuantity);
                }
            }

            // fire the wait handle indicating this order is closed
            if (orderEvent.Status.IsClosed())
            {
                _orderStatusClosedEvent.Set();
            }
        }

        /// <summary>
        /// Updates the internal order object with the current state
        /// </summary>
        /// <param name="order">The order</param>
        internal void SetOrder(Order order)
        {
            if (_order != null && _order.Id != order.Id)
            {
                throw new ArgumentException("Order id mismatch");
            }

            _order = order;

            _orderSetEvent.Set();
        }

        /// <summary>
        /// Adds a new <see cref="UpdateOrderRequest"/> to this ticket.
        /// </summary>
        /// <param name="request">The recently processed <see cref="UpdateOrderRequest"/></param>
        internal void AddUpdateRequest(UpdateOrderRequest request)
        {
            if (request.OrderId != OrderId)
            {
                throw new ArgumentException("Received UpdateOrderRequest for incorrect order id.");
            }

            lock (_lock)
            {
                _updateRequests.Add(request);
            }
        }

        /// <summary>
        /// Sets the <see cref="CancelOrderRequest"/> for this ticket. This can only be performed once.
        /// </summary>
        /// <remarks>
        /// This method is thread safe.
        /// </remarks>
        /// <param name="request">The <see cref="CancelOrderRequest"/> that canceled this ticket.</param>
        /// <returns>False if the the CancelRequest has already been set, true if this call set it</returns>
        internal bool TrySetCancelRequest(CancelOrderRequest request)
        {
            if (request.OrderId != OrderId)
            {
                throw new ArgumentException("Received CancelOrderRequest for incorrect order id.");
            }

            lock (_lock)
            {
                // don't submit duplicate cancel requests, if the cancel request wasn't flagged as error
                // this could happen when trying to cancel an order which status is still new and hasn't even been submitted to the brokerage
                if (_cancelRequest != null && _cancelRequest.Status != OrderRequestStatus.Error)
                {
                    return false;
                }
                _cancelRequest = request;
            }

            return true;
        }

        /// <summary>
        /// Creates a new <see cref="OrderTicket"/> that represents trying to cancel an order for which no ticket exists
        /// </summary>
        public static OrderTicket InvalidCancelOrderId(SecurityTransactionManager transactionManager, CancelOrderRequest request)
        {
            var submit = new SubmitOrderRequest(OrderType.Market, SecurityType.Base, Symbol.Empty, 0, 0, 0, DateTime.MaxValue, request.Tag);
            submit.SetResponse(OrderResponse.UnableToFindOrder(request));
            submit.SetOrderId(request.OrderId);
            var ticket = new OrderTicket(transactionManager, submit);
            request.SetResponse(OrderResponse.UnableToFindOrder(request));
            ticket.TrySetCancelRequest(request);
            ticket._orderStatusOverride = OrderStatus.Invalid;
            return ticket;
        }

        /// <summary>
        /// Creates a new <see cref="OrderTicket"/> that represents trying to update an order for which no ticket exists
        /// </summary>
        public static OrderTicket InvalidUpdateOrderId(SecurityTransactionManager transactionManager, UpdateOrderRequest request)
        {
            var submit = new SubmitOrderRequest(OrderType.Market, SecurityType.Base, Symbol.Empty, 0, 0, 0, DateTime.MaxValue, request.Tag);
            submit.SetResponse(OrderResponse.UnableToFindOrder(request));
            submit.SetOrderId(request.OrderId);
            var ticket = new OrderTicket(transactionManager, submit);
            request.SetResponse(OrderResponse.UnableToFindOrder(request));
            ticket.AddUpdateRequest(request);
            ticket._orderStatusOverride = OrderStatus.Invalid;
            return ticket;
        }

        /// <summary>
        /// Creates a new <see cref="OrderTicket"/> that represents trying to submit a new order that had errors embodied in the <paramref name="response"/>
        /// </summary>
        public static OrderTicket InvalidSubmitRequest(SecurityTransactionManager transactionManager, SubmitOrderRequest request, OrderResponse response)
        {
            request.SetResponse(response);
            return new OrderTicket(transactionManager, request) { _orderStatusOverride = OrderStatus.Invalid };
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
            var requestCount = 1;
            var responseCount = _submitRequest.Response == OrderResponse.Unprocessed ? 0 : 1;
            lock (_lock)
            {
                // Avoid creating the update requests list if not necessary
                if (_updateRequestsImpl != null)
                {
                    requestCount += _updateRequestsImpl.Count;
                    responseCount += _updateRequestsImpl.Count(x => x.Response != OrderResponse.Unprocessed);
                }

                requestCount += _cancelRequest == null ? 0 : 1;
                responseCount += _cancelRequest == null || _cancelRequest.Response == OrderResponse.Unprocessed ? 0 : 1;
            }

            return Messages.OrderTicket.ToString(this, _order, requestCount, responseCount);
        }

        /// <summary>
        /// This is provided for API backward compatibility and will resolve to the order ID, except during
        /// an error, where it will return the integer value of the <see cref="OrderResponseErrorCode"/> from
        /// the most recent response
        /// </summary>
        public static implicit operator int(OrderTicket ticket)
        {
            var response = ticket.GetMostRecentOrderResponse();
            if (response != null && response.IsError)
            {
                return (int) response.ErrorCode;
            }
            return ticket.OrderId;
        }

        private static P AccessOrder<T, P>(OrderTicket ticket, OrderField field, Func<T, P> orderSelector, Func<SubmitOrderRequest, P> requestSelector)
            where T : Order
        {
            var order = ticket._order;
            if (order == null)
            {
                return requestSelector(ticket._submitRequest);
            }
            var typedOrder = order as T;
            if (typedOrder != null)
            {
                return orderSelector(typedOrder);
            }
            throw new ArgumentException(Invariant($"Unable to access property {field} on order of type {order.Type}"));
        }

        /// <summary>
        /// Reference wrapper for decimal average fill price and quantity filled.
        /// In order to update the average fill price and quantity filled, we create a new instance of this class
        /// so we avoid potential race conditions when accessing these properties
        /// (e.g. the decimals might be being updated and in a invalid state when being read)
        /// </summary>
        private class FillState
        {
            public decimal AverageFillPrice { get; }
            public decimal QuantityFilled { get; }

            public FillState(decimal averageFillPrice, decimal quantityFilled)
            {
                AverageFillPrice = averageFillPrice;
                QuantityFilled = quantityFilled;
            }
        }
    }
}
