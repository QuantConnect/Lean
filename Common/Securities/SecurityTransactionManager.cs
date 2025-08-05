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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using Python.Runtime;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Algorithm Transactions Manager - Recording Transactions
    /// </summary>
    public class SecurityTransactionManager : IOrderProvider
    {
        private class TransactionRecordEntry
        {
            public decimal ProfitLoss;
            public bool IsWin;
        }

        private readonly Dictionary<DateTime, TransactionRecordEntry> _transactionRecord;
        private readonly IAlgorithm _algorithm;
        private int _orderId;
        private int _groupOrderManagerId;
        private readonly SecurityManager _securities;
        private TimeSpan _marketOrderFillTimeout = TimeSpan.MinValue;

        private IOrderProcessor _orderProcessor;

        /// <summary>
        /// Gets the time the security information was last updated
        /// </summary>
        public DateTime UtcTime
        {
            get { return _securities.UtcTime; }
        }

        /// <summary>
        /// Initialise the transaction manager for holding and processing orders.
        /// </summary>
        public SecurityTransactionManager(IAlgorithm algorithm, SecurityManager security)
        {
            _algorithm = algorithm;

            //Private reference for processing transactions
            _securities = security;

            //Internal storage for transaction records:
            _transactionRecord = new Dictionary<DateTime, TransactionRecordEntry>();
        }

        /// <summary>
        /// Trade record of profits and losses for each trade statistics calculations
        /// </summary>
        /// <remarks>Will return a shallow copy, modifying the returned container
        /// will have no effect <see cref="AddTransactionRecord"/></remarks>
        public Dictionary<DateTime, decimal> TransactionRecord
        {
            get
            {
                lock (_transactionRecord)
                {
                    return _transactionRecord.ToDictionary(x => x.Key, x => x.Value.ProfitLoss);
                }
            }
        }

        /// <summary>
        /// Gets the number or winning transactions
        /// </summary>
        public int WinCount
        {
            get
            {
                lock (_transactionRecord)
                {
                    return _transactionRecord.Values.Count(x => x.IsWin);
                }
            }
        }

        /// <summary>
        /// Gets the number of losing transactions
        /// </summary>
        public int LossCount
        {
            get
            {
                lock (_transactionRecord)
                {
                    return _transactionRecord.Values.Count(x => !x.IsWin);
                }
            }
        }

        /// <summary>
        /// Trade record of profits and losses for each trade statistics calculations that are considered winning trades
        /// </summary>
        public Dictionary<DateTime, decimal> WinningTransactions
        {
            get
            {
                lock (_transactionRecord)
                {
                    return _transactionRecord.Where(x => x.Value.IsWin).ToDictionary(x => x.Key, x => x.Value.ProfitLoss);
                }
            }
        }

        /// <summary>
        /// Trade record of profits and losses for each trade statistics calculations that are considered losing trades
        /// </summary>
        public Dictionary<DateTime, decimal> LosingTransactions
        {
            get
            {
                lock (_transactionRecord)
                {
                    return _transactionRecord.Where(x => !x.Value.IsWin).ToDictionary(x => x.Key, x => x.Value.ProfitLoss);
                }
            }
        }

        /// <summary>
        /// Configurable minimum order value to ignore bad orders, or orders with unrealistic sizes
        /// </summary>
        /// <remarks>Default minimum order size is $0 value</remarks>
        [Obsolete("MinimumOrderSize is obsolete and will not be used, please use Settings.MinimumOrderMarginPortfolioPercentage instead")]
        public decimal MinimumOrderSize { get; }

        /// <summary>
        /// Configurable minimum order size to ignore bad orders, or orders with unrealistic sizes
        /// </summary>
        /// <remarks>Default minimum order size is 0 shares</remarks>
        [Obsolete("MinimumOrderQuantity is obsolete and will not be used, please use Settings.MinimumOrderMarginPortfolioPercentage instead")]
        public int MinimumOrderQuantity { get; }

        /// <summary>
        /// Get the last order id.
        /// </summary>
        public int LastOrderId
        {
            get
            {
                return _orderId;
            }
        }

        /// <summary>
        /// Configurable timeout for market order fills
        /// </summary>
        /// <remarks>Default value is 5 seconds</remarks>
        public TimeSpan MarketOrderFillTimeout
        {
            get
            {
                return _marketOrderFillTimeout;
            }
            set
            {
                _marketOrderFillTimeout = value;
            }
        }

        /// <summary>
        /// Processes the order request
        /// </summary>
        /// <param name="request">The request to be processed</param>
        /// <returns>The order ticket for the request</returns>
        public OrderTicket ProcessRequest(OrderRequest request)
        {
            if (_algorithm != null && _algorithm.IsWarmingUp)
            {
                throw new Exception(OrderResponse.WarmingUp(request).ToString());
            }

            var submit = request as SubmitOrderRequest;
            if (submit != null)
            {
                SetOrderId(submit);
            }
            return _orderProcessor.Process(request);
        }

        /// <summary>
        /// Sets the order id for the specified submit request
        /// </summary>
        /// <param name="request">Request to set the order id for</param>
        /// <remarks>This method is public so we can request an order id from outside the assembly, for testing for example</remarks>
        public void SetOrderId(SubmitOrderRequest request)
        {
            // avoid setting the order id if it's already been set
            if (request.OrderId < 1)
            {
                request.SetOrderId(GetIncrementOrderId());
            }
        }

        /// <summary>
        /// Add an order to collection and return the unique order id or negative if an error.
        /// </summary>
        /// <param name="request">A request detailing the order to be submitted</param>
        /// <returns>New unique, increasing orderid</returns>
        public OrderTicket AddOrder(SubmitOrderRequest request)
        {
            return ProcessRequest(request);
        }

        /// <summary>
        /// Update an order yet to be filled such as stop or limit orders.
        /// </summary>
        /// <param name="request">Request detailing how the order should be updated</param>
        /// <remarks>Does not apply if the order is already fully filled</remarks>
        public OrderTicket UpdateOrder(UpdateOrderRequest request)
        {
            return ProcessRequest(request);
        }

        /// <summary>
        /// Added alias for RemoveOrder -
        /// </summary>
        /// <param name="orderId">Order id we wish to cancel</param>
        /// <param name="orderTag">Tag to indicate from where this method was called</param>
        public OrderTicket CancelOrder(int orderId, string orderTag = null)
        {
            return RemoveOrder(orderId, orderTag);
        }

        /// <summary>
        /// Cancels all open orders for all symbols
        /// </summary>
        /// <returns>List containing the cancelled order tickets</returns>
        public List<OrderTicket> CancelOpenOrders()
        {
            if (_algorithm != null && _algorithm.IsWarmingUp)
            {
                throw new InvalidOperationException(Messages.SecurityTransactionManager.CancelOpenOrdersNotAllowedOnInitializeOrWarmUp);
            }

            var cancelledOrders = new List<OrderTicket>();
            foreach (var ticket in GetOpenOrderTickets())
            {
                ticket.Cancel(Messages.SecurityTransactionManager.OrderCanceledByCancelOpenOrders(_algorithm.UtcTime));
                cancelledOrders.Add(ticket);
            }
            return cancelledOrders;
        }

        /// <summary>
        /// Cancels all open orders for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol whose orders are to be cancelled</param>
        /// <param name="tag">Custom order tag</param>
        /// <returns>List containing the cancelled order tickets</returns>
        public List<OrderTicket> CancelOpenOrders(Symbol symbol, string tag = null)
        {
            if (_algorithm != null && _algorithm.IsWarmingUp)
            {
                throw new InvalidOperationException(Messages.SecurityTransactionManager.CancelOpenOrdersNotAllowedOnInitializeOrWarmUp);
            }

            var cancelledOrders = new List<OrderTicket>();
            foreach (var ticket in GetOpenOrderTickets(x => x.Symbol == symbol))
            {
                ticket.Cancel(tag);
                cancelledOrders.Add(ticket);
            }
            return cancelledOrders;
        }

        /// <summary>
        /// Remove this order from outstanding queue: user is requesting a cancel.
        /// </summary>
        /// <param name="orderId">Specific order id to remove</param>
        /// <param name="tag">Tag request</param>
        public OrderTicket RemoveOrder(int orderId, string tag = null)
        {
            return ProcessRequest(new CancelOrderRequest(_securities.UtcTime, orderId, tag ?? string.Empty));
        }

        /// <summary>
        /// Gets an enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The filter predicate used to find the required order tickets</param>
        /// <returns>An enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _orderProcessor.GetOrderTickets(filter ?? (x => true));
        }

        /// <summary>
        /// Gets an enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The Python function filter used to find the required order tickets</param>
        /// <returns>An enumerable of <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOrderTickets(PyObject filter)
        {
            return _orderProcessor.GetOrderTickets(filter.SafeAs<Func<OrderTicket, bool>>());
        }

        /// <summary>
        /// Get an enumerable of open <see cref="OrderTicket"/> for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol for which to return the order tickets</param>
        /// <returns>An enumerable of open <see cref="OrderTicket"/>.</returns>
        public IEnumerable<OrderTicket> GetOpenOrderTickets(Symbol symbol)
        {
            return GetOpenOrderTickets(x => x.Symbol == symbol);
        }

        /// <summary>
        /// Gets an enumerable of opened <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// </summary>
        /// <param name="filter">The filter predicate used to find the required order tickets</param>
        /// <returns>An enumerable of opened <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOpenOrderTickets(Func<OrderTicket, bool> filter = null)
        {
            return _orderProcessor.GetOpenOrderTickets(filter ?? (x => true));
        }

        /// <summary>
        /// Gets an enumerable of opened <see cref="OrderTicket"/> matching the specified <paramref name="filter"/>
        /// However, this method can be confused with the override that takes a Symbol as parameter. For this reason
        /// it first checks if it can convert the parameter into a symbol. If that conversion cannot be aplied it
        /// assumes the parameter is a Python function object and not a Python representation of a Symbol.
        /// </summary>
        /// <param name="filter">The Python function filter used to find the required order tickets</param>
        /// <returns>An enumerable of opened <see cref="OrderTicket"/> matching the specified <paramref name="filter"/></returns>
        public IEnumerable<OrderTicket> GetOpenOrderTickets(PyObject filter)
        {
            Symbol pythonSymbol;
            if (filter.TryConvert(out pythonSymbol))
            {
                return GetOpenOrderTickets(pythonSymbol);
            }
            return _orderProcessor.GetOpenOrderTickets(filter.SafeAs<Func<OrderTicket, bool>>());
        }

        /// <summary>
        /// Gets the remaining quantity to be filled from open orders, i.e. order size minus quantity filled
        /// </summary>
        /// <param name="filter">Filters the order tickets to be included in the aggregate quantity remaining to be filled</param>
        /// <returns>Total quantity that hasn't been filled yet for all orders that were not filtered</returns>
        public decimal GetOpenOrdersRemainingQuantity(Func<OrderTicket, bool> filter = null)
        {
            return GetOpenOrderTickets(filter)
                .Aggregate(0m, (d, t) => d + t.QuantityRemaining);
        }

        /// <summary>
        /// Gets the remaining quantity to be filled from open orders, i.e. order size minus quantity filled
        /// However, this method can be confused with the override that takes a Symbol as parameter. For this reason
        /// it first checks if it can convert the parameter into a symbol. If that conversion cannot be aplied it
        /// assumes the parameter is a Python function object and not a Python representation of a Symbol.
        /// </summary>
        /// <param name="filter">Filters the order tickets to be included in the aggregate quantity remaining to be filled</param>
        /// <returns>Total quantity that hasn't been filled yet for all orders that were not filtered</returns>
        public decimal GetOpenOrdersRemainingQuantity(PyObject filter)
        {
            Symbol pythonSymbol;
            if (filter.TryConvert(out pythonSymbol))
            {
                return GetOpenOrdersRemainingQuantity(pythonSymbol);
            }

            return GetOpenOrderTickets(filter)
                .Aggregate(0m, (d, t) => d + t.QuantityRemaining);
        }

        /// <summary>
        /// Gets the remaining quantity to be filled from open orders for a Symbol, i.e. order size minus quantity filled
        /// </summary>
        /// <param name="symbol">Symbol to get the remaining quantity of currently open orders</param>
        /// <returns>Total quantity that hasn't been filled yet for orders matching the Symbol</returns>
        public decimal GetOpenOrdersRemainingQuantity(Symbol symbol)
        {
            return GetOpenOrdersRemainingQuantity(t => t.Symbol == symbol);
        }

        /// <summary>
        /// Gets the order ticket for the specified order id. Returns null if not found
        /// </summary>
        /// <param name="orderId">The order's id</param>
        /// <returns>The order ticket with the specified id, or null if not found</returns>
        public OrderTicket GetOrderTicket(int orderId)
        {
            return _orderProcessor.GetOrderTicket(orderId);
        }

        /// <summary>
        /// Wait for a specific order to be either Filled, Invalid or Canceled
        /// </summary>
        /// <param name="orderId">The id of the order to wait for</param>
        /// <returns>True if we successfully wait for the fill, false if we were unable
        /// to wait. This may be because it is not a market order or because the timeout
        /// was reached</returns>
        public bool WaitForOrder(int orderId)
        {
            var orderTicket = GetOrderTicket(orderId);
            if (orderTicket == null)
            {
                Log.Error($@"SecurityTransactionManager.WaitForOrder(): {
                    Messages.SecurityTransactionManager.UnableToLocateOrderTicket(orderId)}");

                return false;
            }

            if (!orderTicket.OrderClosed.WaitOne(_marketOrderFillTimeout))
            {
                if(_marketOrderFillTimeout > TimeSpan.Zero)
                {
                    Log.Error($@"SecurityTransactionManager.WaitForOrder(): {Messages.SecurityTransactionManager.OrderNotFilledWithinExpectedTime(_marketOrderFillTimeout)}");
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a list of all open orders for a symbol.
        /// </summary>
        /// <param name="symbol">The symbol for which to return the orders</param>
        /// <returns>List of open orders.</returns>
        public List<Order> GetOpenOrders(Symbol symbol)
        {
            return GetOpenOrders(x => x.Symbol == symbol);
        }

        /// <summary>
        /// Gets open orders matching the specified filter. Specifying null will return an enumerable
        /// of all open orders.
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All filtered open orders this order provider currently holds</returns>
        public List<Order> GetOpenOrders(Func<Order, bool> filter = null)
        {
            filter = filter ?? (x => true);
            return _orderProcessor.GetOpenOrders(x => filter(x));
        }

        /// <summary>
        /// Gets open orders matching the specified filter. However, this method can be confused with the
        /// override that takes a Symbol as parameter. For this reason it first checks if it can convert
        /// the parameter into a symbol. If that conversion cannot be aplied it assumes the parameter is
        /// a Python function object and not a Python representation of a Symbol.
        /// </summary>
        /// <param name="filter">Python function object used to filter the orders</param>
        /// <returns>All filtered open orders this order provider currently holds</returns>
        public List<Order> GetOpenOrders(PyObject filter)
        {
            Symbol pythonSymbol;
            if (filter.TryConvert(out pythonSymbol))
            {
                return GetOpenOrders(pythonSymbol);
            }
            Func<Order, bool> csharpFilter = filter.SafeAs<Func<Order, bool>>();
            return _orderProcessor.GetOpenOrders(x => csharpFilter(x));
        }

        /// <summary>
        /// Gets the current number of orders that have been processed
        /// </summary>
        public int OrdersCount
        {
            get { return _orderProcessor.OrdersCount; }
        }

        /// <summary>
        /// Get the order by its id
        /// </summary>
        /// <param name="orderId">Order id to fetch</param>
        /// <returns>A clone of the order with the specified id, or null if no match is found</returns>
        public Order GetOrderById(int orderId)
        {
            return _orderProcessor.GetOrderById(orderId);
        }

        /// <summary>
        /// Gets the order by its brokerage id
        /// </summary>
        /// <param name="brokerageId">The brokerage id to fetch</param>
        /// <returns>The first order matching the brokerage id, or null if no match is found</returns>
        public List<Order> GetOrdersByBrokerageId(string brokerageId)
        {
            return _orderProcessor.GetOrdersByBrokerageId(brokerageId);
        }

        /// <summary>
        /// Gets all orders matching the specified filter. Specifying null will return an enumerable
        /// of all orders.
        /// </summary>
        /// <param name="filter">Delegate used to filter the orders</param>
        /// <returns>All orders this order provider currently holds by the specified filter</returns>
        public IEnumerable<Order> GetOrders(Func<Order, bool> filter = null)
        {
            return _orderProcessor.GetOrders(filter ?? (x => true));
        }

        /// <summary>
        /// Gets all orders matching the specified filter.
        /// </summary>
        /// <param name="filter">Python function object used to filter the orders</param>
        /// <returns>All orders this order provider currently holds by the specified filter</returns>
        public IEnumerable<Order> GetOrders(PyObject filter)
        {
            return _orderProcessor.GetOrders(filter.SafeAs<Func<Order, bool>>());
        }

        /// <summary>
        /// Get a new order id, and increment the internal counter.
        /// </summary>
        /// <returns>New unique int order id.</returns>
        public int GetIncrementOrderId()
        {
            return Interlocked.Increment(ref _orderId);
        }

        /// <summary>
        /// Get a new group order manager id, and increment the internal counter.
        /// </summary>
        /// <returns>New unique int group order manager id.</returns>
        public int GetIncrementGroupOrderManagerId()
        {
            return Interlocked.Increment(ref _groupOrderManagerId);
        }

        /// <summary>
        /// Sets the <see cref="IOrderProvider"/> used for fetching orders for the algorithm
        /// </summary>
        /// <param name="orderProvider">The <see cref="IOrderProvider"/> to be used to manage fetching orders</param>
        public void SetOrderProcessor(IOrderProcessor orderProvider)
        {
            _orderProcessor = orderProvider;
        }

        /// <summary>
        /// Record the transaction value and time in a list to later be processed for statistics creation.
        /// </summary>
        /// <remarks>
        /// Bit of a hack -- but using datetime as dictionary key is dangerous as you can process multiple orders within a second.
        /// For the accounting / statistics generating purposes its not really critical to know the precise time, so just add a millisecond while there's an identical key.
        /// </remarks>
        /// <param name="time">Time of order processed </param>
        /// <param name="transactionProfitLoss">Profit Loss.</param>
        /// <param name="isWin">
        /// Whether the transaction is a win.
        /// For options exercise, this might not depend only on the profit/loss value
        /// </param>
        public void AddTransactionRecord(DateTime time, decimal transactionProfitLoss, bool isWin)
        {
            lock (_transactionRecord)
            {
                var clone = time;
                while (_transactionRecord.ContainsKey(clone))
                {
                    clone = clone.AddMilliseconds(1);
                }
                _transactionRecord.Add(clone, new TransactionRecordEntry { ProfitLoss = transactionProfitLoss, IsWin = isWin });
            }
        }

        /// <summary>
        /// Set live mode state of the algorithm
        /// </summary>
        /// <param name="isLiveMode">True, live mode is enabled</param>
        public void SetLiveMode(bool isLiveMode)
        {
            if (isLiveMode)
            {
                if(MarketOrderFillTimeout == TimeSpan.MinValue)
                {
                    // set default value in live trading
                    MarketOrderFillTimeout = TimeSpan.FromSeconds(5);
                }
            }
            else
            {
                // always zero in backtesting, fills happen synchronously, there's no dedicated thread like in live
                MarketOrderFillTimeout = TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Calculates the projected holdings for the specified security based on the current open orders.
        /// </summary>
        /// <param name="security">The security</param>
        /// <returns>
        /// The projected holdings for the specified security, which is the sum of the current holdings
        /// plus the sum of the open orders quantity.
        /// </returns>
        public ProjectedHoldings GetProjectedHoldings(Security security)
        {
            return _orderProcessor.GetProjectedHoldings(security);
        }
    }
}
