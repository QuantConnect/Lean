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
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Brokerages.CrossZero;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents the base Brokerage implementation. This provides logging on brokerage events.
    /// </summary>
    public abstract class Brokerage : IBrokerage
    {
        // 7:45 AM (New York time zone)
        private static readonly TimeSpan LiveBrokerageCashSyncTime = new TimeSpan(7, 45, 0);

        private readonly object _performCashSyncReentranceGuard = new object();
        private bool _syncedLiveBrokerageCashToday = true;
        private long _lastSyncTimeTicks = DateTime.UtcNow.Ticks;

        /// <summary>
        /// Event that fires each time the brokerage order id changes
        /// </summary>
        public event EventHandler<BrokerageOrderIdChangedEvent> OrderIdChanged;

        /// <summary>
        /// Event that fires each time the status for a list of orders change
        /// </summary>
        public event EventHandler<List<OrderEvent>> OrdersStatusChanged;

        /// <summary>
        /// Event that fires each time an order is updated in the brokerage side
        /// </summary>
        /// <remarks>
        /// These are not status changes but mainly price changes, like the stop price of a trailing stop order
        /// </remarks>
        public event EventHandler<OrderUpdateEvent> OrderUpdated;

        /// <summary>
        /// Event that fires each time a short option position is assigned
        /// </summary>
        public event EventHandler<OrderEvent> OptionPositionAssigned;

        /// <summary>
        /// Event that fires each time an option position has changed
        /// </summary>
        public event EventHandler<OptionNotificationEventArgs> OptionNotification;

        /// <summary>
        /// Event that fires each time there's a brokerage side generated order
        /// </summary>
        public event EventHandler<NewBrokerageOrderNotificationEventArgs> NewBrokerageOrderNotification;

        /// <summary>
        /// Event that fires each time a delisting occurs
        /// </summary>
        public event EventHandler<DelistingNotificationEventArgs> DelistingNotification;

        /// <summary>
        /// Event that fires each time a user's brokerage account is changed
        /// </summary>
        public event EventHandler<AccountEvent> AccountChanged;

        /// <summary>
        /// Event that fires when an error is encountered in the brokerage
        /// </summary>
        public event EventHandler<BrokerageMessageEvent> Message;

        /// <summary>
        /// Gets the name of the brokerage
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Creates a new Brokerage instance with the specified name
        /// </summary>
        /// <param name="name">The name of the brokerage</param>
        protected Brokerage(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public abstract bool PlaceOrder(Order order);

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public abstract bool UpdateOrder(Order order);

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public abstract bool CancelOrder(Order order);

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// Dispose of the brokerage instance
        /// </summary>
        public virtual void Dispose()
        {
            // NOP
        }

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="orderEvents">The list of order events</param>
        protected virtual void OnOrderEvents(List<OrderEvent> orderEvents)
        {
            try
            {
                OrdersStatusChanged?.Invoke(this, orderEvents);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="e">The order event</param>
        protected virtual void OnOrderEvent(OrderEvent e)
        {
            OnOrderEvents(new List<OrderEvent> { e });
        }

        /// <summary>
        /// Event invocator for the OrderUpdated event
        /// </summary>
        /// <param name="e">The update event</param>
        protected virtual void OnOrderUpdated(OrderUpdateEvent e)
        {
            try
            {
                OrderUpdated?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the OrderIdChanged event
        /// </summary>
        /// <param name="e">The BrokerageOrderIdChangedEvent</param>
        protected virtual void OnOrderIdChangedEvent(BrokerageOrderIdChangedEvent e)
        {
            try
            {
                OrderIdChanged?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the OptionPositionAssigned event
        /// </summary>
        /// <param name="e">The OrderEvent</param>
        protected virtual void OnOptionPositionAssigned(OrderEvent e)
        {
            try
            {
                Log.Debug("Brokerage.OptionPositionAssigned(): " + e);

                OptionPositionAssigned?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the OptionNotification event
        /// </summary>
        /// <param name="e">The OptionNotification event arguments</param>
        protected virtual void OnOptionNotification(OptionNotificationEventArgs e)
        {
            try
            {
                Log.Debug("Brokerage.OnOptionNotification(): " + e);

                OptionNotification?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the NewBrokerageOrderNotification event
        /// </summary>
        /// <param name="e">The NewBrokerageOrderNotification event arguments</param>
        protected virtual void OnNewBrokerageOrderNotification(NewBrokerageOrderNotificationEventArgs e)
        {
            try
            {
                Log.Debug("Brokerage.OnNewBrokerageOrderNotification(): " + e);

                NewBrokerageOrderNotification?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the DelistingNotification event
        /// </summary>
        /// <param name="e">The DelistingNotification event arguments</param>
        protected virtual void OnDelistingNotification(DelistingNotificationEventArgs e)
        {
            try
            {
                Log.Debug("Brokerage.OnDelistingNotification(): " + e);

                DelistingNotification?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the AccountChanged event
        /// </summary>
        /// <param name="e">The AccountEvent</param>
        protected virtual void OnAccountChanged(AccountEvent e)
        {
            try
            {
                Log.Trace($"Brokerage.OnAccountChanged(): {e}");

                AccountChanged?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Event invocator for the Message event
        /// </summary>
        /// <param name="e">The error</param>
        protected virtual void OnMessage(BrokerageMessageEvent e)
        {
            try
            {
                if (e.Type == BrokerageMessageType.Error)
                {
                    Log.Error("Brokerage.OnMessage(): " + e);
                }
                else
                {
                    Log.Trace("Brokerage.OnMessage(): " + e);
                }

                Message?.Invoke(this, e);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Helper method that will try to get the live holdings from the provided brokerage data collection else will default to the algorithm state
        /// </summary>
        /// <remarks>Holdings will removed from the provided collection on the first call, since this method is expected to be called only
        /// once on initialize, after which the algorithm should use Lean accounting</remarks>
        protected virtual List<Holding> GetAccountHoldings(Dictionary<string, string> brokerageData, IEnumerable<Security> securities)
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug("Brokerage.GetAccountHoldings(): starting...");
            }

            if (brokerageData != null && brokerageData.Remove("live-holdings", out var value) && !string.IsNullOrEmpty(value))
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug($"Brokerage.GetAccountHoldings(): raw value: {value}");
                }

                // remove the key, we really only want to return the cached value on the first request
                var result = JsonConvert.DeserializeObject<List<Holding>>(value);
                if (result == null)
                {
                    return new List<Holding>();
                }
                Log.Trace($"Brokerage.GetAccountHoldings(): sourcing holdings from provided brokerage data, found {result.Count} entries");
                return result;
            }

            return securities?.Where(security => security.Holdings.AbsoluteQuantity > 0)
                .OrderBy(security => security.Symbol)
                .Select(security => new Holding(security)).ToList() ?? new List<Holding>();
        }

        /// <summary>
        /// Helper method that will try to get the live cash balance from the provided brokerage data collection else will default to the algorithm state
        /// </summary>
        /// <remarks>Cash balance will removed from the provided collection on the first call, since this method is expected to be called only
        /// once on initialize, after which the algorithm should use Lean accounting</remarks>
        protected virtual List<CashAmount> GetCashBalance(Dictionary<string, string> brokerageData, CashBook cashBook)
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug("Brokerage.GetCashBalance(): starting...");
            }

            if (brokerageData != null && brokerageData.Remove("live-cash-balance", out var value) && !string.IsNullOrEmpty(value))
            {
                // remove the key, we really only want to return the cached value on the first request
                var result = JsonConvert.DeserializeObject<List<CashAmount>>(value);
                if (result == null)
                {
                    return new List<CashAmount>();
                }
                Log.Trace($"Brokerage.GetCashBalance(): sourcing cash balance from provided brokerage data, found {result.Count} entries");
                return result;
            }

            return cashBook?.Select(x => new CashAmount(x.Value.Amount, x.Value.Symbol)).ToList() ?? new List<CashAmount>();
        }

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public abstract List<Order> GetOpenOrders();

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public abstract List<Holding> GetAccountHoldings();

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public abstract List<CashAmount> GetCashBalance();

        /// <summary>
        /// Specifies whether the brokerage will instantly update account balances
        /// </summary>
        public virtual bool AccountInstantlyUpdated => false;

        /// <summary>
        /// Returns the brokerage account's base currency
        /// </summary>
        public virtual string AccountBaseCurrency { get; protected set; }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public virtual IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            return Enumerable.Empty<BaseData>();
        }

        /// <summary>
        /// Gets the position that might result given the specified order direction and the current holdings quantity.
        /// This is useful for brokerages that require more specific direction information than provided by the OrderDirection enum
        /// (e.g. Tradier differentiates Buy/Sell and BuyToOpen/BuyToCover/SellShort/SellToClose)
        /// </summary>
        /// <param name="orderDirection">The order direction</param>
        /// <param name="holdingsQuantity">The current holdings quantity</param>
        /// <returns>The order position</returns>
        protected static OrderPosition GetOrderPosition(OrderDirection orderDirection, decimal holdingsQuantity)
        {
            return orderDirection switch
            {
                OrderDirection.Buy => holdingsQuantity >= 0 ? OrderPosition.BuyToOpen : OrderPosition.BuyToClose,
                OrderDirection.Sell => holdingsQuantity <= 0 ? OrderPosition.SellToOpen : OrderPosition.SellToClose,
                _ => throw new ArgumentOutOfRangeException(nameof(orderDirection), orderDirection, "Invalid order direction")
            };
        }

        #region IBrokerageCashSynchronizer implementation

        /// <summary>
        /// Gets the date of the last sync (New York time zone)
        /// </summary>
        protected DateTime LastSyncDate => LastSyncDateTimeUtc.ConvertFromUtc(TimeZones.NewYork).Date;

        /// <summary>
        /// Gets the datetime of the last sync (UTC)
        /// </summary>
        public DateTime LastSyncDateTimeUtc => new DateTime(Interlocked.Read(ref _lastSyncTimeTicks));

        /// <summary>
        /// Returns whether the brokerage should perform the cash synchronization
        /// </summary>
        /// <param name="currentTimeUtc">The current time (UTC)</param>
        /// <returns>True if the cash sync should be performed</returns>
        public virtual bool ShouldPerformCashSync(DateTime currentTimeUtc)
        {
            // every morning flip this switch back
            var currentTimeNewYork = currentTimeUtc.ConvertFromUtc(TimeZones.NewYork);
            if (_syncedLiveBrokerageCashToday && currentTimeNewYork.Date != LastSyncDate)
            {
                _syncedLiveBrokerageCashToday = false;
            }

            return !_syncedLiveBrokerageCashToday && currentTimeNewYork.TimeOfDay >= LiveBrokerageCashSyncTime;
        }

        /// <summary>
        /// Synchronizes the cashbook with the brokerage account
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="currentTimeUtc">The current time (UTC)</param>
        /// <param name="getTimeSinceLastFill">A function which returns the time elapsed since the last fill</param>
        /// <returns>True if the cash sync was performed successfully</returns>
        public virtual bool PerformCashSync(IAlgorithm algorithm, DateTime currentTimeUtc, Func<TimeSpan> getTimeSinceLastFill)
        {
            try
            {
                // prevent reentrance in this method
                if (!Monitor.TryEnter(_performCashSyncReentranceGuard))
                {
                    Log.Trace("Brokerage.PerformCashSync(): Reentrant call, cash sync not performed");
                    return false;
                }

                Log.Trace("Brokerage.PerformCashSync(): Sync cash balance");

                List<CashAmount> balances = null;
                try
                {
                    balances = GetCashBalance();
                }
                catch (Exception err)
                {
                    Log.Error(err, "Error in GetCashBalance:");
                }

                // empty cash balance is valid, if there was No error/exception
                if (balances == null)
                {
                    Log.Trace("Brokerage.PerformCashSync(): No cash balances available, cash sync not performed");
                    return false;
                }

                // Adds currency to the cashbook that the user might have deposited
                foreach (var balance in balances)
                {
                    if (!algorithm.Portfolio.CashBook.ContainsKey(balance.Currency))
                    {
                        Log.Trace($"Brokerage.PerformCashSync(): Unexpected cash found {balance.Currency} {balance.Amount}", true);
                        algorithm.Portfolio.SetCash(balance.Currency, balance.Amount, 0);
                    }
                }

                var totalPorfolioValueThreshold = algorithm.Portfolio.TotalPortfolioValue * 0.02m;
                // if we were returned our balances, update everything and flip our flag as having performed sync today
                foreach (var kvp in algorithm.Portfolio.CashBook)
                {
                    var cash = kvp.Value;

                    //update the cash if the entry if found in the balances
                    var balanceCash = balances.Find(balance => balance.Currency == cash.Symbol);
                    if (balanceCash != default(CashAmount))
                    {
                        // compare in account currency
                        var delta = cash.Amount - balanceCash.Amount;
                        if (Math.Abs(algorithm.Portfolio.CashBook.ConvertToAccountCurrency(delta, cash.Symbol)) > totalPorfolioValueThreshold)
                        {
                            // log the delta between
                            Log.Trace($"Brokerage.PerformCashSync(): {balanceCash.Currency} Delta: {delta:0.00}", true);
                        }
                        algorithm.Portfolio.CashBook[cash.Symbol].SetAmount(balanceCash.Amount);
                    }
                    else
                    {
                        //Set the cash amount to zero if cash entry not found in the balances
                        Log.Trace($"Brokerage.PerformCashSync(): {cash.Symbol} was not found in brokerage cash balance, setting the amount to 0", true);
                        algorithm.Portfolio.CashBook[cash.Symbol].SetAmount(0);
                    }
                }
                _syncedLiveBrokerageCashToday = true;
                _lastSyncTimeTicks = currentTimeUtc.Ticks;
            }
            finally
            {
                Monitor.Exit(_performCashSyncReentranceGuard);
            }

            // fire off this task to check if we've had recent fills, if we have then we'll invalidate the cash sync
            // and do it again until we're confident in it
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
                {
                    // we want to make sure this is a good value, so check for any recent fills
                    if (getTimeSinceLastFill() <= TimeSpan.FromSeconds(20))
                    {
                        // this will cause us to come back in and reset cash again until we
                        // haven't processed a fill for +- 10 seconds of the set cash time
                        _syncedLiveBrokerageCashToday = false;
                        //_failedCashSyncAttempts = 0;
                        Log.Trace("Brokerage.PerformCashSync(): Unverified cash sync - resync required.");
                    }
                    else
                    {
                        Log.Trace("Brokerage.PerformCashSync(): Verified cash sync.");

                        algorithm.Portfolio.LogMarginInformation();
                    }
                });

            return true;
        }

        #endregion

        #region CrossZeroOrder implementation

        /// <summary>
        /// A dictionary to store the relationship between brokerage crossing orders and Lean orer id.
        /// </summary>
        private readonly ConcurrentDictionary<int, CrossZeroSecondOrderRequest> _leanOrderByBrokerageCrossingOrders = new();

        /// <summary>
        /// An object used to lock the critical section in the <see cref="TryGetOrRemoveCrossZeroOrder"/> method,
        /// ensuring thread safety when accessing the order collection.
        /// </summary>
        private object _lockCrossZeroObject = new();

        /// <summary>
        /// A thread-safe dictionary that maps brokerage order IDs to their corresponding Order objects.
        /// </summary>
        /// <remarks>
        /// This ConcurrentDictionary is used to maintain a mapping between Zero Cross brokerage order IDs and Lean Order objects. 
        /// The dictionary is protected and read-only, ensuring that it can only be modified by the class that declares it and cannot 
        /// be assigned a new instance after initialization.
        /// </remarks>
        protected ConcurrentDictionary<string, Order> LeanOrderByZeroCrossBrokerageOrderId { get; } = new();

        /// <summary>
        /// Places an order that crosses zero (transitions from a short position to a long position or vice versa) and returns the response.
        /// This method should be overridden in a derived class to implement brokerage-specific logic for placing such orders.
        /// </summary>
        /// <param name="crossZeroOrderRequest">The request object containing details of the cross zero order to be placed.</param>
        /// <param name="isPlaceOrderWithLeanEvent">
        /// A boolean indicating whether the order should be placed with triggering a Lean event. 
        /// Default is <c>true</c>, meaning Lean events will be triggered.
        /// </param>
        /// <returns>
        /// A <see cref="CrossZeroOrderResponse"/> object indicating the result of the order placement.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Thrown if the method is not overridden in a derived class.
        /// </exception>
        protected virtual CrossZeroOrderResponse PlaceCrossZeroOrder(CrossZeroFirstOrderRequest crossZeroOrderRequest, bool isPlaceOrderWithLeanEvent = true)
        {
            throw new NotImplementedException($"{nameof(PlaceCrossZeroOrder)} method should be overridden in the derived class to handle brokerage-specific logic.");
        }

        /// <summary>
        /// Attempts to place an order that may cross the zero position. 
        /// If the order needs to be split into two parts due to crossing zero, 
        /// this method handles the split and placement accordingly.
        /// </summary>
        /// <param name="order">The order to be placed. Must not be <c>null</c>.</param>
        /// <param name="holdingQuantity">The current holding quantity of the order's symbol.</param>
        /// <returns>
        /// <para><c>true</c> if the order crosses zero and the first part was successfully placed;</para>
        /// <para><c>false</c> if the first part of the order could not be placed;</para>
        /// <para><c>null</c> if the order does not cross zero.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="order"/> is <c>null</c>.
        /// </exception>
        protected bool? TryCrossZeroPositionOrder(Order order, decimal holdingQuantity)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order), "The order parameter cannot be null.");
            }

            // do we need to split the order into two pieces?
            var crossesZero = BrokerageExtensions.OrderCrossesZero(holdingQuantity, order.Quantity);
            if (crossesZero)
            {
                // first we need an order to close out the current position
                var (firstOrderQuantity, secondOrderQuantity) = GetQuantityOnCrossPosition(holdingQuantity, order.Quantity);

                // Note: original quantity - already sell
                var firstOrderPartRequest = new CrossZeroFirstOrderRequest(order, order.Type, firstOrderQuantity, holdingQuantity,
                    GetOrderPosition(order.Direction, holdingQuantity));

                // we actually can't place this order until the closingOrder is filled
                // create another order for the rest, but we'll convert the order type to not be a stop
                // but a market or a limit order                
                var secondOrderPartRequest = new CrossZeroSecondOrderRequest(order, order.Type, secondOrderQuantity, 0m,
                    GetOrderPosition(order.Direction, 0m), firstOrderPartRequest);

                _leanOrderByBrokerageCrossingOrders.AddOrUpdate(order.Id, secondOrderPartRequest);

                CrossZeroOrderResponse response;
                lock (_lockCrossZeroObject)
                {
                    // issue the first order to close the position
                    response = PlaceCrossZeroOrder(firstOrderPartRequest);
                    if (response.IsOrderPlacedSuccessfully)
                    {
                        var orderId = response.BrokerageOrderId;
                        if (!order.BrokerId.Contains(orderId))
                        {
                            order.BrokerId.Add(orderId);
                        }
                    }
                }

                if (!response.IsOrderPlacedSuccessfully)
                {
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, $"{nameof(Brokerage)}: {response.Message}")
                    {
                        Status = OrderStatus.Invalid
                    });
                    // remove the contingent order if we weren't successful in placing the first
                    //ContingentOrderQueue contingent;
                    _leanOrderByBrokerageCrossingOrders.TryRemove(order.Id, out _);
                    return false;
                }
                return true;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the given Lean order crosses zero quantity based on the initial order quantity.
        /// </summary>
        /// <param name="leanOrder">The Lean order to check.</param>
        /// <param name="quantity">The quantity to be updated based on whether the order crosses zero.</param>
        /// <returns>
        /// <c>true</c> if the Lean order does not cross zero quantity; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="leanOrder"/> is null.</exception>
        protected bool TryGetUpdateCrossZeroOrderQuantity(Order leanOrder, out decimal quantity)
        {
            if (leanOrder == null)
            {
                throw new ArgumentNullException(nameof(leanOrder), "The provided leanOrder cannot be null.");
            }

            // Check if the order is a CrossZeroOrder.
            if (_leanOrderByBrokerageCrossingOrders.TryGetValue(leanOrder.Id, out var crossZeroOrderRequest))
            {
                // If it is a CrossZeroOrder, use the first part of the quantity for the update.
                quantity = crossZeroOrderRequest.FirstPartCrossZeroOrder.OrderQuantity;
                // If the quantities of the LeanOrder do not match, return false. Don't support.
                if (crossZeroOrderRequest.LeanOrder.Quantity != leanOrder.Quantity)
                {
                    return false;
                }
            }
            else
            {
                // If it is not a CrossZeroOrder, use the original order quantity.
                quantity = leanOrder.Quantity;
            }
            return true;
        }

        /// <summary>
        /// Attempts to retrieve or remove a cross-zero order based on the brokerage order ID and its filled status.
        /// </summary>
        /// <param name="brokerageOrderId">The unique identifier of the brokerage order.</param>
        /// <param name="leanOrderStatus">The updated status of the order received from the brokerage</param>
        /// <param name="leanOrder">
        /// When this method returns, contains the <see cref="Order"/> object associated with the given brokerage order ID,
        /// if the operation was successful; otherwise, null.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <c>true</c> if the method successfully retrieves or removes the order; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The method locks on a private object to ensure thread safety while accessing the collection of orders.
        /// If the order is filled, it is removed from the collection. If the order is partially filled,
        /// it is retrieved but not removed. If the order is not found, the method returns <c>false</c>.
        /// </remarks>
        protected bool TryGetOrRemoveCrossZeroOrder(string brokerageOrderId, OrderStatus leanOrderStatus, out Order leanOrder)
        {
            lock (_lockCrossZeroObject)
            {
                if (LeanOrderByZeroCrossBrokerageOrderId.TryGetValue(brokerageOrderId, out leanOrder))
                {
                    switch (leanOrderStatus)
                    {
                        case OrderStatus.Filled:
                        case OrderStatus.Canceled:
                        case OrderStatus.Invalid:
                            LeanOrderByZeroCrossBrokerageOrderId.TryRemove(brokerageOrderId, out var _);
                            break;
                    };
                    return true;
                }
                // Return false if the brokerage order ID does not correspond to a cross-zero order
                return false;
            }
        }

        /// <summary>
        /// Attempts to handle any remaining orders that cross the zero boundary.
        /// </summary>
        /// <param name="leanOrder">The order object that needs to be processed.</param>
        /// <param name="orderEvent">The event object containing order event details.</param>
        protected bool TryHandleRemainingCrossZeroOrder(Order leanOrder, OrderEvent orderEvent)
        {
            if (leanOrder != null && orderEvent != null && _leanOrderByBrokerageCrossingOrders.TryGetValue(leanOrder.Id, out var brokerageOrder))
            {
                switch (orderEvent.Status)
                {
                    case OrderStatus.Filled:
                        // if we have a contingent that needs to be submitted then we can't respect the 'Filled' state from the order
                        // because the Lean order hasn't been technically filled yet, so mark it as 'PartiallyFilled'
                        orderEvent.Status = OrderStatus.PartiallyFilled;
                        _leanOrderByBrokerageCrossingOrders.Remove(leanOrder.Id, out var _);
                        break;
                    case OrderStatus.Canceled:
                    case OrderStatus.Invalid:
                        _leanOrderByBrokerageCrossingOrders.Remove(leanOrder.Id, out var _);
                        return false;
                    default:
                        return false;
                };

                OnOrderEvent(orderEvent);

                Task.Run(() =>
                {
#pragma warning disable CA1031 // Do not catch general exception types
                    try
                    {
                        var response = default(CrossZeroOrderResponse);
                        lock (_lockCrossZeroObject)
                        {
                            Log.Trace($"{nameof(Brokerage)}.{nameof(TryHandleRemainingCrossZeroOrder)}: Submit the second part of cross order by Id:{leanOrder.Id}");
                            response = PlaceCrossZeroOrder(brokerageOrder, false);

                            if (response.IsOrderPlacedSuccessfully)
                            {
                                // add the new brokerage id for retrieval later
                                var orderId = response.BrokerageOrderId;
                                if (!leanOrder.BrokerId.Contains(orderId))
                                {
                                    leanOrder.BrokerId.Add(orderId);
                                }

                                // leanOrder is a clone, here we can add the new brokerage order Id for the second part of the cross zero
                                OnOrderIdChangedEvent(new BrokerageOrderIdChangedEvent { OrderId = leanOrder.Id, BrokerId = leanOrder.BrokerId });
                                LeanOrderByZeroCrossBrokerageOrderId.AddOrUpdate(orderId, leanOrder);
                            }
                        }

                        if (!response.IsOrderPlacedSuccessfully)
                        {
                            // if we failed to place this order I don't know what to do, we've filled the first part
                            // and failed to place the second... strange. Should we invalidate the rest of the order??
                            Log.Error($"{nameof(Brokerage)}.{nameof(TryHandleRemainingCrossZeroOrder)}: Failed to submit contingent order.");
                            var message = $"{leanOrder.Symbol} Failed submitting the second part of cross order for " +
                                $"LeanOrderId: {leanOrder.Id.ToStringInvariant()} Filled - BrokerageOrderId: {response.BrokerageOrderId}. " +
                                $"{response.Message}";
                            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "CrossZeroFailed", message));
                            OnOrderEvent(new OrderEvent(leanOrder, DateTime.UtcNow, OrderFee.Zero) { Status = OrderStatus.Canceled });
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "CrossZeroOrderError", "Error occurred submitting cross zero order: " + err.Message));
                        OnOrderEvent(new OrderEvent(leanOrder, DateTime.UtcNow, OrderFee.Zero) { Status = OrderStatus.Canceled });
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                });
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the quantities needed to close the current position and establish a new position based on the provided order.
        /// </summary>
        /// <param name="holdingQuantity">The quantity currently held in the position that needs to be closed.</param>
        /// <param name="orderQuantity">The quantity defined in the new order to be established.</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item>
        /// <description>The quantity needed to close the current position (negative value).</description>
        /// </item>
        /// <item>
        /// <description>The quantity needed to establish the new position.</description>
        /// </item>
        /// </list>
        /// </returns>
        private static (decimal closePostionQunatity, decimal newPositionQuantity) GetQuantityOnCrossPosition(decimal holdingQuantity, decimal orderQuantity)
        {
            // first we need an order to close out the current position
            var firstOrderQuantity = -holdingQuantity;
            var secondOrderQuantity = orderQuantity - firstOrderQuantity;

            return (firstOrderQuantity, secondOrderQuantity);
        }

        #endregion
    }
}
