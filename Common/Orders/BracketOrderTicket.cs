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
using QuantConnect.Securities;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides a single handle to the linked tickets of a bracket order: an entry order plus protective
    /// stop loss and/or take profit exit orders with one-cancels-the-other (OCO) semantics.
    /// </summary>
    /// <remarks>
    /// The exit legs are placed by the engine once the entry order closes, sized to the actual filled
    /// quantity, so <see cref="StopLossTicket"/> and <see cref="TakeProfitTicket"/> are null until then.
    /// The linkage is guaranteed by the engine without any user code (see
    /// <see cref="SecurityTransactionManager.ProcessBracketOrderEvent"/>): when one leg fills its sibling
    /// is canceled, and when the position is closed or flipped by an unrelated order the remaining legs
    /// are canceled. The stop loss leg is submitted first so that when a single bar spans both leg prices
    /// it wins deterministically (backtesting fills scan orders by ascending id).
    /// </remarks>
    public class BracketOrderTicket
    {
        private readonly object _lock = new object();
        private readonly SecurityTransactionManager _transactionManager;
        private readonly SubmitOrderRequest _entryRequest;

        private decimal? _stopLossPrice;
        private decimal? _takeProfitPrice;
        // entry state tracked from order events so it is accurate even before the entry ticket
        // reference is set (in live trading fills can be processed while the submit call is in flight)
        private decimal _entryFilledQuantity;
        private bool _entryClosed;
        private bool _legsPlaced;
        private bool _canceled;
        private bool _completed;

        /// <summary>
        /// The symbol being traded by this bracket
        /// </summary>
        public Symbol Symbol => _entryRequest.Symbol;

        /// <summary>
        /// The quantity of the entry order. The exit legs are sized to the actual filled entry quantity
        /// </summary>
        public decimal Quantity => _entryRequest.Quantity;

        /// <summary>
        /// The stop loss price, null if no stop loss leg was requested
        /// </summary>
        public decimal? StopLossPrice
        {
            get { lock (_lock) { return _stopLossPrice; } }
        }

        /// <summary>
        /// The take profit price, null if no take profit leg was requested
        /// </summary>
        public decimal? TakeProfitPrice
        {
            get { lock (_lock) { return _takeProfitPrice; } }
        }

        /// <summary>
        /// The ticket of the entry order
        /// </summary>
        public OrderTicket EntryTicket { get; private set; }

        /// <summary>
        /// The ticket of the protective stop loss order. Null until the entry order fills
        /// </summary>
        public OrderTicket StopLossTicket { get; private set; }

        /// <summary>
        /// The ticket of the take profit order. Null until the entry order fills
        /// </summary>
        public OrderTicket TakeProfitTicket { get; private set; }

        /// <summary>
        /// True while any piece of the bracket is still working: the entry order is open, the entry
        /// filled and the exit legs are pending placement, or any exit leg is open. A new bracket
        /// cannot be placed for the same symbol while an active one exists
        /// </summary>
        public bool IsActive
        {
            get
            {
                lock (_lock)
                {
                    if (_completed)
                    {
                        return false;
                    }
                    if (_legsPlaced)
                    {
                        return IsTicketOpen(StopLossTicket) || IsTicketOpen(TakeProfitTicket);
                    }
                    // consult the live ticket state and not only our event-driven flags: ticket statuses
                    // are updated before the user's OnOrderEvent runs, while our flags are updated after
                    // it, so checks made inside the event handler see the true state
                    if (_entryClosed || EntryTicket != null && EntryTicket.Status.IsClosed())
                    {
                        return !_canceled && EntryFilledQuantity != 0;
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BracketOrderTicket"/> class
        /// </summary>
        /// <param name="transactionManager">The transaction manager used for submitting the exit legs and cancels</param>
        /// <param name="entryRequest">The submit request of the entry order. Must already have its order id set</param>
        /// <param name="stopLossPrice">The stop price of the protective stop market leg, null for no stop loss</param>
        /// <param name="takeProfitPrice">The limit price of the take profit leg, null for no take profit</param>
        public BracketOrderTicket(SecurityTransactionManager transactionManager, SubmitOrderRequest entryRequest,
            decimal? stopLossPrice, decimal? takeProfitPrice)
        {
            _transactionManager = transactionManager;
            _entryRequest = entryRequest;
            _stopLossPrice = stopLossPrice;
            _takeProfitPrice = takeProfitPrice;
        }

        /// <summary>
        /// Requests cancellation of every open piece of this bracket: the entry order if it is still
        /// open and any open exit leg. If the entry order already partially filled, the resulting
        /// position is left as is, no protective legs will be placed for it
        /// </summary>
        /// <param name="tag">Optional reason to attach to the cancel requests</param>
        public void Cancel(string tag = null)
        {
            lock (_lock)
            {
                _canceled = true;
                TryCancel(EntryTicket, tag ?? "Canceled bracket order");
                TryCancel(StopLossTicket, tag ?? "Canceled bracket order");
                TryCancel(TakeProfitTicket, tag ?? "Canceled bracket order");
            }
        }

        /// <summary>
        /// Moves the stop loss to the specified stop price. If the exit legs have not been placed yet,
        /// the pending stop loss price is updated in place, otherwise an update request is submitted
        /// for the live stop loss order
        /// </summary>
        /// <param name="stopPrice">The new stop price</param>
        /// <param name="tag">Optional new tag for the order</param>
        /// <returns>The response of the update</returns>
        public OrderResponse MoveStopLoss(decimal stopPrice, string tag = null)
        {
            lock (_lock)
            {
                if (StopLossTicket != null)
                {
                    var response = StopLossTicket.UpdateStopPrice(stopPrice, tag);
                    if (!response.IsError)
                    {
                        _stopLossPrice = stopPrice;
                    }
                    return response;
                }

                var request = new UpdateOrderRequest(_transactionManager.UtcTime, _entryRequest.OrderId,
                    new UpdateOrderFields { StopPrice = stopPrice, Tag = tag });
                if (!_stopLossPrice.HasValue || !IsActive)
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.InvalidRequest,
                        $"BracketOrderTicket.MoveStopLoss(): the bracket for {Symbol} has no live or pending stop loss to move.");
                }
                _stopLossPrice = stopPrice;
                return OrderResponse.Success(request);
            }
        }

        /// <summary>
        /// Moves the take profit to the specified limit price. If the exit legs have not been placed yet,
        /// the pending take profit price is updated in place, otherwise an update request is submitted
        /// for the live take profit order
        /// </summary>
        /// <param name="limitPrice">The new limit price</param>
        /// <param name="tag">Optional new tag for the order</param>
        /// <returns>The response of the update</returns>
        public OrderResponse MoveTakeProfit(decimal limitPrice, string tag = null)
        {
            lock (_lock)
            {
                if (TakeProfitTicket != null)
                {
                    var response = TakeProfitTicket.UpdateLimitPrice(limitPrice, tag);
                    if (!response.IsError)
                    {
                        _takeProfitPrice = limitPrice;
                    }
                    return response;
                }

                var request = new UpdateOrderRequest(_transactionManager.UtcTime, _entryRequest.OrderId,
                    new UpdateOrderFields { LimitPrice = limitPrice, Tag = tag });
                if (!_takeProfitPrice.HasValue || !IsActive)
                {
                    return OrderResponse.Error(request, OrderResponseErrorCode.InvalidRequest,
                        $"BracketOrderTicket.MoveTakeProfit(): the bracket for {Symbol} has no live or pending take profit to move.");
                }
                _takeProfitPrice = limitPrice;
                return OrderResponse.Success(request);
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            lock (_lock)
            {
                return $"BracketOrderTicket for {Symbol}: Quantity: {Quantity}, StopLoss: {_stopLossPrice}, " +
                    $"TakeProfit: {_takeProfitPrice}, Entry: {EntryTicket?.ToString() ?? "not submitted"}, " +
                    $"StopLossTicket: {StopLossTicket?.ToString() ?? "not placed"}, TakeProfitTicket: {TakeProfitTicket?.ToString() ?? "not placed"}";
            }
        }

        /// <summary>
        /// Creates a new <see cref="BracketOrderTicket"/> whose entry order submission had errors embodied
        /// in the <paramref name="response"/>. The resulting bracket is not active
        /// </summary>
        public static BracketOrderTicket InvalidEntry(SecurityTransactionManager transactionManager,
            SubmitOrderRequest entryRequest, OrderResponse response, decimal? stopLossPrice, decimal? takeProfitPrice)
        {
            var bracket = new BracketOrderTicket(transactionManager, entryRequest, stopLossPrice, takeProfitPrice);
            bracket.EntryTicket = OrderTicket.InvalidSubmitRequest(transactionManager, entryRequest, response);
            bracket._completed = true;
            return bracket;
        }

        /// <summary>
        /// Submits the entry order. Called by <see cref="SecurityTransactionManager.AddBracketOrder"/>
        /// after the bracket has been registered, so that in live trading a fill processed while the
        /// submission is in flight already finds the bracket
        /// </summary>
        internal void SubmitEntryOrder()
        {
            EntryTicket = _transactionManager.AddOrder(_entryRequest);
        }

        /// <summary>
        /// Processes an order event for the bracket's symbol, driving the OCO state machine. Called by
        /// the transaction handler for every order event, after the user's OnOrderEvent handler
        /// </summary>
        /// <param name="orderEvent">The order event to process</param>
        /// <param name="holdingsQuantity">The current holdings quantity for the bracket's symbol</param>
        /// <returns>True if any order request (submit, update or cancel) was issued as a result</returns>
        internal bool HandleOrderEvent(OrderEvent orderEvent, decimal holdingsQuantity)
        {
            lock (_lock)
            {
                if (_completed)
                {
                    return false;
                }

                var requestsIssued = false;
                if (orderEvent.OrderId == _entryRequest.OrderId)
                {
                    if (orderEvent.Status.IsFill())
                    {
                        _entryFilledQuantity += orderEvent.FillQuantity;
                    }
                    if (orderEvent.Status.IsClosed() && !_entryClosed)
                    {
                        _entryClosed = true;
                        var filledQuantity = EntryFilledQuantity;
                        if (_canceled || filledQuantity == 0)
                        {
                            // entry never filled (canceled or invalid) or the user canceled the whole
                            // bracket: there is nothing to protect
                            _completed = true;
                        }
                        else
                        {
                            // the entry closed with fills (including a canceled entry that partially
                            // filled: that position still needs its protective exits)
                            PlaceExitLegs(filledQuantity);
                            requestsIssued = true;
                        }
                    }
                }
                else if (_legsPlaced && (orderEvent.OrderId == StopLossTicket?.OrderId || orderEvent.OrderId == TakeProfitTicket?.OrderId))
                {
                    var leg = orderEvent.OrderId == StopLossTicket?.OrderId ? StopLossTicket : TakeProfitTicket;
                    var sibling = ReferenceEquals(leg, StopLossTicket) ? TakeProfitTicket : StopLossTicket;
                    if (orderEvent.Status == OrderStatus.Filled)
                    {
                        // one-cancels-the-other: this leg exited the bracket's position, its sibling
                        // must not fill too (both legs filling on a gapping bar flips the position,
                        // exhausting margin: fleet deployment A-20b9ed)
                        requestsIssued |= TryCancel(sibling, $"Bracket #{_entryRequest.OrderId} sibling leg filled");
                    }
                    else if (orderEvent.Status == OrderStatus.PartiallyFilled && IsTicketOpen(sibling))
                    {
                        // keep the sibling sized to what the bracket still holds so it cannot overshoot.
                        // both legs exit the same position so they share the sign of the filling leg's
                        // remaining quantity
                        requestsIssued |= TryResize(sibling, leg.QuantityRemaining);
                    }

                    if (!IsTicketOpen(StopLossTicket) && !IsTicketOpen(TakeProfitTicket))
                    {
                        _completed = true;
                    }
                }
                else if (_legsPlaced && orderEvent.Status.IsFill())
                {
                    // an unrelated order for the same symbol filled: keep the protective legs in sync
                    // with the remaining position so a stranded leg cannot idle against a closed position
                    // (fleet deployment A-6eb8558a: dangling stop leg produced 1931x InsufficientBuyingPower)
                    if (holdingsQuantity == 0 || Math.Sign(holdingsQuantity) != Math.Sign(Quantity))
                    {
                        requestsIssued |= TryCancel(StopLossTicket, $"Bracket #{_entryRequest.OrderId} position closed");
                        requestsIssued |= TryCancel(TakeProfitTicket, $"Bracket #{_entryRequest.OrderId} position closed");
                    }
                    else
                    {
                        // the position was partially reduced: downsize the legs so a later leg fill
                        // cannot flip the position. The legs are never sized up, the bracket only
                        // protects the quantity it filled
                        requestsIssued |= TryResize(StopLossTicket, -holdingsQuantity);
                        requestsIssued |= TryResize(TakeProfitTicket, -holdingsQuantity);
                    }
                }

                return requestsIssued;
            }
        }

        /// <summary>
        /// The entry quantity filled so far, from the ticket when available
        /// </summary>
        private decimal EntryFilledQuantity => EntryTicket?.QuantityFilled ?? _entryFilledQuantity;

        /// <summary>
        /// Places the protective exit legs for the filled entry quantity. The stop loss is submitted
        /// first on purpose: it gets the lower order id, and backtesting fills scan orders by ascending
        /// id, so when a single bar spans both leg prices the conservative stop loss exit wins
        /// </summary>
        private void PlaceExitLegs(decimal entryFilledQuantity)
        {
            var legQuantity = -entryFilledQuantity;
            var utcTime = _transactionManager.UtcTime;

            if (_stopLossPrice.HasValue)
            {
                var stopLossRequest = new SubmitOrderRequest(OrderType.StopMarket, _entryRequest.SecurityType, Symbol,
                    legQuantity, _stopLossPrice.Value, 0, utcTime, GetLegTag("stop loss"),
                    _entryRequest.OrderProperties?.Clone(), asynchronous: true);
                StopLossTicket = _transactionManager.AddOrder(stopLossRequest);
            }
            if (_takeProfitPrice.HasValue)
            {
                var takeProfitRequest = new SubmitOrderRequest(OrderType.Limit, _entryRequest.SecurityType, Symbol,
                    legQuantity, 0, _takeProfitPrice.Value, utcTime, GetLegTag("take profit"),
                    _entryRequest.OrderProperties?.Clone(), asynchronous: true);
                TakeProfitTicket = _transactionManager.AddOrder(takeProfitRequest);
            }
            _legsPlaced = true;
        }

        private string GetLegTag(string legName)
        {
            return string.IsNullOrEmpty(_entryRequest.Tag)
                ? $"Bracket #{_entryRequest.OrderId} {legName}"
                : $"{_entryRequest.Tag} ({legName})";
        }

        /// <summary>
        /// Requests cancellation of the ticket if it is open and not already being canceled
        /// </summary>
        private static bool TryCancel(OrderTicket ticket, string tag)
        {
            if (IsTicketOpen(ticket) && ticket.CancelRequest == null)
            {
                ticket.Cancel(tag);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Requests an update of the ticket quantity if it is open and oversized versus the target
        /// </summary>
        private static bool TryResize(OrderTicket ticket, decimal quantity)
        {
            if (IsTicketOpen(ticket) && ticket.CancelRequest == null && Math.Abs(ticket.Quantity) > Math.Abs(quantity))
            {
                ticket.UpdateQuantity(quantity);
                return true;
            }
            return false;
        }

        private static bool IsTicketOpen(OrderTicket ticket)
        {
            return ticket != null && !ticket.Status.IsClosed();
        }
    }
}
