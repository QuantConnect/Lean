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
using System.Linq;
using System.Net;
using System.Threading;
using com.fxcm.external.api.transport;
using com.fxcm.external.api.transport.listeners;
using com.fxcm.external.api.util;
using com.fxcm.fix;
using com.fxcm.fix.pretrade;
using com.fxcm.fix.trade;
using com.fxcm.messaging.util;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.Fxcm
{
    /// <summary>
    /// FXCM brokerage - implementation of IBrokerage interface
    /// </summary>
    [BrokerageFactory(typeof(FxcmBrokerageFactory))]
    public partial class FxcmBrokerage : Brokerage, IDataQueueHandler, IGenericMessageListener, IStatusMessageListener
    {
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;
        private readonly IDataAggregator _aggregator;
        private readonly string _server;
        private readonly string _terminal;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _accountId;

        private Thread _orderEventThread;
        private Thread _connectionMonitorThread;

        private readonly object _lockerConnectionMonitor = new object();
        private DateTime _lastReadyMessageTime;
        private volatile bool _connectionLost;

        // tracks requested order updates, so we can flag Submitted order events as updates
        private readonly ConcurrentDictionary<int, int> _orderUpdates = new ConcurrentDictionary<int, int>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ConcurrentQueue<OrderEvent> _orderEventQueue = new ConcurrentQueue<OrderEvent>();
        private readonly FxcmSymbolMapper _symbolMapper = new FxcmSymbolMapper();
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        private readonly IList<BaseData> _lastHistoryChunk = new List<BaseData>();

        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();

        /// <summary>
        /// Gets/sets a timeout for history requests (in milliseconds)
        /// </summary>
        public int HistoryResponseTimeout { get; set; }

        /// <summary>
        /// Gets/sets the maximum number of retries for a history request
        /// </summary>
        public int MaximumHistoryRetryAttempts { get; set; }

        /// <summary>
        /// Gets/sets a value to enable only history requests to this brokerage
        /// Set to true in parallel downloaders to avoid loading accounts, orders, positions etc. at connect time
        /// </summary>
        public bool EnableOnlyHistoryRequests { get; set; }

        /// <summary>
        /// Static constructor for the <see cref="FxcmBrokerage"/> class
        /// </summary>
        static FxcmBrokerage()
        {
            // FXCM requires TLS 1.2 since 6/16/2019
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FxcmBrokerage"/> class
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        /// <param name="securityProvider">The holdings provider</param>
        /// <param name="aggregator">Consolidate ticks</param>
        /// <param name="server">The url of the server</param>
        /// <param name="terminal">The terminal name</param>
        /// <param name="userName">The user name (login id)</param>
        /// <param name="password">The user password</param>
        /// <param name="accountId">The account id</param>
        public FxcmBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, IDataAggregator aggregator, string server, string terminal, string userName, string password, string accountId)
            : base("FXCM Brokerage")
        {
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
            _aggregator = aggregator;
            _server = server;
            _terminal = terminal;
            _userName = userName;
            _password = password;
            _accountId = accountId;

            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) => Subscribe(s);
            _subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

            HistoryResponseTimeout = 5000;
            MaximumHistoryRetryAttempts = 1;
        }

        #region IBrokerage implementation

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                return _gateway != null && _gateway.isConnected() && !_connectionLost;
            }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            Log.Trace("FxcmBrokerage.Connect()");

            _cancellationTokenSource = new CancellationTokenSource();

            // create new thread to fire order events in queue
            if (!EnableOnlyHistoryRequests)
            {
                _orderEventThread = new Thread(() =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            OrderEvent orderEvent;
                            if (!_orderEventQueue.TryDequeue(out orderEvent))
                            {
                                Thread.Sleep(1);
                                continue;
                            }

                            OnOrderEvent(orderEvent);
                        }
                        catch (Exception exception)
                        {
                            Log.Error(exception);
                        }
                    }
                }) { IsBackground = true };
                _orderEventThread.Start();
                while (!_orderEventThread.IsAlive)
                {
                    Thread.Sleep(1);
                }
            }

            // create the gateway
            _gateway = GatewayFactory.createGateway();

            // register the message listeners with the gateway
            _gateway.registerGenericMessageListener(this);
            _gateway.registerStatusMessageListener(this);

            // create local login properties
            var loginProperties = new FXCMLoginProperties(_userName, _password, _terminal, _server);
            loginProperties.addProperty(IConnectionManager.APP_INFO, "QuantConnect");

            // log in
            try
            {
                _gateway.login(loginProperties);
            }
            catch (Exception err)
            {
                var message =
                    err.Message.Contains("ORA-20101") ? "Incorrect login credentials" :
                    err.Message.Contains("ORA-20003") ? "Contact api@fxcm.com to enable API access, below is a template email. " + Environment.NewLine +
                        "Email: api@fxcm.com " + Environment.NewLine +
                        "Template: " + Environment.NewLine +
                        "Hello FXCM staff, " + Environment.NewLine +
                        "Please enable Java API for all accounts which are associated with this email address. " + Environment.NewLine +
                        "Also, please respond to this email address once Java API has been enabled, letting me know that the change was done successfully." :
                    err.Message;

                _cancellationTokenSource.Cancel();

                throw new BrokerageException(message, err.InnerException);
            }

            // create new thread to manage disconnections and reconnections
            if (!EnableOnlyHistoryRequests)
            {
                _connectionMonitorThread = new Thread(() =>
                {
                    _lastReadyMessageTime = DateTime.UtcNow;

                    try
                    {
                        while (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            TimeSpan elapsed;
                            lock (_lockerConnectionMonitor)
                            {
                                elapsed = DateTime.UtcNow - _lastReadyMessageTime;
                            }

                            if (!_connectionLost && elapsed > TimeSpan.FromSeconds(10))
                            {
                                _connectionLost = true;

                                OnMessage(BrokerageMessageEvent.Disconnected("Connection with FXCM server lost. " +
                                                                             "This could be because of internet connectivity issues. "));
                            }
                            else if (_connectionLost && IsWithinTradingHours())
                            {
                                Log.Trace("FxcmBrokerage.ConnectionMonitorThread(): Attempting reconnection...");

                                try
                                {
                                    // log out
                                    try
                                    {
                                        _gateway.logout();
                                    }
                                    catch (Exception)
                                    {
                                        // ignored
                                    }

                                    // remove the message listeners
                                    _gateway.removeGenericMessageListener(this);
                                    _gateway.removeStatusMessageListener(this);

                                    // register the message listeners with the gateway
                                    _gateway.registerGenericMessageListener(this);
                                    _gateway.registerStatusMessageListener(this);

                                    // log in
                                    _gateway.login(loginProperties);

                                    // load instruments, accounts, orders, positions
                                    LoadInstruments();
                                    if (!EnableOnlyHistoryRequests)
                                    {
                                        LoadAccounts();
                                        LoadOpenOrders();
                                        LoadOpenPositions();
                                    }

                                    _connectionLost = false;

                                    OnMessage(BrokerageMessageEvent.Reconnected("Connection with FXCM server restored."));
                                }
                                catch (Exception exception)
                                {
                                    Log.Trace("FxcmBrokerage.ConnectionMonitorThread(): reconnect failed.");
                                    Log.Error(exception);
                                }
                            }

                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                    }
                }) { IsBackground = true };
                _connectionMonitorThread.Start();
                while (!_connectionMonitorThread.IsAlive)
                {
                    Thread.Sleep(1);
                }
            }

            // load instruments, accounts, orders, positions
            LoadInstruments();
            if (!EnableOnlyHistoryRequests)
            {
                LoadAccounts();
                LoadOpenOrders();
                LoadOpenPositions();
            }
        }

        /// <summary>
        /// Returns true if we are within FXCM trading hours
        /// </summary>
        /// <returns></returns>
        private static bool IsWithinTradingHours()
        {
            var time = DateTime.UtcNow.ConvertFromUtc(TimeZones.EasternStandard);

            // FXCM Trading Hours: http://help.fxcm.com/us/Trading-Basics/New-to-Forex/38757093/What-are-the-Trading-Hours.htm

            return !(time.DayOfWeek == DayOfWeek.Friday && time.TimeOfDay > new TimeSpan(16, 55, 0) ||
                     time.DayOfWeek == DayOfWeek.Saturday ||
                     time.DayOfWeek == DayOfWeek.Sunday && time.TimeOfDay < new TimeSpan(17, 0, 0) ||
                     time.Month == 12 && time.Day == 25 ||
                     time.Month == 1 && time.Day == 1);
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            Log.Trace("FxcmBrokerage.Disconnect()");

            if (_gateway != null)
            {
                // log out
                try
                {
                    if (_gateway.isConnected())
                        _gateway.logout();
                }
                catch (Exception)
                {
                    // ignored
                }

                // remove the message listeners
                _gateway.removeGenericMessageListener(this);
                _gateway.removeStatusMessageListener(this);
            }

            // request and wait for thread to stop
            if (_cancellationTokenSource != null) _cancellationTokenSource.Cancel();

            if (!EnableOnlyHistoryRequests)
            {
                if (_orderEventThread != null) _orderEventThread.Join();
                if (_connectionMonitorThread != null) _connectionMonitorThread.Join();
            }
        }

        /// <summary>
        /// Gets all open orders on the account.
        /// NOTE: The order objects returned do not have QC order IDs.
        /// </summary>
        /// <returns>The open orders returned from FXCM</returns>
        public override List<Order> GetOpenOrders()
        {
            Log.Trace($"FxcmBrokerage.GetOpenOrders(): Located {_openOrders.Count.ToStringInvariant()} orders");
            var orders = _openOrders.Values.ToList()
                .Where(x => OrderIsOpen(x.getFXCMOrdStatus().getCode()))
                .Select(ConvertOrder)
                .ToList();
            return orders;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            Log.Trace("FxcmBrokerage.GetAccountHoldings()");

            // FXCM maintains multiple positions per symbol, so we aggregate them by symbol.
            // The average price for the aggregated position is the quantity weighted average price.
            var holdings = _openPositions.Values
                .Select(ConvertHolding)
                .Where(x => x.Quantity != 0)
                .GroupBy(x => x.Symbol)
                .Select(group => new Holding
                {
                    Symbol = group.Key,
                    Type = group.First().Type,
                    AveragePrice = group.Sum(x => x.AveragePrice * x.Quantity) / group.Sum(x => x.Quantity),
                    CurrencySymbol = group.First().CurrencySymbol,
                    Quantity = group.Sum(x => x.Quantity)
                })
                .ToList();

            // Set MarketPrice in each Holding
            var fxcmSymbols = holdings
                .Select(x => _symbolMapper.GetBrokerageSymbol(x.Symbol))
                .ToList();

            if (fxcmSymbols.Count > 0)
            {
                var quotes = GetQuotes(fxcmSymbols).ToDictionary(x => x.getInstrument().getSymbol());
                foreach (var holding in holdings)
                {
                    MarketDataSnapshot quote;
                    if (quotes.TryGetValue(_symbolMapper.GetBrokerageSymbol(holding.Symbol), out quote))
                    {
                        holding.MarketPrice = Convert.ToDecimal((quote.getBidClose() + quote.getAskClose()) / 2);
                    }
                }
            }

            return holdings;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            Log.Trace("FxcmBrokerage.GetCashBalance()");
            var cashBook = new List<CashAmount>();

            //Adds the account currency to the cashbook.
            cashBook.Add(new CashAmount(Convert.ToDecimal(_accounts[_accountId].getCashOutstanding()),
                _fxcmAccountCurrency));

            // include cash balances from currency swaps for open Forex positions
            foreach (var trade in _openPositions.Values)
            {
                var brokerageSymbol = trade.getInstrument().getSymbol();
                var ticker = FxcmSymbolMapper.ConvertFxcmSymbolToLeanSymbol(brokerageSymbol);
                var securityType = _symbolMapper.GetBrokerageSecurityType(brokerageSymbol);

                if (securityType == SecurityType.Forex)
                {
                    //settlement price for the trade
                    var settlementPrice = Convert.ToDecimal(trade.getSettlPrice());
                    //direction of trade
                    var direction = trade.getPositionQty().getLongQty() > 0 ? 1 : -1;
                    //quantity of the asset
                    var quantity = Convert.ToDecimal(trade.getPositionQty().getQty());
                    //quantity of base currency
                    var baseQuantity = direction * quantity;
                    //quantity of quote currency
                    var quoteQuantity = -direction * quantity * settlementPrice;
                    //base currency
                    var baseCurrency = trade.getCurrency();
                    //quote currency
                    var quoteCurrency = ticker.Substring(ticker.Length - 3);

                    var baseCurrencyAmount = cashBook.FirstOrDefault(x => x.Currency == baseCurrency);
                    //update the value of the base currency
                    if (baseCurrencyAmount != default(CashAmount))
                    {
                        cashBook.Remove(baseCurrencyAmount);
                        cashBook.Add(new CashAmount(baseQuantity + baseCurrencyAmount.Amount, baseCurrency));
                    }
                    else
                    {
                        //add the base currency if not present
                        cashBook.Add(new CashAmount(baseQuantity, baseCurrency));
                    }

                    var quoteCurrencyAmount = cashBook.Find(x => x.Currency == quoteCurrency);
                    //update the value of the quote currency
                    if (quoteCurrencyAmount != default(CashAmount))
                    {
                        cashBook.Remove(quoteCurrencyAmount);
                        cashBook.Add(new CashAmount(quoteQuantity + quoteCurrencyAmount.Amount, quoteCurrency));
                    }
                    else
                    {
                        //add the quote currency if not present
                        cashBook.Add(new CashAmount(quoteQuantity, quoteCurrency));
                    }
                }
            }

            return cashBook;
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            Log.Trace("FxcmBrokerage.PlaceOrder(): {0}", order);

            if (!IsConnected)
                throw new InvalidOperationException("FxcmBrokerage.PlaceOrder(): Unable to place order while not connected.");

            if (order.Direction != OrderDirection.Buy && order.Direction != OrderDirection.Sell)
                throw new ArgumentException("FxcmBrokerage.PlaceOrder(): Invalid Order Direction");

            var fxcmSymbol = _symbolMapper.GetBrokerageSymbol(order.Symbol);
            var orderSide = order.Direction == OrderDirection.Buy ? SideFactory.BUY : SideFactory.SELL;
            var quantity = (double)order.AbsoluteQuantity;

            OrderSingle orderRequest;
            switch (order.Type)
            {
                case OrderType.Market:
                    orderRequest = MessageGenerator.generateMarketOrder(_accountId, quantity, orderSide, fxcmSymbol, "");
                    break;

                case OrderType.Limit:
                    var limitPrice = (double)((LimitOrder)order).LimitPrice;
                    orderRequest = MessageGenerator.generateOpenOrder(limitPrice, _accountId, quantity, orderSide, fxcmSymbol, "");
                    orderRequest.setOrdType(OrdTypeFactory.LIMIT);
                    orderRequest.setTimeInForce(TimeInForceFactory.GOOD_TILL_CANCEL);
                    break;

                case OrderType.StopMarket:
                    var stopPrice = (double)((StopMarketOrder)order).StopPrice;
                    orderRequest = MessageGenerator.generateOpenOrder(stopPrice, _accountId, quantity, orderSide, fxcmSymbol, "");
                    orderRequest.setOrdType(OrdTypeFactory.STOP);
                    orderRequest.setTimeInForce(TimeInForceFactory.GOOD_TILL_CANCEL);
                    break;

                default:
                    throw new NotSupportedException("FxcmBrokerage.PlaceOrder(): Order type " + order.Type + " is not supported.");
            }

            _isOrderSubmitRejected = false;
            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.sendMessage(orderRequest);
                _mapRequestsToOrders[_currentRequest] = order;
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException("FxcmBrokerage.PlaceOrder(): Operation took longer than " +
                    $"{((decimal) ResponseTimeout / 1000).ToStringInvariant()} seconds."
                );

            return !_isOrderSubmitRejected;
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            Log.Trace("FxcmBrokerage.UpdateOrder(): {0}", order);

            if (!IsConnected)
                throw new InvalidOperationException("FxcmBrokerage.UpdateOrder(): Unable to update order while not connected.");

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform an update
                Log.Trace("FxcmBrokerage.UpdateOrder(): Unable to update order without BrokerId.");
                return false;
            }

            var fxcmOrderId = order.BrokerId[0].ToStringInvariant();

            ExecutionReport fxcmOrder;
            if (!_openOrders.TryGetValue(fxcmOrderId, out fxcmOrder))
                throw new ArgumentException("FxcmBrokerage.UpdateOrder(): FXCM order id not found: " + fxcmOrderId);

            double price;
            switch (order.Type)
            {
                case OrderType.Limit:
                    price = (double)((LimitOrder)order).LimitPrice;
                    break;

                case OrderType.StopMarket:
                    price = (double)((StopMarketOrder)order).StopPrice;
                    break;

                default:
                    throw new NotSupportedException("FxcmBrokerage.UpdateOrder(): Invalid order type.");
            }

            _isOrderUpdateOrCancelRejected = false;
            var orderReplaceRequest = MessageGenerator.generateOrderReplaceRequest("", fxcmOrder.getOrderID(), fxcmOrder.getSide(), fxcmOrder.getOrdType(), price, fxcmOrder.getAccount());
            orderReplaceRequest.setInstrument(fxcmOrder.getInstrument());
            orderReplaceRequest.setOrderQty((double)order.AbsoluteQuantity);

            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _orderUpdates[order.Id] = order.Id;
                _currentRequest = _gateway.sendMessage(orderReplaceRequest);
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException("FxcmBrokerage.UpdateOrder(): Operation took longer than " +
                    $"{((decimal) ResponseTimeout / 1000).ToStringInvariant()} seconds."
                );

            return !_isOrderUpdateOrCancelRejected;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace("FxcmBrokerage.CancelOrder(): {0}", order);

            if (!IsConnected)
                throw new InvalidOperationException("FxcmBrokerage.UpdateOrder(): Unable to cancel order while not connected.");

            if (!order.BrokerId.Any())
            {
                // we need the brokerage order id in order to perform a cancellation
                Log.Trace("FxcmBrokerage.CancelOrder(): Unable to cancel order without BrokerId.");
                return false;
            }

            var fxcmOrderId = order.BrokerId[0].ToStringInvariant();

            ExecutionReport fxcmOrder;
            if (!_openOrders.TryGetValue(fxcmOrderId, out fxcmOrder))
                throw new ArgumentException("FxcmBrokerage.CancelOrder(): FXCM order id not found: " + fxcmOrderId);

            _isOrderUpdateOrCancelRejected = false;
            var orderCancelRequest = MessageGenerator.generateOrderCancelRequest("", fxcmOrder.getOrderID(), fxcmOrder.getSide(), fxcmOrder.getAccount());
            AutoResetEvent autoResetEvent;
            lock (_locker)
            {
                _currentRequest = _gateway.sendMessage(orderCancelRequest);
                autoResetEvent = new AutoResetEvent(false);
                _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
            }
            if (!autoResetEvent.WaitOne(ResponseTimeout))
                throw new TimeoutException("FxcmBrokerage.CancelOrder(): Operation took longer than " +
                    $"{((decimal) ResponseTimeout / 1000).ToStringInvariant()} seconds."
                );

            return !_isOrderUpdateOrCancelRejected;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            if (!_symbolMapper.IsKnownLeanSymbol(request.Symbol))
            {
                Log.Trace("FxcmBrokerage.GetHistory(): Invalid symbol: {0}, no history returned", request.Symbol.Value);
                yield break;
            }

            // cache exchange time zone for symbol
            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(request.Symbol, out exchangeTimeZone))
            {
                exchangeTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.FXCM, request.Symbol, request.Symbol.SecurityType).TimeZone;
                _symbolExchangeTimeZones.Add(request.Symbol, exchangeTimeZone);
            }

            var interval = ToFxcmInterval(request.Resolution);

            // download data
            var history = new List<BaseData>();
            var lastEndTime = DateTime.MinValue;

            var end = request.EndTimeUtc;

            var attempt = 1;
            while (end > request.StartTimeUtc)
            {
                Log.Debug($"FxcmBrokerage.GetHistory(): Requesting {end.ToIso8601Invariant()} to {request.StartTimeUtc.ToIso8601Invariant()}");
                _lastHistoryChunk.Clear();

                var mdr = new MarketDataRequest();
                mdr.setSubscriptionRequestType(SubscriptionRequestTypeFactory.SNAPSHOT);
                mdr.setResponseFormat(IFixMsgTypeDefs.__Fields.MSGTYPE_FXCMRESPONSE);
                mdr.setFXCMTimingInterval(interval);
                mdr.setMDEntryTypeSet(MarketDataRequest.MDENTRYTYPESET_ALL);

                mdr.setFXCMStartDate(new UTCDate(ToJavaDateUtc(request.StartTimeUtc)));
                mdr.setFXCMStartTime(new UTCTimeOnly(ToJavaDateUtc(request.StartTimeUtc)));
                mdr.setFXCMEndDate(new UTCDate(ToJavaDateUtc(end)));
                mdr.setFXCMEndTime(new UTCTimeOnly(ToJavaDateUtc(end)));
                mdr.addRelatedSymbol(_fxcmInstruments[_symbolMapper.GetBrokerageSymbol(request.Symbol)]);

                AutoResetEvent autoResetEvent;
                lock (_locker)
                {
                    _currentRequest = _gateway.sendMessage(mdr);
                    autoResetEvent = new AutoResetEvent(false);
                    _mapRequestsToAutoResetEvents[_currentRequest] = autoResetEvent;
                    _pendingHistoryRequests.Add(_currentRequest);
                }

                if (!autoResetEvent.WaitOne(HistoryResponseTimeout))
                {
                    // No response can mean genuine timeout or the history data has ended.

                    // 90% of the time no response because no data; widen the search net to 5m if we don't get a response:
                    if (request.StartTimeUtc.AddSeconds(300) >= end)
                    {
                        break;
                    }

                    // 5% of the time its because the data ends at a specific, repeatible time not close to our desired endtime:
                    if (end == lastEndTime)
                    {
                        Log.Trace("FxcmBrokerage.GetHistory(): Request for {0} ended at {1:O}", request.Symbol.Value, end);
                        break;
                    }

                    // 5% of the time its because of an internet / time of day / api settings / timeout: throw if this is the *second* attempt.
                    if (EnableOnlyHistoryRequests && lastEndTime != DateTime.MinValue)
                    {
                        throw new TimeoutException("FxcmBrokerage.GetHistory(): History operation ending in {end:O} took longer than " +
                            $"{((decimal) HistoryResponseTimeout / 1000).ToStringInvariant()} seconds. This may be because there is no data, retrying..."
                        );
                    }

                    // Assuming Timeout: If we've already retried quite a few times, lets bail.
                    if (++attempt > MaximumHistoryRetryAttempts)
                    {
                        Log.Trace("FxcmBrokerage.GetHistory(): Maximum attempts reached for: " + request.Symbol.Value);
                        break;
                    }

                    // Assuming Timeout: Save end time and if have the same endtime next time, break since its likely there's no data after that time.
                    lastEndTime = end;
                    Log.Trace($"FxcmBrokerage.GetHistory(): Attempt {attempt.ToStringInvariant()} for: " +
                        $"{request.Symbol.Value} ended at {lastEndTime.ToIso8601Invariant()}"
                    );
                    continue;
                }

                // Add data
                lock (_locker)
                {
                    history.InsertRange(0, _lastHistoryChunk);
                }

                var firstDateUtc = _lastHistoryChunk[0].Time.ConvertToUtc(exchangeTimeZone);
                if (end != firstDateUtc)
                {
                    // new end date = first datapoint date.
                    end = request.Resolution == Resolution.Tick ? firstDateUtc.AddMilliseconds(-1) : firstDateUtc.AddSeconds(-1);

                    if (request.StartTimeUtc.AddSeconds(10) >= end)
                        break;
                }
                else
                {
                    break;
                }
            }

            foreach (var data in history)
            {
                yield return data;
            }
        }

        #endregion

    }
}
