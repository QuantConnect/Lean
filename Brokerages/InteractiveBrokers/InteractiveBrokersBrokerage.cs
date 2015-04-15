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
using QuantConnect.Interfaces;
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
        // next valid client id for the gateway/tws
        private static int _nextClientID = 0;
        // next valid request id for queries
        private int _nextRequestID = 0;

        private readonly int _port;
        private readonly string _account;
        private readonly string _host;
        private readonly int _clientID;
        private readonly IOrderIDMapping _orderMapping;
        private readonly IB.IBClient _client;
        private readonly IB.AgentDescription _agentDescription;

        private readonly ManualResetEvent _waitForNextValidID = new ManualResetEvent(false);
        private readonly ManualResetEvent _accountHoldingsResetEvent = new ManualResetEvent(false);

        private readonly ConcurrentDictionary<string, decimal> _cashBalances = new ConcurrentDictionary<string, decimal>(); 
        private readonly ConcurrentDictionary<string, string> _accountProperties = new ConcurrentDictionary<string, string>();
        // number of shares per symbol
        private readonly ConcurrentDictionary<string, Holding> _accountHoldings = new ConcurrentDictionary<string, Holding>();

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
        /// <param name="orderMapping">An instance of IOrderIDMapping used to fetch Order objects by brokerage ID</param>
        public InteractiveBrokersBrokerage(IOrderIDMapping orderMapping)
            : this(
                orderMapping,
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
        /// <param name="orderMapping">An instance of IOrderIDMapping used to fetch Order objects by brokerage ID</param>
        /// <param name="account">The account used to connect to IB</param>
        public InteractiveBrokersBrokerage(IOrderIDMapping orderMapping, string account)
            : this(orderMapping,
                account,
                Config.Get("ib-host", "LOCALHOST"),
                Config.GetInt("ib-port", 4001),
                Config.GetValue("ib-agent-description", IB.AgentDescription.Individual)
                )
        {
        }

        /// <summary>
        /// Creates a new InteractiveBrokersBrokerage from the specified values
        /// </summary>
        /// <param name="orderMapping">An instance of IOrderIDMapping used to fetch Order objects by brokerage ID</param>
        /// <param name="account">The Interactive Brokers account name</param>
        /// <param name="host">host name or IP address of the machine where TWS is running. Leave blank to connect to the local host.</param>
        /// <param name="port">must match the port specified in TWS on the Configure&gt;API&gt;Socket Port field.</param>
        /// <param name="agentDescription">Used for Rule 80A describes the type of trader.</param>
        public InteractiveBrokersBrokerage(IOrderIDMapping orderMapping, string account, string host, int port, IB.AgentDescription agentDescription = IB.AgentDescription.Individual)
            : base("Interactive Brokers Brokerage")
        {
            _orderMapping = orderMapping;
            _account = account;
            _host = host;
            _port = port;
            _clientID = IncrementClientID();
            _agentDescription = agentDescription;
            _client = new IB.IBClient();

            // set up event handlers
            _client.UpdatePortfolio += HandlePortfolioUpdates;
            _client.OrderStatus += HandleOrderStatusUpdates;
            _client.UpdateAccountValue += HandleUpdateAccountValue;
            _client.Error += HandleError;
            _client.AccountDownloadEnd += (sender, args) =>
            {
                Log.Trace("InteractiveBrokersBrokerage.AccounDownloadEnd(): Finished account download for " + args.AccountName);
                _accountHoldingsResetEvent.Set();
            };

            // we need to wait until we receive the next valid id from the server
            _client.NextValidId += (sender, e) =>
            {
                // only grab this id when we initialize, and we'll manually increment it here to avoid threading issues
                if (_nextValidID == 0)
                {
                    _nextValidID = e.OrderId;
                    _waitForNextValidID.Set();
                }
                Log.Trace("InteractiveBrokersBrokerage.HandleNextValidID(): " + e.OrderId);
            };
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
                Log.Trace("InteractiveBrokersBrokerage.PlaceOrder(): Symbol: " + order.Symbol + " Quantity: " + order.Quantity);

                IBPlaceOrder(order, true);
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
                Log.Trace("InteractiveBrokersBrokerage.UpdateOrder(): Symbol: " + order.Symbol + " Quantity: " + order.Quantity + " Status: " + order.Status);

                IBPlaceOrder(order, false);
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

                // canceled order events fired upon confirmation, see HandleError
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
            EventHandler<IB.OpenOrderEventArgs> clientOnOpenOrder = (sender, args) =>
            {
                // convert IB order objects returned from RequestOpenOrders
                orders.Add(ConvertOrder(args.Order, args.Contract));
            };
            EventHandler<EventArgs> clientOnOpenOrderEnd = (sender, args) =>
            {
                // this signals the end of our RequestOpenOrders call
                manualResetEvent.Set();
            };

            _client.OpenOrder += clientOnOpenOrder;
            _client.OpenOrderEnd += clientOnOpenOrderEnd;
            
            _client.RequestOpenOrders();

            // wait for our end signal
            if (!manualResetEvent.WaitOne(5000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetOpenOrders(): Operation took longer than 1 second.");
            }

            // remove our handlers
            _client.OpenOrder -= clientOnOpenOrder;
            _client.OpenOrderEnd -= clientOnOpenOrderEnd;

            return orders;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            return _accountHoldings.Select(x => x.Value.Clone()).ToList();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override Dictionary<string, decimal> GetCashBalance()
        {
            return new Dictionary<string, decimal>(_cashBalances);
        }

        /// <summary>
        /// Gets the execution details matching the filter
        /// </summary>
        /// <returns>A list of executions matching the filter</returns>
        public List<IB.ExecDetailsEventArgs> GetExecutions(string symbol, IB.SecurityType? type, string exchange, DateTime? timeSince, IB.ActionSide? side)
        {
            var filter = new IB.ExecutionFilter
            {
                AcctCode = _account,
                ClientId = _clientID,
                Exchange = exchange,
                SecurityType = type ?? IB.SecurityType.Undefined,
                Symbol = symbol,
                Time = DateTime.MinValue,
                Side = side ?? IB.ActionSide.Undefined
            };

            var details = new List<IB.ExecDetailsEventArgs>();
            using (var client = new IB.IBClient())
            {
                client.Connect(_host, _port, IncrementClientID());

                var manualResetEvent = new ManualResetEvent(false);

                int requestID = Interlocked.Increment(ref _nextRequestID);

                // define our event handlers
                EventHandler<IB.ExecutionDataEndEventArgs> clientOnExecutionDataEnd = (sender, args) =>
                {
                    if (args.RequestId == requestID) manualResetEvent.Set();
                };
                EventHandler<IB.ExecDetailsEventArgs> clientOnExecDetails = (sender, args) =>
                {
                    if (args.RequestId == requestID) details.Add(args);
                };

                client.ExecDetails += clientOnExecDetails;
                client.ExecutionDataEnd += clientOnExecutionDataEnd;

                // no need to be fancy with request id since that's all this client does is 1 request
                client.RequestExecutions(requestID, filter);

                if (!manualResetEvent.WaitOne(5000))
                {
                    throw new TimeoutException("InteractiveBrokersBrokerage.GetExecutions(): Operation took longer than 1 second.");
                }

                // remove our event handlers
                client.ExecDetails -= clientOnExecDetails;
                client.ExecutionDataEnd -= clientOnExecutionDataEnd;
            }

            return details;
        }

        /// <summary>
        /// Connects the client to the IB gateway
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            // we're going to receive fresh values for both of these collections, so clear them
            _accountHoldings.Clear();
            _accountProperties.Clear();

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
            if (!_waitForNextValidID.WaitOne(5000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.Connect(): Operation took longer than 1 second.");
            }

            // wait for our account holdings to have been downloaded
            _client.RequestAccountUpdates(true, _account);
            if (!_accountHoldingsResetEvent.WaitOne(5000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetAccountHoldings(): Operation took longer than 5 seconds.");
            }
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
                _client.Disconnect();
                _client.Dispose();
            }
        }

        /// <summary>
        /// Places the order with InteractiveBrokers
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <param name="needsNewID">Set to true to generate a new order ID, false to leave it alone</param>
        /// <param name="exchange">The exchange to send the order to, defaults to "Smart" to use IB's smart routing</param>
        private void IBPlaceOrder(Order order, bool needsNewID, string exchange = null)
        {
            // connect will throw if it fails
            Connect();

            if (!IsConnected)
            {
                throw new InvalidOperationException("InteractiveBrokersBrokerage.IBPlaceOrder(): Unable to place order while not connected.");
            }

            int ibOrderID = 0;
            if (needsNewID)
            {
                // the order ids are generated for us by the SecurityTransactionManaer
                int id = GetNextBrokerageOrderID();
                order.BrokerId.Add(id);
                ibOrderID = id;
            }
            else if (order.BrokerId.Any())
            {
                // this is *not* perfect code
                ibOrderID = (int)order.BrokerId[0];
            }
            else
            {
                throw new ArgumentException("Expected order with populated BrokerId for updating orders.");
            }

            var contract = CreateContract(order, exchange);
            var ibOrder = ConvertOrder(order, ibOrderID);

            _client.PlaceOrder(ibOrder.OrderId, contract, ibOrder);
        }

        /// <summary>
        /// Handles error messages from IB
        /// </summary>
        private void HandleError(object sender, IB.ErrorEventArgs e)
        {
            Log.Trace(string.Format("InteractiveBrokersBrokerage.HandleError(): Order: {0} ErrorCode: {1} - {2}", e.TickerId, e.ErrorCode, e.ErrorMsg));

            // figure out the message type based on our code collections below
            var brokerageMessageType = BrokerageMessageType.Information;
            if (ErrorCodes.Contains((int) e.ErrorCode))
            {
                brokerageMessageType = BrokerageMessageType.Error;
            }
            else if (WarningCodes.Contains((int) e.ErrorCode))
            {
                brokerageMessageType = BrokerageMessageType.Warning;
            }

            if (InvalidatingCodes.Contains((int)e.ErrorCode))
            {
                Log.Trace(string.Format("InteractiveBrokersBrokerage.HandleError.InvalidateOrder(): Order: {0} ErrorCode: {1} - {2}", e.TickerId, e.ErrorCode, e.ErrorMsg));

                // invalidate the order
                var order = _orderMapping.GetOrderByBrokerageId(e.TickerId);
                var orderEvent = new OrderEvent(order) {Status = OrderStatus.Invalid};
                OnOrderEvent(orderEvent);
            }

            OnMessage(new BrokerageMessageEvent(brokerageMessageType, (int) e.ErrorCode, e.ErrorMsg));
        }

        /// <summary>
        /// Stores all the account values
        /// </summary>
        private void HandleUpdateAccountValue(object sender, IB.UpdateAccountValueEventArgs e)
        {
            //https://www.interactivebrokers.com/en/software/api/apiguide/activex/updateaccountvalue.htm

            try
            {
                _accountProperties[e.Key] = e.Value;

                // we want to capture if the user's cash changes so we can reflect it in the algorithm
                if (e.Key == AccountValueKeys.CashBalance && e.Currency != "BASE")
                {
                    var cashBalance = decimal.Parse(e.Value);
                    _cashBalances.AddOrUpdate(e.Currency, cashBalance);
                    OnAccountChanged(new AccountEvent(e.Currency, cashBalance));
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
                if (update.Status == IB.OrderStatus.PreSubmitted
                 || update.Status == IB.OrderStatus.PendingSubmit)
                {
                    return;
                }

                var status = ConvertOrderStatus(update.Status);
                if (status != OrderStatus.PartiallyFilled &&
                    status != OrderStatus.Filled &&
                    status != OrderStatus.Canceled &&
                    status != OrderStatus.Submitted &&
                    status != OrderStatus.Invalid)
                {
                    Log.Trace("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Status: " + status);
                    return;
                }

                if (status == OrderStatus.Invalid)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): ERROR -- " + update.OrderId);
                }

                var order = _orderMapping.GetOrderByBrokerageId(update.OrderId);
                if (order == null)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Unable to locate order with BrokerageID " + update.OrderId);
                    return;
                }

                // mark sells as negative quantities
                var fillQuantity = order.Direction == OrderDirection.Buy ? update.Filled : -update.Filled;
                var orderEvent = new OrderEvent(order, "Interactive Brokers Fill Event")
                {
                    Status = status,
                    FillPrice = update.AverageFillPrice,
                    FillQuantity = fillQuantity
                };
                OnOrderEvent(orderEvent);
            }
            catch(InvalidOperationException err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Unable to resolve executions for BrokerageID: " + update.OrderId + " - " + err.Message);
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
            Log.Trace("InteractiveBrokersBrokerage.HandlePortfolioUpdates(): Resetting account holdings reset event.");
            _accountHoldingsResetEvent.Reset();
            var holding = CreateHolding(e);
            _accountHoldings[holding.Symbol] = holding;
            OnPortfolioChanged(new SecurityEvent(holding.Symbol, e.Position, e.AverageCost));
        }

        /// <summary>
        /// Converts a QC order to an IB order
        /// </summary>
        private IB.Order ConvertOrder(Order order, int ibOrderID)
        {
            var ibOrder = new IB.Order
            {
                ClientId = _clientID,
                OrderId = ibOrderID,
                Action = ConvertOrderDirection(order.Direction),
                TotalQuantity = Math.Abs(order.Quantity),
                OrderType = ConvertOrderType(order.Type),
                AllOrNone = false,
                Tif = IB.TimeInForce.GoodTillCancel,
                Transmit = true,
                Rule80A = _agentDescription
            };

            var limitOrder = order as LimitOrder;
            var stopMarketOrder = order as StopMarketOrder;
            if (limitOrder != null)
            {
                ibOrder.LimitPrice = limitOrder.LimitPrice;
            }
            else if (stopMarketOrder != null)
            {
                ibOrder.AuxPrice = stopMarketOrder.StopPrice;
            }

            // not yet supported
            //ibOrder.ParentId = 
            //ibOrder.OcaGroup =

            return ibOrder;
        }

        private Order ConvertOrder(IB.Order ibOrder, IB.Contract contract)
        {
            // this function is called by GetOpenOrders which is mainly used by the setup handler to
            // initialize algorithm state.  So the only time we'll be executing this code is when the account
            // has orders sitting and waiting from before algo initialization...
            // because of this we can't get the time accurately

            Order order;
            var mappedSymbol = MapSymbol(contract);
            var orderType = ConvertOrderType(ibOrder.OrderType);
            switch (orderType)
            {
                case OrderType.Market:
                    order = new MarketOrder(mappedSymbol,
                        ibOrder.TotalQuantity,
                        new DateTime() // not sure how to get this data
                        );
                    break;
                case OrderType.Limit:
                    order = new LimitOrder(mappedSymbol,
                        ibOrder.TotalQuantity,
                        ibOrder.LimitPrice,
                        new DateTime()
                        );
                    break;
                case OrderType.StopMarket:
                    order = new StopMarketOrder(mappedSymbol,
                        ibOrder.TotalQuantity,
                        ibOrder.AuxPrice,
                        new DateTime()
                        );
                    break;
                case OrderType.StopLimit:
                    order = new StopLimitOrder(mappedSymbol,
                        ibOrder.TotalQuantity,
                        ibOrder.AuxPrice,
                        ibOrder.LimitPrice,
                        new DateTime()
                        );
                    break;
                default:
                    throw new InvalidEnumArgumentException("orderType", (int) orderType, typeof (OrderType));
            }

            order.SecurityType = ConvertSecurityType(contract.SecurityType);
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
            if (order.SecurityType == SecurityType.Forex)
            {
                // forex is special, so rewrite some of the properties to make it work
                contract.Exchange = "IDEALPRO";
                contract.Symbol = order.Symbol.Substring(0, 3);
                contract.Currency = order.Symbol.Substring(3);
            }
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
        /// Creates a holding object from te UpdatePortfolioEventArgs
        /// </summary>
        private Holding CreateHolding(IB.UpdatePortfolioEventArgs e)
        {
            return new Holding
            {
                Symbol = MapSymbol(e.Contract),
                Type = ConvertSecurityType(e.Contract.SecurityType),
                Quantity = e.Position,
                AveragePrice = e.AverageCost,
                MarketPrice = e.MarketPrice
            };
        }

        /// <summary>
        /// Maps the IB Contract's symbol to a QC symbol
        /// </summary>
        private static string MapSymbol(IB.Contract contract)
        {
            if (contract.SecurityType == IB.SecurityType.Cash)
            {
                // reformat for QC
                return contract.Symbol + contract.Currency;
            }
            return contract.Symbol;
        }

        /// <summary>
        /// Handles the threading issues of creating an IB order ID
        /// </summary>
        /// <returns>The new IB ID</returns>
        private int GetNextBrokerageOrderID()
        {
            // spin until we get a next valid id, this should only execute if we create a new instance
            // and immediately try to place an order
            while (_nextValidID == 0) { Thread.Yield(); }

            return Interlocked.Increment(ref _nextValidID);
        }

        /// <summary>
        /// Increments the client ID for communication with the gateway
        /// </summary>
        private static int IncrementClientID()
        {
            return Interlocked.Increment(ref _nextClientID);
        }

        private static class AccountValueKeys
        {
            public const string CashBalance = "CashBalance";
        }

        // these are fatal errors from IB
        private static readonly HashSet<int> ErrorCodes = new HashSet<int>
        {
            100, 101, 103, 138, 139, 142, 143, 144, 145, 200, 203, 300,301,302,306,308,309,310,311,316,317,320,321,322,323,324,326,327,330,331,332,333,344,346,354,357,365,366,381,384,401,414,431,432,438,501,502,503,504,505,506,507,508,510,511,512,513,514,515,516,517,518,519,520,521,522,523,524,525,526,527,528,529,530,531,10000,10001,10005,10013,10015,10016,10021,10022,10023,10024,10025,10026,10027,1100,1300
        };

        // these are warning messages from IB
        private static readonly HashSet<int> WarningCodes = new HashSet<int>
        {
            102, 104, 105, 106, 107, 109, 110, 111, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 129, 131, 132, 133, 134, 135, 136, 137, 140, 141, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 201, 202, 303,313,314,315,319,325,328,329,334,335,336,337,338,339,340,341,342,343,345,347,348,349,350,352,353,355,356,358,359,360,361,362,363,364,367,368,369,370,371,372,373,374,375,376,377,378,379,380,382,383,385,386,387,388,389,390,391,392,393,394,395,396,397,398,399,400,402,403,404,405,406,407,408,409,410,411,412,413,417,418,419,420,421,422,423,424,425,426,427,428,429,430,433,434,435,436,437,439,440,441,442,443,444,445,446,447,448,449,450,10002,10003,10006,10007,10008,10009,10010,10011,10012,10014,10018,10019,10020,1101,1102,2100,2101,2102,2103,2104,2105,2106,2107,2108,2109,2110
        };

        // these require us to issue invalidated order events
        private static readonly HashSet<int> InvalidatingCodes = new HashSet<int>
        {
            104, 105, 106, 107, 109, 110, 111, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 129, 131, 132, 133, 134, 135, 136, 137, 140, 141, 146, 147, 148, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 167, 168, 201, 202,313,314,315,325,328,329,334,335,336,337,338,339340,341,342,343,345,347,348,349,350,352,353,355,356,358,359,360,361,362,363,364,367,368,369,370,371,372,373,374,375,376,377,378,379,380,382,383,387,388,389,390,391,392,393,394,395,396,397,398,400,401,402,403,404,405,406,407,408,409,410,411,412,413,417,418,419,421,423,424,427,428,429,433,434,435,436,437,439,440,441,442,443,444,445,446,447,448,449,10002,10006,10007,10008,10009,10010,10011,10012,10014,10020,2102
        };
    }
}
