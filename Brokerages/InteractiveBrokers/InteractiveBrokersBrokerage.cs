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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using IB = Krs.Ats.IBNet;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// The Interactive Brokers brokerage
    /// </summary>
    public sealed class InteractiveBrokersBrokerage : Brokerage
    {
        // next valid order id for this client
        private int _nextValidID;
        // the last known cash balance in the account
        private decimal _currentCashBalance;

        // next valid client id for the gateway/tws
        private static int _nextClientID;

        private readonly int _port;
        private readonly string _account;
        private readonly string _host;
        private readonly int _clientID;
        private readonly IB.IBClient _client;
        private readonly IB.AgentDescription _agentDescription;

        // the key here is the QC order ID
        private readonly ConcurrentDictionary<int, Order>  _outstandingOrders = new ConcurrentDictionary<int, Order>();
        private readonly Dictionary<string, string> _accountProperties = new Dictionary<string, string>(); 

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                if (_client == null) return false;
                return _client.Connected;
            }
        }

        /// <summary>
        /// Creates a new InteractiveBrokersBrokerage using values from configuration:
        ///     ib-account (required)
        ///     ib-host (optional, defaults to LOCALHOST)
        ///     ib-port (optional, defaults to 4001)
        ///     ib-agent-description (optional, defaults to Individual)
        /// </summary>
        public InteractiveBrokersBrokerage()
            : this(
                Config.Get("ib-account"),
                Config.Get("ib-host", "LOCALHOST"),
                Config.GetInt("ib-port", 4001),
                Config.GetValue("ib-agent-description", IB.AgentDescription.Individual)
                )
        {
        }

        /// <summary>
        /// Creates a new InteractiveBrokeragBrokerage for the specified account
        /// </summary>
        /// <param name="account">The account used to connect to IB</param>
        public InteractiveBrokersBrokerage(string account)
            : this(account,
                Config.Get("ib-host", "LOCALHOST"),
                Config.GetInt("ib-port", 4001),
                Config.GetValue("ib-agent-description", IB.AgentDescription.Individual)
                )
        {
        }

        /// <summary>
        /// Creates a new InteractiveBrokersBrokerage from the specified values
        /// </summary>
        /// <param name="account">The Interactive Brokers account name</param>
        /// <param name="host">host name or IP address of the machine where TWS is running. Leave blank to connect to the local host.</param>
        /// <param name="port">must match the port specified in TWS on the Configure&gt;API&gt;Socket Port field.</param>
        /// <param name="agentDescription">Used for Rule 80A describes the type of trader.</param>
        public InteractiveBrokersBrokerage(string account, string host, int port, IB.AgentDescription agentDescription = IB.AgentDescription.Individual)
            : base("Interactive Brokers Brokerage")
        {
            _account = account;
            _host = host;
            _port = port;
            _clientID = Interlocked.Increment(ref _nextClientID);
            _agentDescription = agentDescription;
            _client = new IB.IBClient();
        }

        /// <summary>
        /// Provides public access to the underlying IBClient instance
        /// </summary>
        public IB.IBClient Client
        {
            get { return _client; }
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            try
            {
                // add the order to our outstanding orders collection
                if (!_outstandingOrders.TryAdd(order.Id, order))
                {
                    // this order has already been placed
                    Log.Trace("InteractiveBrokersBrokerage.PlaceOrder(): Attempted to place order for existing order ID");
                    return false;
                }

                Log.Trace("InteractiveBrokersBrokerage.PlaceOrder(): Symbol: " + order.Symbol + " Quantity: " + order.Quantity);

                IBPlaceOrder(order);
                return true;
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.PlaceOrder(): " + err.Message);
                return false;
            }
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            try
            {
                Order outstanding;
                if (!_outstandingOrders.TryGetValue(order.Id, out outstanding))
                {
                    Log.Trace("InteractiveBrokersBrokerage.UpdateOrder(): Unable to update order " + order.Id + " because it is no longer outstanding");
                    return false;
                }

                if (!(outstanding.Status != OrderStatus.Filled && outstanding.Status != OrderStatus.Canceled))
                {
                    Log.Trace("InteractiveBrokersBrokerage.UpdateOrder(): Unable to update order " + order.Id + " because it is " + outstanding.Status);
                    return false;
                }

                Log.Trace("InteractiveBrokersBrokerage.UpdateOrder(): Symbol: " + order.Symbol + " Quantity: " + order.Quantity + " Status: " + order.Status);

                IBPlaceOrder(order);
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.UpdateOrder(): " + err.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            try
            {
                Log.Trace("InteractiveBrokersBrokerage.UpdateOrder(): Symbol: " + order.Symbol + " Quantity: " + order.Quantity);

                // this could be better
                foreach (var id in order.BrokerId)
                {
                    _client.CancelOrder((int) id);
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.CancelOrder(): OrderID: " + order.Id + " - " + err.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Order> GetOpenOrders()
        {
            var orders = new List<Order>();

            var manualResetEvent = new ManualResetEvent(false);
            
            // define our handlers
            EventHandler<IB.OpenOrderEventArgs> clientOnOpenOrder = (sender, args) => orders.Add(ConvertOrder(args.Order, args.Contract));
            EventHandler<EventArgs> clientOnOpenOrderEnd = (sender, args) => manualResetEvent.Set();

            // add the handlers
            _client.OpenOrder += clientOnOpenOrder;
            _client.OpenOrderEnd += clientOnOpenOrderEnd;

            _client.RequestOpenOrders();

            manualResetEvent.WaitOne();

            // remove the handlers
            _client.OpenOrder -= clientOnOpenOrder;
            _client.OpenOrderEnd -= clientOnOpenOrderEnd;

            return orders;
        }

        /// <summary>
        /// Gets the current USD cash balance in the brokerage account
        /// </summary>
        /// <returns>The current USD cash balance available for trading</returns>
        public override decimal GetCashBalance()
        {
            return _currentCashBalance;
        }

        /// <summary>
        /// Connects the client to the IB gateway
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            // set up event handlers
            _client.UpdatePortfolio += HandlePortfolioUpdates;
            _client.OrderStatus += HandleOrderStatusUpdates;
            _client.UpdateAccountValue += HandleUpdateAccountValue;
            _client.Error += HandleError;

            // we need to wait until we receive the next valid id from the server
            var manualResetEvent = new ManualResetEvent(false);
            _client.NextValidId += (sender, e) =>
            {
                // only grab this id when we initialize, and we'll manually increment it here to avoid threading issues
                if (_nextValidID == 0)
                {
                    _nextValidID = e.OrderId;
                    manualResetEvent.Set();
                }
                Log.Trace("InteractiveBrokersBrokerage.HandleNextValidID(): " + e.OrderId);
            };

            int attempt = 1;
            while (true)
            {
                try
                {
                    Log.Trace("InteractiveBrokersBrokerage.Connect(): Attempting to connect (" + attempt + "/10) ...");

                    // we're going to try and connect several times, if successful break
                    _client.Connect(_host, _port, _clientID);
                    break;
                }
                catch (Exception err)
                { 
                    // max out at 10 attempts to connect
                    if (attempt++ < 10)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    // we couldn't connect after several attempts, log the error and throw an exception
                    Log.Error("InteractiveBrokersBrokerage.Connect(): " + err.Message);
                    throw;
                }
            }

            // pause for a moment to receive next valid ID message from gateway
            if (!manualResetEvent.WaitOne(1000))
            {
                // we timed out our wait for next valid id... we'll just log it
                Log.Error("InteractiveBrokersBrokerage.Connect(): Timed out waiting for next valid ID event to fire.");
            }

            _client.RequestAccountUpdates(true, _account);
        }

        /// <summary>
        /// Disconnects the client from the IB gateway
        /// </summary>
        public override void Disconnect()
        {
            if (!IsConnected) return;

            _client.Disconnect();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
        }

        /// <summary>
        /// Places the order with InteractiveBrokers
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <param name="exchange">The exchange to send the order to, defaults to "Smart" to use IB's smart routing</param>
        private void IBPlaceOrder(Order order, string exchange = null)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("InteractiveBrokersBrokerage.IBPlaceOrder(): Unable to place order while not connected.");
            }

            var contract = CreateContract(order, exchange);
            var ibOrder = ConvertOrder(order);

            _client.PlaceOrder(ibOrder.OrderId, contract, ibOrder);
        }

        /// <summary>
        /// Handles error messages from IB
        /// </summary>
        private void HandleError(object sender, IB.ErrorEventArgs e)
        {
            // if greater than zero error generated from order or ticker
            var message = string.Format("InteractiveBrokersBrokerage.HandleError(): {0} - {1}", e.TickerId, e.ErrorMsg);
            if (e.TickerId > 0)
            {
                Log.Error(message);
            }
            else
            {
                Log.Trace(message);
            }
            
            OnError(new InteractiveBrokersException(e.ErrorCode, e.TickerId, e.ErrorMsg));
        }

        /// <summary>
        /// Stores all the account values
        /// </summary>
        private void HandleUpdateAccountValue(object sender, IB.UpdateAccountValueEventArgs e)
        {
            //https://www.interactivebrokers.com/en/software/api/apiguide/java/updateaccountvalue.htm

            try
            { 
                // not sure if we need to track all the information
                if (_accountProperties.ContainsKey(e.Key))
                {
                    _accountProperties[e.Key] = e.Value;
                }
                else
                {
                    _accountProperties.Add(e.Key, e.Value);
                }

                // we want to capture if the user's cash changes so we can reflect it in the algorithm
                if (e.Key == "CashBalance")
                {
                    var cashBalance = e.Value.ToDecimal();
                    _currentCashBalance = cashBalance;
                    OnAccountChanged(new AccountEvent(cashBalance));
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleUpdateAccountValue(): " + err.Message);
            }
        }

        /// <summary>
        /// Handle order events from IB
        /// </summary>
        private void HandleOrderStatusUpdates(object sender, IB.OrderStatusEventArgs update)
        {
            try
            { 
                // don't use .Values since it will require us to copy the dictionary
                var order = _outstandingOrders.FirstOrDefault(x => x.Value.BrokerId.Contains(update.OrderId)).Value;
                if (order == null)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Unable to resolve order " + update.OrderId);
                    return;
                }

                Log.Trace("InteractiveBrokersBrokerage.HandleOrderStatusUpdtes(): QC OrderID: " + order.Id + " IB OrderID: " + update.OrderId + " Status: " + update.Status);

                OnOrderEvent(new OrderEvent(order.Id, order.Symbol, ConvertOrderStatus(update.Status), update.AverageFillPrice, update.Filled, "Interactive Brokers Fill Event"));

                if (update.Remaining == 0)
                {
                    // the order is completed and no longer outstanding
                    _outstandingOrders.TryRemove(order.Id, out order);
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): " + err.Message);
            }
        }

        /// <summary>
        /// Handle portfolio changed events from IB
        /// </summary>
        private void HandlePortfolioUpdates(object sender, IB.UpdatePortfolioEventArgs e)
        {
            OnPortfolioChanged(new PortfolioEvent(e.Contract.Symbol, e.Position));
        }

        /// <summary>
        /// Converts a QC order to an IB order
        /// </summary>
        private IB.Order ConvertOrder(Order order)
        {
            var ibOrder = new IB.Order();
            ibOrder.ClientId = _clientID;

            int id = AddInteractiveBrokersOrderID(order);

            // the order ids are generated for us by the SecurityTransactionManaer
            ibOrder.OrderId = id;
            ibOrder.PermId = id;
            ibOrder.Action = ConvertOrderDirection(order.Direction);
            ibOrder.TotalQuantity = Math.Abs(order.Quantity);
            ibOrder.OrderType = ConvertOrderType(order.Type);

            if (ibOrder.OrderType == IB.OrderType.Limit)
            {
                ibOrder.LimitPrice = order.Price;
            }
            else if (ibOrder.OrderType == IB.OrderType.Stop)
            {
                ibOrder.AuxPrice = order.Price;
            }
            else if (ibOrder.OrderType == IB.OrderType.TrailingStop)
            {
                ibOrder.TrailStopPrice = order.Price;
            }

            // not yet supported
            //ibOrder.ParentId = 
            //ibOrder.OcaGroup =

            ibOrder.AllOrNone = false;
            ibOrder.Tif = IB.TimeInForce.GoodTillCancel;
            ibOrder.Transmit = true;
            ibOrder.Rule80A = _agentDescription;

            return ibOrder;
        }

        private Order ConvertOrder(IB.Order ibOrder, IB.Contract contract)
        {
            decimal price = 0;
            if (ibOrder.LimitPrice != 0)
            {
                price = ibOrder.LimitPrice;
            }
            else if (ibOrder.AuxPrice != 0)
            {
                price = ibOrder.AuxPrice;
            }

            var order = new Order(contract.Symbol,
                ConvertSecurityType(contract.SecurityType),
                ibOrder.TotalQuantity,
                ConvertOrderType(ibOrder.OrderType),
                new DateTime(), // not sure how to get this data
                price
                );

            order.BrokerId.Add(ibOrder.OrderId);

            return order;
        }

        /// <summary>
        /// Creates an IB contract from the order.
        /// </summary>
        /// <param name="order">The order to create a contract from</param>
        /// <param name="exchange">The exchange where the order will be placed, defaults to 'Smart'</param>
        /// <returns>A new IB contract for the order</returns>
        private IB.Contract CreateContract(Order order, string exchange = null)
        {
            var securityType = ConvertSecurityType(order.SecurityType);
            var contract = new IB.Contract(order.Symbol, exchange ?? "Smart", securityType, "USD");
            return contract;
        }

        /// <summary>
        /// Maps OrderDirection enumeration
        /// </summary>
        private IB.ActionSide ConvertOrderDirection(OrderDirection direction)
        {
            switch (direction)
            {
                case OrderDirection.Buy:  return IB.ActionSide.Buy;
                case OrderDirection.Sell: return IB.ActionSide.Sell;
                case OrderDirection.Hold: return IB.ActionSide.Undefined;
                default:
                    throw new InvalidEnumArgumentException("direction", (int) direction, typeof (OrderDirection));
            }
        }

        /// <summary>
        /// Maps OrderType enum
        /// </summary>
        private IB.OrderType ConvertOrderType(OrderType type)
        {
            switch (type)
            {
                case OrderType.Market:      return IB.OrderType.Market;
                case OrderType.Limit:       return IB.OrderType.Limit;
                case OrderType.StopMarket:  return IB.OrderType.Stop;
                default:
                    throw new InvalidEnumArgumentException("type", (int)type, typeof(OrderType));
            }
        }

        /// <summary>
        /// Maps OrderType enum
        /// </summary>
        private OrderType ConvertOrderType(IB.OrderType type)
        {
            switch (type)
            {
                case IB.OrderType.Market: return OrderType.Market;
                case IB.OrderType.Limit:  return OrderType.Limit;
                case IB.OrderType.Stop:   return OrderType.StopMarket;
                default:
                    throw new InvalidEnumArgumentException("type", (int)type, typeof(OrderType));
            }
        }

        /// <summary>
        /// Maps IB's OrderStats enum
        /// </summary>
        private OrderStatus ConvertOrderStatus(IB.OrderStatus status)
        {
            switch (status)
            {
                case IB.OrderStatus.ApiPending:
                case IB.OrderStatus.PendingSubmit:
                case IB.OrderStatus.PreSubmitted: 
                    return OrderStatus.New;

                case IB.OrderStatus.ApiCancelled:
                case IB.OrderStatus.PendingCancel:
                case IB.OrderStatus.Canceled: 
                    return OrderStatus.Canceled;

                case IB.OrderStatus.Submitted: 
                    return OrderStatus.Submitted;

                case IB.OrderStatus.Filled: 
                    return OrderStatus.Filled;

                case IB.OrderStatus.PartiallyFilled: 
                    return OrderStatus.PartiallyFilled;

                case IB.OrderStatus.Error: 
                    return OrderStatus.Invalid;

                case IB.OrderStatus.Inactive:
                    Log.Error("InteractiveBrokersBrokerage.ConvertOrderStatus(): Inactive order");
                    return OrderStatus.None;

                case IB.OrderStatus.None: 
                    return OrderStatus.None;
                    
                // not sure how to map these guys
                default:
                    throw new InvalidEnumArgumentException("status", (int)status, typeof(IB.OrderStatus));
            }
        }

        /// <summary>
        /// Maps SecurityType enum
        /// </summary>
        private IB.SecurityType ConvertSecurityType(SecurityType type)
        {
            switch (type)
            {
                case SecurityType.Equity:
                    return IB.SecurityType.Stock;

                case SecurityType.Option:
                    return IB.SecurityType.Option;

                case SecurityType.Commodity:
                    return IB.SecurityType.Commodity;

                case SecurityType.Forex:
                    return IB.SecurityType.Cash;

                case SecurityType.Future:
                    return IB.SecurityType.Future;

                case SecurityType.Base:
                    throw new ArgumentException("InteractiveBrokers does not support SecurityType.Base");

                default:
                    throw new InvalidEnumArgumentException("type", (int)type, typeof(SecurityType));
            }
        }
        /// <summary>
        /// Maps SecurityType enum
        /// </summary>
        private SecurityType ConvertSecurityType(IB.SecurityType type)
        {
            switch (type)
            {
                case IB.SecurityType.Stock:
                    return SecurityType.Equity;

                case IB.SecurityType.Option:
                    return SecurityType.Option;

                case IB.SecurityType.Commodity:
                    return SecurityType.Commodity;

                case IB.SecurityType.Cash:
                    return SecurityType.Forex;

                case IB.SecurityType.Future:
                    return SecurityType.Future;

                // we don't map these security types to anything specific yet, load them as custom data instead of throwing
                case IB.SecurityType.Index:
                case IB.SecurityType.FutureOption:
                case IB.SecurityType.Bag:
                case IB.SecurityType.Bond:
                case IB.SecurityType.Warrant:
                case IB.SecurityType.Bill:
                case IB.SecurityType.Undefined:
                    return SecurityType.Base;

                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        /// <summary>
        /// Handles the threading issues of creating an IB order ID
        /// </summary>
        /// <returns>The new IB ID</returns>
        private int AddInteractiveBrokersOrderID(Order order)
        {
            // spin until we get a next valid id, this should only execute if we create a new instance
            // and immediately try to place an order
            while (_nextValidID == 0) { Thread.Yield(); }

            int id = Interlocked.Increment(ref _nextValidID);
            order.BrokerId.Add(id);
            return id;
        }
    }
}
