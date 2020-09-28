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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using Order = QuantConnect.Orders.Order;
using IB = QuantConnect.Brokerages.InteractiveBrokers.Client;
using IBApi;
using NodaTime;
using QuantConnect.IBAutomater;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities.Option;
using Bar = QuantConnect.Data.Market.Bar;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// The Interactive Brokers brokerage
    /// </summary>
    [BrokerageFactory(typeof(InteractiveBrokersBrokerageFactory))]
    public sealed class InteractiveBrokersBrokerage : Brokerage, IDataQueueHandler, IDataQueueUniverseProvider
    {
        private readonly IBAutomater.IBAutomater _ibAutomater;

        // Existing orders created in TWS can *only* be cancelled/modified when connected with ClientId = 0
        private const int ClientId = 0;

        // next valid order id for this client
        private int _nextValidId;
        private readonly object _nextValidIdLocker = new object();

        // next valid request id for queries
        private int _nextRequestId;
        private int _nextTickerId;

        private readonly int _port;
        private readonly string _account;
        private readonly string _host;
        private readonly IAlgorithm _algorithm;
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;
        private readonly IDataAggregator _aggregator;
        private readonly IB.InteractiveBrokersClient _client;
        private readonly string _agentDescription;
        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        private Thread _messageProcessingThread;

        // Notifies the thread reading information from Gateway/TWS whenever there are messages ready to be consumed
        private readonly EReaderSignal _signal = new EReaderMonitorSignal();

        private readonly ManualResetEvent _waitForNextValidId = new ManualResetEvent(false);
        private readonly ManualResetEvent _accountHoldingsResetEvent = new ManualResetEvent(false);
        private Exception _accountHoldingsLastException;

        // tracks requested order updates, so we can flag Submitted order events as updates
        private readonly ConcurrentDictionary<int, int> _orderUpdates = new ConcurrentDictionary<int, int>();
        // tracks executions before commission reports, map: execId -> execution
        private readonly ConcurrentDictionary<string, Execution> _orderExecutions = new ConcurrentDictionary<string, Execution>();
        // tracks commission reports before executions, map: execId -> commission report
        private readonly ConcurrentDictionary<string, CommissionReport> _commissionReports = new ConcurrentDictionary<string, CommissionReport>();

        // holds account properties, cash balances and holdings for the account
        private readonly InteractiveBrokersAccountData _accountData = new InteractiveBrokersAccountData();

        // holds brokerage state information (connection status, error conditions, etc.)
        private readonly InteractiveBrokersStateManager _stateManager = new InteractiveBrokersStateManager();

        private readonly object _sync = new object();

        private readonly ConcurrentDictionary<string, ContractDetails> _contractDetails = new ConcurrentDictionary<string, ContractDetails>();

        private readonly InteractiveBrokersSymbolMapper _symbolMapper = new InteractiveBrokersSymbolMapper();

        // Prioritized list of exchanges used to find right futures contract
        private readonly Dictionary<string, string> _futuresExchanges = new Dictionary<string, string>
        {
            { Market.CME, "GLOBEX" },
            { Market.NYMEX, "NYMEX" },
            { Market.COMEX, "NYMEX" },
            { Market.CBOT, "ECBOT" },
            { Market.ICE, "NYBOT" },
            { Market.CBOE, "CFE" }
        };

        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

        // exchange time zones by symbol
        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();

        // IB requests made through the IB-API must be limited to a maximum of 50 messages/second
        private readonly RateGate _messagingRateLimiter = new RateGate(50, TimeSpan.FromSeconds(1));

        // additional IB request information, will be matched with errors in the handler, for better error reporting
        private readonly ConcurrentDictionary<int, string> _requestInformation = new ConcurrentDictionary<int, string>();

        // when unsubscribing symbols immediately after subscribing IB returns an error (Can't find EId with tickerId:nnn),
        // so we track subscription times to ensure symbols are not unsubscribed before a minimum time span has elapsed
        private readonly Dictionary<int, DateTime> _subscriptionTimes = new Dictionary<int, DateTime>();
        private readonly TimeSpan _minimumTimespanBeforeUnsubscribe = TimeSpan.FromMilliseconds(500);

        private readonly bool _enableDelayedStreamingData = Config.GetBool("ib-enable-delayed-streaming-data");

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected => _client != null && _client.Connected && !_stateManager.Disconnected1100Fired;

        /// <summary>
        /// Returns true if the connected user is a financial advisor or non-disclosed broker
        /// </summary>
        public bool IsFinancialAdvisor => IsMasterAccount(_account);

        /// <summary>
        /// Returns true if the account is a financial advisor or non-disclosed broker master account
        /// </summary>
        /// <param name="account">The account code</param>
        /// <returns>True if the account is a master account</returns>
        public static bool IsMasterAccount(string account)
        {
            return account.Contains("F") || account.Contains("I");
        }

        /// <summary>
        /// Creates a new InteractiveBrokersBrokerage using values from configuration:
        ///     ib-account (required)
        ///     ib-host (optional, defaults to LOCALHOST)
        ///     ib-port (optional, defaults to 4001)
        ///     ib-agent-description (optional, defaults to Individual)
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="orderProvider">An instance of IOrderProvider used to fetch Order objects by brokerage ID</param>
        /// <param name="securityProvider">The security provider used to give access to algorithm securities</param>
        /// <param name="aggregator">consolidate ticks</param>
        public InteractiveBrokersBrokerage(IAlgorithm algorithm, IOrderProvider orderProvider, ISecurityProvider securityProvider, IDataAggregator aggregator)
            : this(
                algorithm,
                orderProvider,
                securityProvider,
                aggregator,
                Config.Get("ib-account"),
                Config.Get("ib-host", "LOCALHOST"),
                Config.GetInt("ib-port", 4001),
                Config.Get("ib-tws-dir"),
                Config.Get("ib-version", "974"),
                Config.Get("ib-user-name"),
                Config.Get("ib-password"),
                Config.Get("ib-trading-mode"),
                Config.GetValue("ib-agent-description", IB.AgentDescription.Individual)
                )
        {
        }

        /// <summary>
        /// Creates a new InteractiveBrokersBrokerage for the specified account
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="orderProvider">An instance of IOrderProvider used to fetch Order objects by brokerage ID</param>
        /// <param name="securityProvider">The security provider used to give access to algorithm securities</param>
        /// <param name="account">The account used to connect to IB</param>
        public InteractiveBrokersBrokerage(IAlgorithm algorithm, IOrderProvider orderProvider, ISecurityProvider securityProvider, IDataAggregator aggregator, string account)
            : this(
                algorithm,
                orderProvider,
                securityProvider,
                aggregator,
                account,
                Config.Get("ib-host", "LOCALHOST"),
                Config.GetInt("ib-port", 4001),
                Config.Get("ib-tws-dir"),
                Config.Get("ib-version", "974"),
                Config.Get("ib-user-name"),
                Config.Get("ib-password"),
                Config.Get("ib-trading-mode"),
                Config.GetValue("ib-agent-description", IB.AgentDescription.Individual)
                )
        {
        }

        /// <summary>
        /// Creates a new InteractiveBrokersBrokerage from the specified values
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="orderProvider">An instance of IOrderProvider used to fetch Order objects by brokerage ID</param>
        /// <param name="securityProvider">The security provider used to give access to algorithm securities</param>
        /// <param name="aggregator">consolidate ticks</param>
        /// <param name="account">The Interactive Brokers account name</param>
        /// <param name="host">host name or IP address of the machine where TWS is running. Leave blank to connect to the local host.</param>
        /// <param name="port">must match the port specified in TWS on the Configure&gt;API&gt;Socket Port field.</param>
        /// <param name="ibDirectory">The IB Gateway root directory</param>
        /// <param name="ibVersion">The IB Gateway version</param>
        /// <param name="userName">The login user name</param>
        /// <param name="password">The login password</param>
        /// <param name="tradingMode">The trading mode: 'live' or 'paper'</param>
        /// <param name="agentDescription">Used for Rule 80A describes the type of trader.</param>
        public InteractiveBrokersBrokerage(
            IAlgorithm algorithm,
            IOrderProvider orderProvider,
            ISecurityProvider securityProvider,
            IDataAggregator aggregator,
            string account,
            string host,
            int port,
            string ibDirectory,
            string ibVersion,
            string userName,
            string password,
            string tradingMode,
            string agentDescription = IB.AgentDescription.Individual)
            : base("Interactive Brokers Brokerage")
        {
            _algorithm = algorithm;
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
            _aggregator = aggregator;
            _account = account;
            _host = host;
            _port = port;
            _agentDescription = agentDescription;

            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += (s, t) => Subscribe(s);
            _subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

            Log.Trace("InteractiveBrokersBrokerage.InteractiveBrokersBrokerage(): Starting IB Automater...");

            // start IB Gateway
            _ibAutomater = new IBAutomater.IBAutomater(ibDirectory, ibVersion, userName, password, tradingMode, port);
            _ibAutomater.OutputDataReceived += OnIbAutomaterOutputDataReceived;
            _ibAutomater.ErrorDataReceived += OnIbAutomaterErrorDataReceived;
            _ibAutomater.Exited += OnIbAutomaterExited;

            CheckIbAutomaterError(_ibAutomater.Start(false));

            Log.Trace($"InteractiveBrokersBrokerage.InteractiveBrokersBrokerage(): Host: {host}, Port: {port}, Account: {account}, AgentDescription: {agentDescription}");

            _client = new IB.InteractiveBrokersClient(_signal);

            // set up event handlers
            _client.UpdatePortfolio += HandlePortfolioUpdates;
            _client.OrderStatus += HandleOrderStatusUpdates;
            _client.OpenOrder += HandleOpenOrder;
            _client.OpenOrderEnd += HandleOpenOrderEnd;
            _client.UpdateAccountValue += HandleUpdateAccountValue;
            _client.ExecutionDetails += HandleExecutionDetails;
            _client.CommissionReport += HandleCommissionReport;
            _client.Error += HandleError;
            _client.TickPrice += HandleTickPrice;
            _client.TickSize += HandleTickSize;
            _client.CurrentTimeUtc += HandleBrokerTime;

            // we need to wait until we receive the next valid id from the server
            _client.NextValidId += (sender, e) =>
            {
                lock (_nextValidIdLocker)
                {
                    Log.Trace($"InteractiveBrokersBrokerage.HandleNextValidID(): updating nextValidId from {_nextValidId} to {e.OrderId}");

                    _nextValidId = e.OrderId;
                    _waitForNextValidId.Set();
                }
            };

            _client.ConnectAck += (sender, e) =>
            {
                Log.Trace("InteractiveBrokersBrokerage.HandleConnectAck(): API client connected.");
            };

            _client.ConnectionClosed += (sender, e) =>
            {
                Log.Trace("InteractiveBrokersBrokerage.HandleConnectionClosed(): API client disconnected.");
            };
        }

        /// <summary>
        /// Provides public access to the underlying IBClient instance
        /// </summary>
        public IB.InteractiveBrokersClient Client => _client;

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            try
            {
                Log.Trace("InteractiveBrokersBrokerage.PlaceOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

                if (!IsConnected)
                {
                    OnMessage(
                        new BrokerageMessageEvent(
                            BrokerageMessageType.Warning,
                            "PlaceOrderWhenDisconnected",
                            "Orders cannot be submitted when disconnected."));
                    return false;
                }

                IBPlaceOrder(order, true);
                return true;
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.PlaceOrder(): " + err);
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
                Log.Trace("InteractiveBrokersBrokerage.UpdateOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity + " Status: " + order.Status);

                if (!IsConnected)
                {
                    OnMessage(
                        new BrokerageMessageEvent(
                            BrokerageMessageType.Warning,
                            "UpdateOrderWhenDisconnected",
                            "Orders cannot be updated when disconnected."));
                    return false;
                }

                _orderUpdates[order.Id] = order.Id;
                IBPlaceOrder(order, false);
            }
            catch (Exception err)
            {
                int id;
                _orderUpdates.TryRemove(order.Id, out id);
                Log.Error("InteractiveBrokersBrokerage.UpdateOrder(): " + err);
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
                Log.Trace("InteractiveBrokersBrokerage.CancelOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

                if (!IsConnected)
                {
                    OnMessage(
                        new BrokerageMessageEvent(
                            BrokerageMessageType.Warning,
                            "CancelOrderWhenDisconnected",
                            "Orders cannot be cancelled when disconnected."));
                    return false;
                }

                // this could be better
                foreach (var id in order.BrokerId)
                {
                    var orderId = Parse.Int(id);

                    _requestInformation[orderId] = "CancelOrder: " + order;

                    CheckRateLimiting();

                    _client.ClientSocket.cancelOrder(orderId);
                }

                // canceled order events fired upon confirmation, see HandleError
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.CancelOrder(): OrderID: " + order.Id + " - " + err);
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
            // If client 0 invokes reqOpenOrders, it will cause currently open orders placed from TWS manually to be 'bound',
            // i.e. assigned an order ID so that they can be modified or cancelled by the API client 0.
            GetOpenOrdersInternal(false);

            // return all open orders (including those placed from TWS, which will have a negative order id)
            lock (_nextValidIdLocker)
            {
                return GetOpenOrdersInternal(true);
            }
        }

        private List<Order> GetOpenOrdersInternal(bool all)
        {
            var orders = new List<Order>();

            var manualResetEvent = new ManualResetEvent(false);

            Exception exception = null;
            var lastOrderId = 0;

            // define our handlers
            EventHandler<IB.OpenOrderEventArgs> clientOnOpenOrder = (sender, args) =>
            {
                try
                {
                    if (args.OrderId > lastOrderId)
                    {
                        lastOrderId = args.OrderId;
                    }

                    // convert IB order objects returned from RequestOpenOrders
                    orders.Add(ConvertOrder(args.Order, args.Contract));
                }
                catch (Exception e)
                {
                    exception = e;
                }
            };
            EventHandler clientOnOpenOrderEnd = (sender, args) =>
            {
                // this signals the end of our RequestOpenOrders call
                manualResetEvent.Set();
            };

            _client.OpenOrder += clientOnOpenOrder;
            _client.OpenOrderEnd += clientOnOpenOrderEnd;

            CheckRateLimiting();

            if (all)
            {
                _client.ClientSocket.reqAllOpenOrders();
            }
            else
            {
                _client.ClientSocket.reqOpenOrders();
            }

            // wait for our end signal
            var timedOut = !manualResetEvent.WaitOne(15000);

            // remove our handlers
            _client.OpenOrder -= clientOnOpenOrder;
            _client.OpenOrderEnd -= clientOnOpenOrderEnd;

            if (exception != null)
            {
                throw new Exception("InteractiveBrokersBrokerage.GetOpenOrders(): ", exception);
            }

            if (timedOut)
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetOpenOrders(): Operation took longer than 15 seconds.");
            }

            if (all)
            {
                // https://interactivebrokers.github.io/tws-api/order_submission.html
                // if the function reqAllOpenOrders is used by a client, subsequent orders placed by that client
                // must have order IDs greater than the order IDs of all orders returned because of that function call.

                if (lastOrderId >= _nextValidId)
                {
                    Log.Trace($"InteractiveBrokersBrokerage.GetOpenOrders(): Updating nextValidId from {_nextValidId} to {lastOrderId + 1}");
                    _nextValidId = lastOrderId + 1;
                }
            }

            return orders;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            if (!IsConnected)
            {
                Log.Trace("InteractiveBrokersBrokerage.GetAccountHoldings(): not connected, connecting now");
                Connect();
            }

            var utcNow = DateTime.UtcNow;
            var holdings = new List<Holding>();

            foreach (var kvp in _accountData.AccountHoldings)
            {
                var holding = ObjectActivator.Clone(kvp.Value);

                if (holding.Quantity != 0)
                {
                    if (OptionSymbol.IsOptionContractExpired(holding.Symbol, utcNow))
                    {
                        OnMessage(
                            new BrokerageMessageEvent(
                                BrokerageMessageType.Warning,
                                "ExpiredOptionHolding",
                                $"The option holding for [{holding.Symbol.Value}] is expired and will be excluded from the account holdings."));

                        continue;
                    }

                    holdings.Add(holding);
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
            if (!IsConnected)
            {
                if (_ibAutomater.IsWithinScheduledServerResetTimes())
                {
                    // Occasionally the disconnection due to the IB reset period might last
                    // much longer than expected during weekends (even up to the cash sync time).
                    // In this case we do not try to reconnect (since this would fail anyway)
                    // but we return the existing balances instead.
                    Log.Trace("InteractiveBrokersBrokerage.GetCashBalance(): not connected within reset times, returning existing balances");
                }
                else
                {
                    Log.Trace("InteractiveBrokersBrokerage.GetCashBalance(): not connected, connecting now");
                    Connect();
                }
            }

            var balances = _accountData.CashBalances.Select(x => new CashAmount(x.Value, x.Key)).ToList();

            if (balances.Count == 0)
            {
                Log.Trace($"InteractiveBrokersBrokerage.GetCashBalance(): no balances found, IsConnected: {IsConnected}, _disconnected1100Fired: {_stateManager.Disconnected1100Fired}");
            }

            return balances;
        }

        /// <summary>
        /// Gets the execution details matching the filter
        /// </summary>
        /// <returns>A list of executions matching the filter</returns>
        public List<IB.ExecutionDetailsEventArgs> GetExecutions(string symbol, string type, string exchange, DateTime? timeSince, string side)
        {
            var filter = new ExecutionFilter
            {
                AcctCode = _account,
                ClientId = ClientId,
                Exchange = exchange,
                SecType = type ?? IB.SecurityType.Undefined,
                Symbol = symbol,
                Time = (timeSince ?? DateTime.MinValue).ToStringInvariant("yyyyMMdd HH:mm:ss"),
                Side = side ?? IB.ActionSide.Undefined
            };

            var details = new List<IB.ExecutionDetailsEventArgs>();

            var manualResetEvent = new ManualResetEvent(false);

            var requestId = GetNextRequestId();

            _requestInformation[requestId] = "GetExecutions: " + symbol;

            // define our event handlers
            EventHandler<IB.RequestEndEventArgs> clientOnExecutionDataEnd = (sender, args) =>
            {
                if (args.RequestId == requestId) manualResetEvent.Set();
            };
            EventHandler<IB.ExecutionDetailsEventArgs> clientOnExecDetails = (sender, args) =>
            {
                if (args.RequestId == requestId) details.Add(args);
            };

            _client.ExecutionDetails += clientOnExecDetails;
            _client.ExecutionDetailsEnd += clientOnExecutionDataEnd;

            CheckRateLimiting();

            // no need to be fancy with request id since that's all this client does is 1 request
            _client.ClientSocket.reqExecutions(requestId, filter);

            if (!manualResetEvent.WaitOne(5000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetExecutions(): Operation took longer than 5 seconds.");
            }

            // remove our event handlers
            _client.ExecutionDetails -= clientOnExecDetails;
            _client.ExecutionDetailsEnd -= clientOnExecutionDataEnd;

            return details;
        }

        /// <summary>
        /// Connects the client to the IB gateway
        /// </summary>
        public override void Connect()
        {
            if (IsConnected) return;

            // we're going to receive fresh values for all account data, so we clear all
            _accountData.Clear();

            var attempt = 1;
            const int maxAttempts = 5;

            var subscribedSymbolsCount = _subscribedSymbols.Skip(0).Count();
            if (subscribedSymbolsCount > 0)
            {
                Log.Trace($"InteractiveBrokersBrokerage.Connect(): Data subscription count {subscribedSymbolsCount}, restoring data subscriptions is required");
            }

            while (true)
            {
                try
                {
                    Log.Trace("InteractiveBrokersBrokerage.Connect(): Attempting to connect ({0}/{1}) ...", attempt, maxAttempts);

                    // if message processing thread is still running, wait until it terminates
                    Disconnect();

                    // At initial startup or after a gateway restart, we need to wait for the gateway to be ready for a connect request.
                    // Attempting to connect to the socket too early will get a SocketException: Connection refused.
                    if (attempt == 1)
                    {
                        Thread.Sleep(2500);
                    }

                    // we're going to try and connect several times, if successful break
                    _client.ClientSocket.eConnect(_host, _port, ClientId);

                    // create the message processing thread
                    var reader = new EReader(_client.ClientSocket, _signal);
                    reader.Start();

                    _messageProcessingThread = new Thread(() =>
                    {
                        Log.Trace("InteractiveBrokersBrokerage.Connect(): IB message processing thread started: #" + Thread.CurrentThread.ManagedThreadId);

                        while (_client.ClientSocket.IsConnected())
                        {
                            try
                            {
                                _signal.waitForSignal();
                                reader.processMsgs();
                            }
                            catch (Exception error)
                            {
                                // error in message processing thread, log error and disconnect
                                Log.Error("InteractiveBrokersBrokerage.Connect(): Error in message processing thread #" + Thread.CurrentThread.ManagedThreadId + ": " + error);
                            }
                        }

                        Log.Trace("InteractiveBrokersBrokerage.Connect(): IB message processing thread ended: #" + Thread.CurrentThread.ManagedThreadId);
                    })
                    { IsBackground = true };

                    _messageProcessingThread.Start();

                    // pause for a moment to receive next valid ID message from gateway
                    if (!_waitForNextValidId.WaitOne(15000))
                    {
                        Log.Trace("InteractiveBrokersBrokerage.Connect(): Operation took longer than 15 seconds.");

                        // no response, disconnect and retry
                        Disconnect();

                        // max out at 5 attempts to connect ~1 minute
                        if (attempt++ < maxAttempts)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        throw new TimeoutException("InteractiveBrokersBrokerage.Connect(): Operation took longer than 15 seconds.");
                    }

                    Log.Trace("InteractiveBrokersBrokerage.Connect(): IB next valid id received.");

                    if (!_client.Connected) throw new Exception("InteractiveBrokersBrokerage.Connect(): Connection returned but was not in connected state.");

                    if (IsFinancialAdvisor)
                    {
                        if (!DownloadFinancialAdvisorAccount(_account))
                        {
                            Log.Trace("InteractiveBrokersBrokerage.Connect(): DownloadFinancialAdvisorAccount failed.");

                            Disconnect();

                            if (_accountHoldingsLastException != null)
                            {
                                // if an exception was thrown during account download, do not retry but exit immediately
                                attempt = maxAttempts;
                                throw new Exception(_accountHoldingsLastException.Message, _accountHoldingsLastException);
                            }

                            if (attempt++ < maxAttempts)
                            {
                                Thread.Sleep(1000);
                                continue;
                            }

                            throw new TimeoutException("InteractiveBrokersBrokerage.Connect(): DownloadFinancialAdvisorAccount failed.");
                        }
                    }
                    else
                    {
                        if (!DownloadAccount(_account))
                        {
                            Log.Trace("InteractiveBrokersBrokerage.Connect(): DownloadAccount failed. Operation took longer than 15 seconds.");

                            Disconnect();

                            if (_accountHoldingsLastException != null)
                            {
                                // if an exception was thrown during account download, do not retry but exit immediately
                                attempt = maxAttempts;
                                throw new Exception(_accountHoldingsLastException.Message, _accountHoldingsLastException);
                            }

                            if (attempt++ < maxAttempts)
                            {
                                Thread.Sleep(1000);
                                continue;
                            }

                            throw new TimeoutException("InteractiveBrokersBrokerage.Connect(): DownloadAccount failed.");
                        }
                    }

                    // enable detailed logging
                    _client.ClientSocket.setServerLogLevel(5);

                    break;
                }
                catch (Exception err)
                {
                    // max out at 5 attempts to connect ~1 minute
                    if (attempt++ < maxAttempts)
                    {
                        Thread.Sleep(15000);
                        continue;
                    }

                    // we couldn't connect after several attempts, log the error and throw an exception
                    Log.Error(err);

                    throw;
                }
            }

            // if we reached here we should be connected, check just in case
            if (IsConnected)
            {
                Log.Trace("InteractiveBrokersBrokerage.Connect(): Restoring data subscriptions...");
                RestoreDataSubscriptions();
            }
            else
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "ConnectionState", "Unexpected, not connected state. Unable to connect to Interactive Brokers. Terminating algorithm."));
            }
        }

        /// <summary>
        /// Downloads the financial advisor configuration information.
        /// This method is called upon successful connection.
        /// </summary>
        private bool DownloadFinancialAdvisorAccount(string account)
        {
            if (!_accountData.FinancialAdvisorConfiguration.Load(_client))
                return false;

            // Only one account can be subscribed at a time.
            // With Financial Advisory (FA) account structures there is an alternative way of
            // specifying the account code such that information is returned for 'All' sub accounts.
            // This is done by appending the letter 'A' to the end of the account number
            // https://interactivebrokers.github.io/tws-api/account_updates.html#gsc.tab=0

            // subscribe to the FA account
            return DownloadAccount(account + "A");
        }

        /// <summary>
        /// Downloads the account information and subscribes to account updates.
        /// This method is called upon successful connection.
        /// </summary>
        private bool DownloadAccount(string account)
        {
            Log.Trace($"InteractiveBrokersBrokerage.DownloadAccount(): Downloading account data for {account}");

            _accountHoldingsLastException = null;
            _accountHoldingsResetEvent.Reset();

            // define our event handler, this acts as stop to make sure when we leave Connect we have downloaded the full account
            EventHandler<IB.AccountDownloadEndEventArgs> clientOnAccountDownloadEnd = (sender, args) =>
            {
                Log.Trace("InteractiveBrokersBrokerage.DownloadAccount(): Finished account download for " + args.Account);
                _accountHoldingsResetEvent.Set();
            };
            _client.AccountDownloadEnd += clientOnAccountDownloadEnd;

            // we'll wait to get our first account update, we need to be absolutely sure we
            // have downloaded the entire account before leaving this function
            var firstAccountUpdateReceived = new ManualResetEvent(false);
            EventHandler<IB.UpdateAccountValueEventArgs> clientOnUpdateAccountValue = (sender, args) =>
            {
                firstAccountUpdateReceived.Set();
            };

            _client.UpdateAccountValue += clientOnUpdateAccountValue;

            // first we won't subscribe, wait for this to finish, below we'll subscribe for continuous updates
            _client.ClientSocket.reqAccountUpdates(true, account);

            // wait to see the first account value update
            firstAccountUpdateReceived.WaitOne(2500);

            // take pause to ensure the account is downloaded before continuing, this was added because running in
            // linux there appears to be different behavior where the account download end fires immediately.
            Thread.Sleep(2500);

            if (!_accountHoldingsResetEvent.WaitOne(15000))
            {
                // remove our event handlers
                _client.AccountDownloadEnd -= clientOnAccountDownloadEnd;
                _client.UpdateAccountValue -= clientOnUpdateAccountValue;

                Log.Trace("InteractiveBrokersBrokerage.DownloadAccount(): Operation took longer than 15 seconds.");

                return false;
            }

            // remove our event handlers
            _client.AccountDownloadEnd -= clientOnAccountDownloadEnd;
            _client.UpdateAccountValue -= clientOnUpdateAccountValue;

            return _accountHoldingsLastException == null;
        }

        /// <summary>
        /// Disconnects the client from the IB gateway
        /// </summary>
        public override void Disconnect()
        {
            _client.ClientSocket.eDisconnect();

            if (_messageProcessingThread != null)
            {
                _signal.issueSignal();
                _messageProcessingThread.Join();
                _messageProcessingThread = null;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            Log.Trace("InteractiveBrokersBrokerage.Dispose(): Disposing of IB resources.");

            if (_client != null)
            {
                Disconnect();
                _client.Dispose();
            }

            _aggregator.DisposeSafely();
            _ibAutomater?.Stop();

            _messagingRateLimiter.Dispose();
        }

        /// <summary>
        /// Places the order with InteractiveBrokers
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <param name="needsNewId">Set to true to generate a new order ID, false to leave it alone</param>
        /// <param name="exchange">The exchange to send the order to, defaults to "Smart" to use IB's smart routing</param>
        private void IBPlaceOrder(Order order, bool needsNewId, string exchange = null)
        {
            // MOO/MOC require directed option orders
            if (exchange == null &&
                order.Symbol.SecurityType == SecurityType.Option &&
                (order.Type == OrderType.MarketOnOpen || order.Type == OrderType.MarketOnClose))
            {
                exchange = Market.CBOE.ToUpperInvariant();
            }

            var contract = CreateContract(order.Symbol, false, exchange);

            int ibOrderId;
            if (needsNewId)
            {
                // the order ids are generated for us by the SecurityTransactionManaer
                var id = GetNextBrokerageOrderId();
                order.BrokerId.Add(id.ToStringInvariant());
                ibOrderId = id;
            }
            else if (order.BrokerId.Any())
            {
                // this is *not* perfect code
                ibOrderId = Parse.Int(order.BrokerId[0]);
            }
            else
            {
                throw new ArgumentException("Expected order with populated BrokerId for updating orders.");
            }

            _requestInformation[ibOrderId] = $"IBPlaceOrder: {order.Symbol.Value} ({contract})";

            CheckRateLimiting();

            if (order.Type == OrderType.OptionExercise)
            {
                _client.ClientSocket.exerciseOptions(ibOrderId, contract, 1, decimal.ToInt32(order.Quantity), _account, 0);
            }
            else
            {
                var ibOrder = ConvertOrder(order, contract, ibOrderId);
                _client.ClientSocket.placeOrder(ibOrder.OrderId, contract, ibOrder);
            }
        }

        private static string GetUniqueKey(Contract contract)
        {
            return $"{contract} {contract.LastTradeDateOrContractMonth.ToStringInvariant()} {contract.Strike.ToStringInvariant()} {contract.Right}";
        }

        private string GetPrimaryExchange(Contract contract, Symbol symbol)
        {
            ContractDetails details;
            if (_contractDetails.TryGetValue(GetUniqueKey(contract), out details))
            {
                return details.Contract.PrimaryExch;
            }

            details = GetContractDetails(contract, symbol);
            if (details == null)
            {
                // we were unable to find the contract details
                return null;
            }

            return details.Contract.PrimaryExch;
        }

        private string GetTradingClass(Contract contract, Symbol symbol)
        {
            ContractDetails details;
            if (_contractDetails.TryGetValue(GetUniqueKey(contract), out details))
            {
                return details.Contract.TradingClass;
            }

            details = GetContractDetails(contract, symbol);
            if (details == null)
            {
                // we were unable to find the contract details
                return null;
            }

            return details.Contract.TradingClass;
        }

        private decimal GetMinTick(Contract contract, Symbol symbol)
        {
            ContractDetails details;
            if (_contractDetails.TryGetValue(GetUniqueKey(contract), out details))
            {
                return (decimal)details.MinTick;
            }

            details = GetContractDetails(contract, symbol);
            if (details == null)
            {
                // we were unable to find the contract details
                return 0;
            }

            return (decimal)details.MinTick;
        }

        private ContractDetails GetContractDetails(Contract contract, Symbol symbol)
        {
            const int timeout = 60; // sec

            ContractDetails details = null;
            var requestId = GetNextRequestId();

            _requestInformation[requestId] = $"GetContractDetails: {symbol.Value} ({contract})";

            var manualResetEvent = new ManualResetEvent(false);

            // define our event handlers
            EventHandler<IB.ContractDetailsEventArgs> clientOnContractDetails = (sender, args) =>
            {
                // ignore other requests
                if (args.RequestId != requestId) return;
                details = args.ContractDetails;
                var uniqueKey = GetUniqueKey(contract);
                _contractDetails.TryAdd(uniqueKey, details);
                manualResetEvent.Set();
                Log.Trace("InteractiveBrokersBrokerage.GetContractDetails(): clientOnContractDetails event: " + uniqueKey);
            };

            EventHandler<IB.ErrorEventArgs> clientOnError = (sender, args) =>
            {
                if (args.Id == requestId)
                {
                    manualResetEvent.Set();
                }
            };

            _client.ContractDetails += clientOnContractDetails;
            _client.Error += clientOnError;

            CheckRateLimiting();

            // make the request for data
            _client.ClientSocket.reqContractDetails(requestId, contract);

            if (!manualResetEvent.WaitOne(timeout * 1000))
            {
                Log.Error("InteractiveBrokersBrokerage.GetContractDetails(): failed to receive response from IB within {0} seconds", timeout);
            }

            // be sure to remove our event handlers
            _client.Error -= clientOnError;
            _client.ContractDetails -= clientOnContractDetails;

            return details;
        }

        private string GetFuturesContractExchange(Contract contract, string ticker)
        {
            // searching for available contracts on different exchanges
            var contractDetails = FindContracts(contract, ticker);

            var exchanges = _futuresExchanges.Values.Reverse().ToArray();

            // sorting list of available contracts by exchange priority, taking the top 1
            return contractDetails
                    .Select(details => details.Contract.Exchange)
                    .OrderByDescending(e => Array.IndexOf(exchanges, e))
                    .FirstOrDefault();
        }

        public IEnumerable<ContractDetails> FindContracts(Contract contract, string ticker)
        {
            const int timeout = 60; // sec

            var requestId = GetNextRequestId();

            _requestInformation[requestId] = $"FindContracts: {ticker} ({contract})";

            var manualResetEvent = new ManualResetEvent(false);
            var contractDetails = new List<ContractDetails>();

            // define our event handlers
            EventHandler<IB.ContractDetailsEventArgs> clientOnContractDetails = (sender, args) =>
            {
                if (args.RequestId == requestId)
                {
                    contractDetails.Add(args.ContractDetails);
                }
            };

            EventHandler<IB.RequestEndEventArgs> clientOnContractDetailsEnd = (sender, args) =>
            {
                if (args.RequestId == requestId)
                {
                    manualResetEvent.Set();
                }
            };

            EventHandler<IB.ErrorEventArgs> clientOnError = (sender, args) =>
            {
                if (args.Id == requestId)
                {
                    manualResetEvent.Set();
                }
            };

            _client.ContractDetails += clientOnContractDetails;
            _client.ContractDetailsEnd += clientOnContractDetailsEnd;
            _client.Error += clientOnError;

            CheckRateLimiting();

            // make the request for data
            _client.ClientSocket.reqContractDetails(requestId, contract);

            if (!manualResetEvent.WaitOne(timeout * 1000))
            {
                Log.Error("InteractiveBrokersBrokerage.FindContracts(): failed to receive response from IB within {0} seconds", timeout);
            }

            // be sure to remove our event handlers
            _client.Error -= clientOnError;
            _client.ContractDetailsEnd -= clientOnContractDetailsEnd;
            _client.ContractDetails -= clientOnContractDetails;

            return contractDetails;
        }

        /// <summary>
        /// Handles error messages from IB
        /// </summary>
        private void HandleError(object sender, IB.ErrorEventArgs e)
        {
            // https://www.interactivebrokers.com/en/software/api/apiguide/tables/api_message_codes.htm

            var requestId = e.Id;
            var errorCode = e.Code;
            var errorMsg = e.Message;

            // rewrite these messages to be single lined
            errorMsg = errorMsg.Replace("\r\n", ". ").Replace("\r", ". ").Replace("\n", ". ");

            // if there is additional information for the originating request, append it to the error message
            string requestMessage;
            if (_requestInformation.TryGetValue(requestId, out requestMessage))
            {
                errorMsg += ". Origin: " + requestMessage;
            }

            // historical data request with no data returned
            if (errorCode == 162 && errorMsg.Contains("HMDS query returned no data"))
            {
                return;
            }

            Log.Trace($"InteractiveBrokersBrokerage.HandleError(): RequestId: {requestId} ErrorCode: {errorCode} - {errorMsg}");

            // figure out the message type based on our code collections below
            var brokerageMessageType = BrokerageMessageType.Information;
            if (ErrorCodes.Contains(errorCode))
            {
                brokerageMessageType = BrokerageMessageType.Error;
            }
            else if (WarningCodes.Contains(errorCode))
            {
                brokerageMessageType = BrokerageMessageType.Warning;
            }

            // code 1100 is a connection failure, we'll wait a minute before exploding gracefully
            if (errorCode == 1100)
            {
                if (!_stateManager.Disconnected1100Fired)
                {
                    _stateManager.Disconnected1100Fired = true;

                    // begin the try wait logic
                    TryWaitForReconnect();
                }
                else
                {
                    // The IB API sends many consecutive disconnect messages (1100) during nightly reset periods and weekends,
                    // so we send the message event only when we transition from connected to disconnected state,
                    // to avoid flooding the logs with the same message.
                    return;
                }
            }
            else if (errorCode == 1102)
            {
                // Connectivity between IB and TWS has been restored - data maintained.
                OnMessage(BrokerageMessageEvent.Reconnected(errorMsg));

                _stateManager.Disconnected1100Fired = false;
                return;
            }
            else if (errorCode == 1101)
            {
                // Connectivity between IB and TWS has been restored - data lost.
                OnMessage(BrokerageMessageEvent.Reconnected(errorMsg));

                _stateManager.Disconnected1100Fired = false;

                RestoreDataSubscriptions();
                return;
            }
            else if (errorCode == 506)
            {
                Log.Trace("InteractiveBrokersBrokerage.HandleError(): Server Version: " + _client.ClientSocket.ServerVersion);
            }

            if (InvalidatingCodes.Contains(errorCode))
            {
                var message = $"{errorCode} - {errorMsg}";
                Log.Trace($"InteractiveBrokersBrokerage.HandleError.InvalidateOrder(): IBOrderId: {requestId} ErrorCode: {message}");

                // invalidate the order
                var order = _orderProvider.GetOrderByBrokerageId(requestId);
                if (order != null)
                {
                    var orderEvent = new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero)
                    {
                        Status = OrderStatus.Invalid,
                        Message = message
                    };
                    OnOrderEvent(orderEvent);
                }
                else
                {
                    Log.Error($"InteractiveBrokersBrokerage.HandleError.InvalidateOrder(): Unable to locate order with BrokerageID {requestId}");
                }
            }

            OnMessage(new BrokerageMessageEvent(brokerageMessageType, errorCode, errorMsg));
        }

        /// <summary>
        /// Restores data subscriptions existing before the IB Gateway restart
        /// </summary>
        private void RestoreDataSubscriptions()
        {
            List<Symbol> subscribedSymbols;
            lock (_sync)
            {
                subscribedSymbols = _subscribedSymbols.Keys.ToList();

                _subscribedSymbols.Clear();
                _subscribedTickers.Clear();
                _underlyings.Clear();
            }

            Subscribe(subscribedSymbols);
        }

        /// <summary>
        /// If we lose connection to TWS/IB servers we don't want to send the Error event if it is within
        /// the scheduled server reset times
        /// </summary>
        private void TryWaitForReconnect()
        {
            // IB has server reset schedule: https://www.interactivebrokers.com/en/?f=%2Fen%2Fsoftware%2FsystemStatus.php%3Fib_entity%3Dllc

            if (!_stateManager.Disconnected1100Fired)
            {
                return;
            }

            var isResetTime = _ibAutomater.IsWithinScheduledServerResetTimes();

            if (!isResetTime)
            {
                if (!_stateManager.PreviouslyInResetTime)
                {
                    // if we were disconnected and we're not within the reset times, send the error event
                    OnMessage(BrokerageMessageEvent.Disconnected("Connection with Interactive Brokers lost. " +
                                                                 "This could be because of internet connectivity issues or a log in from another location."
                        ));
                }
            }
            else
            {
                Log.Trace("InteractiveBrokersBrokerage.TryWaitForReconnect(): Within server reset times, trying to wait for reconnect...");

                // we're still not connected but we're also within the schedule reset time, so just keep polling
                Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => TryWaitForReconnect());
            }

            _stateManager.PreviouslyInResetTime = isResetTime;
        }

        /// <summary>
        /// Stores all the account values
        /// </summary>
        private void HandleUpdateAccountValue(object sender, IB.UpdateAccountValueEventArgs e)
        {
            try
            {
                _accountData.AccountProperties[e.Currency + ":" + e.Key] = e.Value;

                // we want to capture if the user's cash changes so we can reflect it in the algorithm
                if (e.Key == AccountValueKeys.CashBalance && e.Currency != "BASE")
                {
                    var cashBalance = decimal.Parse(e.Value, CultureInfo.InvariantCulture);
                    _accountData.CashBalances.AddOrUpdate(e.Currency, cashBalance);

                    OnAccountChanged(new AccountEvent(e.Currency, cashBalance));
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleUpdateAccountValue(): " + err);
            }
        }

        /// <summary>
        /// Handle order events from IB
        /// </summary>
        private void HandleOrderStatusUpdates(object sender, IB.OrderStatusEventArgs update)
        {
            try
            {
                Log.Trace("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): " + update);

                if (!IsConnected)
                {
                    if (_client != null)
                    {
                        Log.Error($"InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Not connected; update dropped, _client.Connected: {_client.Connected}, _disconnected1100Fired: {_stateManager.Disconnected1100Fired}");
                    }
                    else
                    {
                        Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Not connected; _client is null");
                    }
                    return;
                }

                var order = _orderProvider.GetOrderByBrokerageId(update.OrderId);
                if (order == null)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Unable to locate order with BrokerageID " + update.OrderId);
                    return;
                }

                var status = ConvertOrderStatus(update.Status);

                if (status == OrderStatus.Filled || status == OrderStatus.PartiallyFilled)
                {
                    // fill events will be only processed in HandleExecutionDetails and HandleCommissionReports
                    return;
                }

                int id;
                // if we get a Submitted status and we had placed an order update, this new event is flagged as an update
                var isUpdate = status == OrderStatus.Submitted && _orderUpdates.TryRemove(order.Id, out id);

                // IB likes to duplicate/triplicate some events, so we fire non-fill events only if status changed
                if (status != order.Status || isUpdate)
                {
                    if (order.Status.IsClosed())
                    {
                        // if the order is already in a closed state, we ignore the event
                        Log.Trace("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): ignoring update in closed state - order.Status: " + order.Status + ", status: " + status);
                    }
                    else if (order.Status == OrderStatus.PartiallyFilled && (status == OrderStatus.New || status == OrderStatus.Submitted) && !isUpdate)
                    {
                        // if we receive a New or Submitted event when already partially filled, we ignore it
                        Log.Trace("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): ignoring status " + status + " after partial fills");
                    }
                    else
                    {
                        // fire the event
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Interactive Brokers Order Event")
                        {
                            Status = isUpdate ? OrderStatus.UpdateSubmitted : status
                        });
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): " + err);
            }
        }

        /// <summary>
        /// Handle OpenOrder event from IB
        /// </summary>
        private static void HandleOpenOrder(object sender, IB.OpenOrderEventArgs e)
        {
            Log.Trace($"InteractiveBrokersBrokerage.HandleOpenOrder(): {e}");
        }

        /// <summary>
        /// Handle OpenOrderEnd event from IB
        /// </summary>
        private static void HandleOpenOrderEnd(object sender, EventArgs e)
        {
            Log.Trace("InteractiveBrokersBrokerage.HandleOpenOrderEnd()");
        }

        /// <summary>
        /// Handle execution events from IB
        /// </summary>
        /// <remarks>
        /// This needs to be handled because if a market order is executed immediately, there will be no OrderStatus event
        /// https://interactivebrokers.github.io/tws-api/order_submission.html#order_status
        /// </remarks>
        private void HandleExecutionDetails(object sender, IB.ExecutionDetailsEventArgs executionDetails)
        {
            try
            {
                Log.Trace("InteractiveBrokersBrokerage.HandleExecutionDetails(): " + executionDetails);

                if (!IsConnected)
                {
                    if (_client != null)
                    {
                        Log.Error($"InteractiveBrokersBrokerage.HandleExecutionDetails(): Not connected; update dropped, _client.Connected: {_client.Connected}, _disconnected1100Fired: {_stateManager.Disconnected1100Fired}");
                    }
                    else
                    {
                        Log.Error("InteractiveBrokersBrokerage.HandleExecutionDetails(): Not connected; _client is null");
                    }
                    return;
                }

                var order = _orderProvider.GetOrderByBrokerageId(executionDetails.Execution.OrderId);
                if (order == null)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleExecutionDetails(): Unable to locate order with BrokerageID " + executionDetails.Execution.OrderId);
                    return;
                }

                // For financial advisor orders, we first receive executions and commission reports for the master order,
                // followed by executions and commission reports for all allocations.
                // We don't want to emit fills for these allocation events,
                // so we ignore events received after the order is completely filled or
                // executions for allocations which are already included in the master execution.

                CommissionReport commissionReport;
                if (_commissionReports.TryGetValue(executionDetails.Execution.ExecId, out commissionReport))
                {
                    if (CanEmitFill(order, executionDetails.Execution))
                    {
                        // we have both execution and commission report, emit the fill
                        EmitOrderFill(order, executionDetails.Execution, commissionReport);
                    }

                    _commissionReports.TryRemove(commissionReport.ExecId, out commissionReport);
                }
                else
                {
                    // save execution in dictionary and wait for commission report
                    _orderExecutions[executionDetails.Execution.ExecId] = executionDetails.Execution;
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleExecutionDetails(): " + err);
            }
        }

        /// <summary>
        /// Handle commission report events from IB
        /// </summary>
        /// <remarks>
        /// This method matches commission reports with previously saved executions and fires the OrderEvents.
        /// </remarks>
        private void HandleCommissionReport(object sender, IB.CommissionReportEventArgs e)
        {
            try
            {
                Log.Trace("InteractiveBrokersBrokerage.HandleCommissionReport(): " + e);

                Execution execution;
                if (!_orderExecutions.TryGetValue(e.CommissionReport.ExecId, out execution))
                {
                    // save commission in dictionary and wait for execution event
                    _commissionReports[e.CommissionReport.ExecId] = e.CommissionReport;
                    return;
                }

                var order = _orderProvider.GetOrderByBrokerageId(execution.OrderId);
                if (order == null)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleExecutionDetails(): Unable to locate order with BrokerageID " + execution.OrderId);
                    return;
                }

                if (CanEmitFill(order, execution))
                {
                    // we have both execution and commission report, emit the fill
                    EmitOrderFill(order, execution, e.CommissionReport);
                }

                // always remove previous execution
                _orderExecutions.TryRemove(e.CommissionReport.ExecId, out execution);
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleCommissionReport(): " + err);
            }
        }

        /// <summary>
        /// Decide which fills should be emitted, accounting for different types of Financial Advisor orders
        /// </summary>
        private bool CanEmitFill(Order order, Execution execution)
        {
            if (order.Status == OrderStatus.Filled)
                return false;

            // non-FA orders
            if (!IsFinancialAdvisor)
                return true;

            var orderProperties = order.Properties as InteractiveBrokersOrderProperties;
            if (orderProperties == null)
                return true;

            return
                // FA master orders for groups/profiles
                string.IsNullOrWhiteSpace(orderProperties.Account) && execution.AcctNumber == _account ||

                // FA orders for single managed accounts
                !string.IsNullOrWhiteSpace(orderProperties.Account) && execution.AcctNumber == orderProperties.Account;
        }

        /// <summary>
        /// Emits an order fill (or partial fill) including the actual IB commission paid
        /// </summary>
        private void EmitOrderFill(Order order, Execution execution, CommissionReport commissionReport)
        {
            var currentQuantityFilled = Convert.ToInt32(execution.Shares);
            var totalQuantityFilled = Convert.ToInt32(execution.CumQty);
            var remainingQuantity = Convert.ToInt32(order.AbsoluteQuantity - totalQuantityFilled);
            var price = Convert.ToDecimal(execution.Price);
            var orderFee = new OrderFee(new CashAmount(
                Convert.ToDecimal(commissionReport.Commission),
                commissionReport.Currency.ToUpperInvariant()));

            // set order status based on remaining quantity
            var status = remainingQuantity > 0 ? OrderStatus.PartiallyFilled : OrderStatus.Filled;

            // mark sells as negative quantities
            var fillQuantity = order.Direction == OrderDirection.Buy ? currentQuantityFilled : -currentQuantityFilled;
            order.PriceCurrency = _securityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;
            var orderEvent = new OrderEvent(order, DateTime.UtcNow, orderFee, "Interactive Brokers Order Fill Event")
            {
                Status = status,
                FillPrice = price,
                FillQuantity = fillQuantity
            };
            if (remainingQuantity != 0)
            {
                orderEvent.Message += " - " + remainingQuantity + " remaining";
            }

            // fire the order fill event
            OnOrderEvent(orderEvent);
        }

        /// <summary>
        /// Handle portfolio changed events from IB
        /// </summary>
        private void HandlePortfolioUpdates(object sender, IB.UpdatePortfolioEventArgs e)
        {
            try
            {
                _accountHoldingsResetEvent.Reset();
                var holding = CreateHolding(e);
                _accountData.AccountHoldings[holding.Symbol.Value] = holding;
            }
            catch (Exception exception)
            {
                Log.Error($"InteractiveBrokersBrokerage.HandlePortfolioUpdates(): {exception}");

                if (e.Position != 0)
                {
                    // Force a runtime error only with a nonzero position for an unsupported security type,
                    // because after the user has manually closed the position and restarted the algorithm,
                    // he'll have a zero position but a nonzero realized PNL, so this event handler will be called again.

                    _accountHoldingsLastException = exception;
                    _accountHoldingsResetEvent.Set();
                }
            }
        }

        /// <summary>
        /// Converts a QC order to an IB order
        /// </summary>
        private IBApi.Order ConvertOrder(Order order, Contract contract, int ibOrderId)
        {
            var ibOrder = new IBApi.Order
            {
                ClientId = ClientId,
                OrderId = ibOrderId,
                Account = _account,
                Action = ConvertOrderDirection(order.Direction),
                TotalQuantity = (int)Math.Abs(order.Quantity),
                OrderType = ConvertOrderType(order.Type),
                AllOrNone = false,
                Tif = ConvertTimeInForce(order),
                Transmit = true,
                Rule80A = _agentDescription
            };

            var gtdTimeInForce = order.TimeInForce as GoodTilDateTimeInForce;
            if (gtdTimeInForce != null)
            {
                DateTime expiryUtc;
                if (order.SecurityType == SecurityType.Forex)
                {
                    expiryUtc = gtdTimeInForce.GetForexOrderExpiryDateTime(order);
                }
                else
                {
                    var exchangeHours = MarketHoursDatabase.FromDataFolder()
                        .GetExchangeHours(order.Symbol.ID.Market, order.Symbol, order.SecurityType);

                    var expiry = exchangeHours.GetNextMarketClose(gtdTimeInForce.Expiry.Date, false);
                    expiryUtc = expiry.ConvertToUtc(exchangeHours.TimeZone);
                }

                // The IB format for the GoodTillDate order property is "yyyymmdd hh:mm:ss xxx" where yyyymmdd and xxx are optional.
                // E.g.: 20031126 15:59:00 EST
                // If no date is specified, current date is assumed. If no time-zone is specified, local time-zone is assumed.

                ibOrder.GoodTillDate = expiryUtc.ToString("yyyyMMdd HH:mm:ss UTC", CultureInfo.InvariantCulture);
            }

            var limitOrder = order as LimitOrder;
            var stopMarketOrder = order as StopMarketOrder;
            var stopLimitOrder = order as StopLimitOrder;
            if (limitOrder != null)
            {
                ibOrder.LmtPrice = Convert.ToDouble(RoundPrice(limitOrder.LimitPrice, GetMinTick(contract, order.Symbol)));
            }
            else if (stopMarketOrder != null)
            {
                ibOrder.AuxPrice = Convert.ToDouble(RoundPrice(stopMarketOrder.StopPrice, GetMinTick(contract, order.Symbol)));
            }
            else if (stopLimitOrder != null)
            {
                var minTick = GetMinTick(contract, order.Symbol);
                ibOrder.LmtPrice = Convert.ToDouble(RoundPrice(stopLimitOrder.LimitPrice, minTick));
                ibOrder.AuxPrice = Convert.ToDouble(RoundPrice(stopLimitOrder.StopPrice, minTick));
            }

            // add financial advisor properties
            if (IsFinancialAdvisor)
            {
                // https://interactivebrokers.github.io/tws-api/financial_advisor.html#gsc.tab=0

                var orderProperties = order.Properties as InteractiveBrokersOrderProperties;
                if (orderProperties != null)
                {
                    if (!string.IsNullOrWhiteSpace(orderProperties.Account))
                    {
                        // order for a single managed account
                        ibOrder.Account = orderProperties.Account;
                    }
                    else if (!string.IsNullOrWhiteSpace(orderProperties.FaProfile))
                    {
                        // order for an account profile
                        ibOrder.FaProfile = orderProperties.FaProfile;

                    }
                    else if (!string.IsNullOrWhiteSpace(orderProperties.FaGroup))
                    {
                        // order for an account group
                        ibOrder.FaGroup = orderProperties.FaGroup;
                        ibOrder.FaMethod = orderProperties.FaMethod;

                        if (ibOrder.FaMethod == "PctChange")
                        {
                            ibOrder.FaPercentage = orderProperties.FaPercentage.ToStringInvariant();
                            ibOrder.TotalQuantity = 0;
                        }
                    }
                }
            }

            // not yet supported
            //ibOrder.ParentId =
            //ibOrder.OcaGroup =

            return ibOrder;
        }

        private Order ConvertOrder(IBApi.Order ibOrder, Contract contract)
        {
            // this function is called by GetOpenOrders which is mainly used by the setup handler to
            // initialize algorithm state.  So the only time we'll be executing this code is when the account
            // has orders sitting and waiting from before algo initialization...
            // because of this we can't get the time accurately

            Order order;
            var mappedSymbol = MapSymbol(contract);
            var direction = ConvertOrderDirection(ibOrder.Action);
            var quantitySign = direction == OrderDirection.Sell ? -1 : 1;
            var orderType = ConvertOrderType(ibOrder);
            switch (orderType)
            {
                case OrderType.Market:
                    order = new MarketOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity) * quantitySign,
                        new DateTime() // not sure how to get this data
                        );
                    break;

                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity) * quantitySign,
                        new DateTime());
                    break;

                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity) * quantitySign,
                        new DateTime()
                        );
                    break;

                case OrderType.Limit:
                    order = new LimitOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity) * quantitySign,
                        Convert.ToDecimal(ibOrder.LmtPrice),
                        new DateTime()
                        );
                    break;

                case OrderType.StopMarket:
                    order = new StopMarketOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity) * quantitySign,
                        Convert.ToDecimal(ibOrder.AuxPrice),
                        new DateTime()
                        );
                    break;

                case OrderType.StopLimit:
                    order = new StopLimitOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity) * quantitySign,
                        Convert.ToDecimal(ibOrder.AuxPrice),
                        Convert.ToDecimal(ibOrder.LmtPrice),
                        new DateTime()
                        );
                    break;

                default:
                    throw new InvalidEnumArgumentException("orderType", (int)orderType, typeof(OrderType));
            }

            order.BrokerId.Add(ibOrder.OrderId.ToStringInvariant());

            order.Properties.TimeInForce = ConvertTimeInForce(ibOrder.Tif, ibOrder.GoodTillDate);

            return order;
        }

        /// <summary>
        /// Creates an IB contract from the order.
        /// </summary>
        /// <param name="symbol">The symbol whose contract we need to create</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="exchange">The exchange where the order will be placed, defaults to 'Smart'</param>
        /// <returns>A new IB contract for the order</returns>
        private Contract CreateContract(Symbol symbol, bool includeExpired, string exchange = null)
        {
            var securityType = ConvertSecurityType(symbol.ID.SecurityType);
            var ibSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var contract = new Contract
            {
                Symbol = ibSymbol,
                Exchange = exchange ?? "Smart",
                SecType = securityType,
                Currency = Currencies.USD
            };
            if (symbol.ID.SecurityType == SecurityType.Forex)
            {
                // forex is special, so rewrite some of the properties to make it work
                contract.Exchange = "IDEALPRO";
                contract.Symbol = ibSymbol.Substring(0, 3);
                contract.Currency = ibSymbol.Substring(3);
            }

            if (symbol.ID.SecurityType == SecurityType.Equity)
            {
                contract.PrimaryExch = GetPrimaryExchange(contract, symbol);
            }

            if (symbol.ID.SecurityType == SecurityType.Option)
            {
                contract.LastTradeDateOrContractMonth = symbol.ID.Date.ToStringInvariant(DateFormat.EightCharacter);
                contract.Right = symbol.ID.OptionRight == OptionRight.Call ? IB.RightType.Call : IB.RightType.Put;
                contract.Strike = Convert.ToDouble(symbol.ID.StrikePrice);
                contract.Symbol = ibSymbol;
                contract.Multiplier = _securityProvider.GetSecurity(symbol)?.SymbolProperties.ContractMultiplier.ToString(CultureInfo.InvariantCulture) ?? "100";
                contract.TradingClass = GetTradingClass(contract, symbol);

                contract.IncludeExpired = includeExpired;
            }

            if (symbol.ID.SecurityType == SecurityType.Future)
            {
                // we convert Market.* markets into IB exchanges if we have them in our map

                contract.Symbol = ibSymbol;
                contract.LastTradeDateOrContractMonth = symbol.ID.Date.ToStringInvariant(DateFormat.EightCharacter);

                contract.Exchange = _futuresExchanges.ContainsKey(symbol.ID.Market) ?
                                        _futuresExchanges[symbol.ID.Market] :
                                        symbol.ID.Market;

                contract.IncludeExpired = includeExpired;
            }

            return contract;
        }

        /// <summary>
        /// Maps OrderDirection enumeration
        /// </summary>
        private OrderDirection ConvertOrderDirection(string direction)
        {
            switch (direction)
            {
                case IB.ActionSide.Buy: return OrderDirection.Buy;
                case IB.ActionSide.Sell: return OrderDirection.Sell;
                case IB.ActionSide.Undefined: return OrderDirection.Hold;
                default:
                    throw new ArgumentException(direction, "direction");
            }
        }

        /// <summary>
        /// Maps OrderDirection enumeration
        /// </summary>
        private static string ConvertOrderDirection(OrderDirection direction)
        {
            switch (direction)
            {
                case OrderDirection.Buy:  return IB.ActionSide.Buy;
                case OrderDirection.Sell: return IB.ActionSide.Sell;
                case OrderDirection.Hold: return IB.ActionSide.Undefined;
                default:
                    throw new InvalidEnumArgumentException("direction", (int)direction, typeof(OrderDirection));
            }
        }

        /// <summary>
        /// Maps OrderType enum
        /// </summary>
        private static string ConvertOrderType(OrderType type)
        {
            switch (type)
            {
                case OrderType.Market:          return IB.OrderType.Market;
                case OrderType.Limit:           return IB.OrderType.Limit;
                case OrderType.StopMarket:      return IB.OrderType.Stop;
                case OrderType.StopLimit:       return IB.OrderType.StopLimit;
                case OrderType.MarketOnOpen:    return IB.OrderType.Market;
                case OrderType.MarketOnClose:   return IB.OrderType.MarketOnClose;
                default:
                    throw new InvalidEnumArgumentException("type", (int)type, typeof(OrderType));
            }
        }

        /// <summary>
        /// Maps OrderType enum
        /// </summary>
        private static OrderType ConvertOrderType(IBApi.Order order)
        {
            switch (order.OrderType)
            {
                case IB.OrderType.Limit:            return OrderType.Limit;
                case IB.OrderType.Stop:             return OrderType.StopMarket;
                case IB.OrderType.StopLimit:        return OrderType.StopLimit;
                case IB.OrderType.MarketOnClose:    return OrderType.MarketOnClose;

                case IB.OrderType.Market:
                    if (order.Tif == IB.TimeInForce.MarketOnOpen)
                    {
                        return OrderType.MarketOnOpen;
                    }
                    return OrderType.Market;

                default:
                    throw new ArgumentException(order.OrderType, "order.OrderType");
            }
        }

        /// <summary>
        /// Maps TimeInForce from IB to LEAN
        /// </summary>
        private static TimeInForce ConvertTimeInForce(string timeInForce, string expiryDateTime)
        {
            switch (timeInForce)
            {
                case IB.TimeInForce.Day:
                    return TimeInForce.Day;

                case IB.TimeInForce.GoodTillDate:
                    return TimeInForce.GoodTilDate(ParseExpiryDateTime(expiryDateTime));

                //case IB.TimeInForce.FillOrKill:
                //    return TimeInForce.FillOrKill;

                //case IB.TimeInForce.ImmediateOrCancel:
                //    return TimeInForce.ImmediateOrCancel;

                case IB.TimeInForce.MarketOnOpen:
                case IB.TimeInForce.GoodTillCancel:
                default:
                    return TimeInForce.GoodTilCanceled;
            }
        }

        private static DateTime ParseExpiryDateTime(string expiryDateTime)
        {
            // NOTE: we currently ignore the time zone in this method for a couple of reasons:
            // - TZ abbreviations are ambiguous and unparsable to a unique time zone
            //   see this article for more info:
            //   https://codeblog.jonskeet.uk/2015/05/05/common-mistakes-in-datetime-formatting-and-parsing/
            // - IB seems to also have issues with Daylight Saving Time zones
            //   Example: an order submitted from Europe with GoodTillDate property set to "20180524 21:00:00 UTC"
            //   when reading the open orders, the same property will have this value: "20180524 23:00:00 CET"
            //   which is incorrect: should be CEST (UTC+2) instead of CET (UTC+1)

            // We can ignore this issue, because the method is only called by GetOpenOrders,
            // we only call GetOpenOrders during live trading, which means we won't be simulating time in force
            // and instead will rely on brokerages to apply TIF properly.

            var parts = expiryDateTime.Split(' ');
            if (parts.Length == 3)
            {
                expiryDateTime = expiryDateTime.Substring(0, expiryDateTime.LastIndexOf(" ", StringComparison.Ordinal));
            }

            return DateTime.ParseExact(expiryDateTime, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture).Date;
        }

        /// <summary>
        /// Maps TimeInForce from LEAN to IB
        /// </summary>
        private static string ConvertTimeInForce(Order order)
        {
            if (order.Type == OrderType.MarketOnOpen)
            {
                return IB.TimeInForce.MarketOnOpen;
            }
            if (order.Type == OrderType.MarketOnClose)
            {
                return IB.TimeInForce.Day;
            }

            if (order.TimeInForce is DayTimeInForce)
            {
                return IB.TimeInForce.Day;
            }

            if (order.TimeInForce is GoodTilDateTimeInForce)
            {
                return IB.TimeInForce.GoodTillDate;
            }

            //if (order.TimeInForce is FillOrKillTimeInForce)
            //{
            //    return IB.TimeInForce.FillOrKill;
            //}

            //if (order.TimeInForce is ImmediateOrCancelTimeInForce)
            //{
            //    return IB.TimeInForce.ImmediateOrCancel;
            //}

            return IB.TimeInForce.GoodTillCancel;
        }

        /// <summary>
        /// Maps IB's OrderStats enum
        /// </summary>
        private static OrderStatus ConvertOrderStatus(string status)
        {
            switch (status)
            {
                case IB.OrderStatus.ApiPending:
                case IB.OrderStatus.PendingSubmit:
                    return OrderStatus.New;

                case IB.OrderStatus.PendingCancel:
                    return OrderStatus.CancelPending;

                case IB.OrderStatus.ApiCancelled:
                case IB.OrderStatus.Cancelled:
                    return OrderStatus.Canceled;

                case IB.OrderStatus.Submitted:
                case IB.OrderStatus.PreSubmitted:
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
                    throw new ArgumentException(status, "status");
            }
        }

        /// <summary>
        /// Maps SecurityType enum
        /// </summary>
        private static string ConvertSecurityType(SecurityType type)
        {
            switch (type)
            {
                case SecurityType.Equity:
                    return IB.SecurityType.Stock;

                case SecurityType.Option:
                    return IB.SecurityType.Option;

                case SecurityType.Forex:
                    return IB.SecurityType.Cash;

                case SecurityType.Future:
                    return IB.SecurityType.Future;

                default:
                    throw new ArgumentException($"The {type} security type is not currently supported.");
            }
        }

        /// <summary>
        /// Maps SecurityType enum
        /// </summary>
        private static SecurityType ConvertSecurityType(Contract contract)
        {
            switch (contract.SecType)
            {
                case IB.SecurityType.Stock:
                    return SecurityType.Equity;

                case IB.SecurityType.Option:
                    return SecurityType.Option;

                case IB.SecurityType.Cash:
                    return SecurityType.Forex;

                case IB.SecurityType.Future:
                    return SecurityType.Future;

                default:
                    throw new NotSupportedException(
                        $"An existing position or open order for an unsupported security type was found: {contract}. " +
                        "Please manually close the position or cancel the order before restarting the algorithm.");
            }
        }

        /// <summary>
        /// Maps Resolution to IB representation
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private string ConvertResolution(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                case Resolution.Second:
                    return IB.BarSize.OneSecond;
                case Resolution.Minute:
                    return IB.BarSize.OneMinute;
                case Resolution.Hour:
                    return IB.BarSize.OneHour;
                case Resolution.Daily:
                default:
                    return IB.BarSize.OneDay;
            }
        }

        /// <summary>
        /// Maps Resolution to IB span
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private string ConvertResolutionToDuration(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                case Resolution.Second:
                    return "60 S";
                case Resolution.Minute:
                    return "1 D";
                case Resolution.Hour:
                    return "1 M";
                case Resolution.Daily:
                default:
                    return "1 Y";
            }
        }

        private static TradeBar ConvertTradeBar(Symbol symbol, Resolution resolution, IB.HistoricalDataEventArgs historyBar)
        {
            var time = resolution != Resolution.Daily ?
                Time.UnixTimeStampToDateTime(Convert.ToDouble(historyBar.Bar.Time, CultureInfo.InvariantCulture)) :
                DateTime.ParseExact(historyBar.Bar.Time, "yyyyMMdd", CultureInfo.InvariantCulture);

            return new TradeBar(time, symbol, (decimal)historyBar.Bar.Open, (decimal)historyBar.Bar.High, (decimal)historyBar.Bar.Low,
                (decimal)historyBar.Bar.Close, historyBar.Bar.Volume, resolution.ToTimeSpan());
        }

        /// <summary>
        /// Creates a holding object from the UpdatePortfolioEventArgs
        /// </summary>
        private Holding CreateHolding(IB.UpdatePortfolioEventArgs e)
        {
            var symbol = MapSymbol(e.Contract);

            var currencySymbol = Currencies.GetCurrencySymbol(
                e.Contract.Currency ??
                _symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, Currencies.USD).QuoteCurrency);

            var multiplier = e.Contract.Multiplier.ConvertInvariant<decimal>();
            if (multiplier == 0m) multiplier = 1m;

            return new Holding
            {
                Symbol = symbol,
                Type = ConvertSecurityType(e.Contract),
                Quantity = e.Position,
                AveragePrice = Convert.ToDecimal(e.AverageCost) / multiplier,
                MarketPrice = Convert.ToDecimal(e.MarketPrice),
                CurrencySymbol = currencySymbol
            };
        }

        /// <summary>
        /// Maps the IB Contract's symbol to a QC symbol
        /// </summary>
        private Symbol MapSymbol(Contract contract)
        {
            var securityType = ConvertSecurityType(contract);
            var ibSymbol = securityType == SecurityType.Forex ? contract.Symbol + contract.Currency : contract.Symbol;

            var market = InteractiveBrokersBrokerageModel.DefaultMarketMap[securityType];

            if (securityType == SecurityType.Future)
            {
                var leanSymbol = _symbolMapper.GetLeanRootSymbol(ibSymbol);
                var defaultMarket = market;
                if (!_symbolPropertiesDatabase.TryGetMarket(leanSymbol, securityType, out market))
                {
                    market = defaultMarket;
                }

                var contractDate = DateTime.ParseExact(contract.LastTradeDateOrContractMonth, DateFormat.EightCharacter, CultureInfo.InvariantCulture);

                return _symbolMapper.GetLeanSymbol(ibSymbol, securityType, market, contractDate);
            }
            else if (securityType == SecurityType.Option)
            {
                var expiryDate = DateTime.ParseExact(contract.LastTradeDateOrContractMonth, DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                var right = contract.Right == IB.RightType.Call ? OptionRight.Call : OptionRight.Put;
                var strike = Convert.ToDecimal(contract.Strike);

                return _symbolMapper.GetLeanSymbol(ibSymbol, securityType, market, expiryDate, strike, right);
            }

            return _symbolMapper.GetLeanSymbol(ibSymbol, securityType, market);
        }

        private static decimal RoundPrice(decimal input, decimal minTick)
        {
            if (minTick == 0) return minTick;
            return Math.Round(input / minTick) * minTick;
        }

        /// <summary>
        /// Handles the threading issues of creating an IB order ID
        /// </summary>
        /// <returns>The new IB ID</returns>
        private int GetNextBrokerageOrderId()
        {
            lock (_nextValidIdLocker)
            {
                // return the current value and increment
                return _nextValidId++;
            }
        }

        private int GetNextRequestId()
        {
            return Interlocked.Increment(ref _nextRequestId);
        }

        private int GetNextTickerId()
        {
            return Interlocked.Increment(ref _nextTickerId);
        }

        private void HandleBrokerTime(object sender, IB.CurrentTimeUtcEventArgs e)
        {
            // keep track of clock drift
            _brokerTimeDiff = e.CurrentTimeUtc.Subtract(DateTime.UtcNow);
        }

        private TimeSpan _brokerTimeDiff = new TimeSpan(0);

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        private bool Subscribe(IEnumerable<Symbol> symbols)
        {
            try
            {
                foreach (var symbol in symbols)
                {
                    lock (_sync)
                    {
                        Log.Trace("InteractiveBrokersBrokerage.Subscribe(): Subscribe Request: " + symbol.Value);

                        if (!_subscribedSymbols.ContainsKey(symbol))
                        {
                            // processing canonical option and futures symbols
                            var subscribeSymbol = symbol;

                            // we subscribe to the underlying
                            if (symbol.ID.SecurityType == SecurityType.Option && symbol.IsCanonical())
                            {
                                subscribeSymbol = symbol.Underlying;
                                _underlyings.Add(subscribeSymbol, symbol);
                            }

                            // we ignore futures canonical symbol
                            if (symbol.ID.SecurityType == SecurityType.Future && symbol.IsCanonical())
                            {
                                return false;
                            }

                            var id = GetNextTickerId();
                            var contract = CreateContract(subscribeSymbol, false);

                            _requestInformation[id] = $"Subscribe: {symbol.Value} ({contract})";

                            CheckRateLimiting();

                            // track subscription time for minimum delay in unsubscribe
                            _subscriptionTimes[id] = DateTime.UtcNow;

                            if (_enableDelayedStreamingData)
                            {
                                // Switch to delayed market data if the user does not have the necessary real time data subscription.
                                // If live data is available, it will always be returned instead of delayed data.
                                Client.ClientSocket.reqMarketDataType(3);
                            }

                            // we would like to receive OI (101)
                            Client.ClientSocket.reqMktData(id, contract, "101", false, false, new List<TagValue>());

                            _subscribedSymbols[symbol] = id;
                            _subscribedTickers[id] = new SubscriptionEntry { Symbol = subscribeSymbol };

                            Log.Trace($"InteractiveBrokersBrokerage.Subscribe(): Subscribe Processed: {symbol.Value} ({contract}) # {id}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.Subscribe(): " + err.Message);
            }
            return false;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            try
            {
                foreach (var symbol in symbols)
                {
                    if (CanSubscribe(symbol))
                    {
                        lock (_sync)
                        {
                            Log.Trace("InteractiveBrokersBrokerage.Unsubscribe(): Unsubscribe Request: " + symbol.Value);

                            if (symbol.ID.SecurityType == SecurityType.Option && symbol.ID.StrikePrice == 0.0m)
                            {
                                _underlyings.Remove(symbol.Underlying);
                            }

                            int id;
                            if (_subscribedSymbols.TryRemove(symbol, out id))
                            {
                                CheckRateLimiting();

                                // ensure minimum time span has elapsed since the symbol was subscribed
                                DateTime subscriptionTime;
                                if (_subscriptionTimes.TryGetValue(id, out subscriptionTime))
                                {
                                    var timeSinceSubscription = DateTime.UtcNow - subscriptionTime;
                                    if (timeSinceSubscription < _minimumTimespanBeforeUnsubscribe)
                                    {
                                        var delay = Convert.ToInt32((_minimumTimespanBeforeUnsubscribe - timeSinceSubscription).TotalMilliseconds);
                                        Thread.Sleep(delay);
                                    }

                                    _subscriptionTimes.Remove(id);
                                }

                                Client.ClientSocket.cancelMktData(id);

                                SubscriptionEntry entry;
                                return _subscribedTickers.TryRemove(id, out entry);
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.Unsubscribe(): " + err.Message);
            }
            return false;
        }

        /// <summary>
        /// Returns true if this data provide can handle the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to be handled</param>
        /// <returns>True if this data provider can get data for the symbol, false otherwise</returns>
        private static bool CanSubscribe(Symbol symbol)
        {
            var market = symbol.ID.Market;
            var securityType = symbol.ID.SecurityType;

            if (symbol.Value.IndexOfInvariant("universe", true) != -1) return false;

            return
                (securityType == SecurityType.Equity && market == Market.USA) ||
                (securityType == SecurityType.Forex && market == Market.Oanda) ||
                (securityType == SecurityType.Option && market == Market.USA) ||
                (securityType == SecurityType.Future);
        }

        /// <summary>
        /// Returns a timestamp for a tick converted to the exchange time zone
        /// </summary>
        private DateTime GetRealTimeTickTime(Symbol symbol)
        {
            var time = DateTime.UtcNow.Add(_brokerTimeDiff);

            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(symbol, out exchangeTimeZone))
            {
                // read the exchange time zone from market-hours-database
                exchangeTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
                _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
            }

            return time.ConvertFromUtc(exchangeTimeZone);
        }

        private void HandleTickPrice(object sender, IB.TickPriceEventArgs e)
        {
            // tickPrice events are always followed by tickSize events,
            // so we save off the bid/ask/last prices and only emit ticks in the tickSize event handler.

            SubscriptionEntry entry;
            if (!_subscribedTickers.TryGetValue(e.TickerId, out entry))
            {
                return;
            }

            var symbol = entry.Symbol;

            // negative price (-1) means no price available, normalize to zero
            var price = e.Price < 0 ? 0 : Convert.ToDecimal(e.Price);

            switch (e.Field)
            {
                case IBApi.TickType.BID:
                case IBApi.TickType.DELAYED_BID:

                    if (entry.LastQuoteTick == null)
                    {
                        entry.LastQuoteTick = new Tick
                        {
                            // in the event of a symbol change this will break since we'll be assigning the
                            // new symbol to the permtick which won't be known by the algorithm
                            Symbol = symbol,
                            TickType = TickType.Quote
                        };
                    }

                    // set the last bid price
                    entry.LastQuoteTick.BidPrice = price;
                    break;

                case IBApi.TickType.ASK:
                case IBApi.TickType.DELAYED_ASK:

                    if (entry.LastQuoteTick == null)
                    {
                        entry.LastQuoteTick = new Tick
                        {
                            // in the event of a symbol change this will break since we'll be assigning the
                            // new symbol to the permtick which won't be known by the algorithm
                            Symbol = symbol,
                            TickType = TickType.Quote
                        };
                    }

                    // set the last ask price
                    entry.LastQuoteTick.AskPrice = price;
                    break;

                case IBApi.TickType.LAST:
                case IBApi.TickType.DELAYED_LAST:

                    if (entry.LastTradeTick == null)
                    {
                        entry.LastTradeTick = new Tick
                        {
                            // in the event of a symbol change this will break since we'll be assigning the
                            // new symbol to the permtick which won't be known by the algorithm
                            Symbol = symbol,
                            TickType = TickType.Trade
                        };
                    }

                    // set the last traded price
                    entry.LastTradeTick.Value = price;
                    break;

                default:
                    return;
            }
        }

        /// <summary>
        /// Modifies the quantity received from IB based on the security type
        /// </summary>
        public static int AdjustQuantity(SecurityType type, int size)
        {
            switch (type)
            {
                case SecurityType.Equity:
                    return size * 100;
                default:
                    return size;
            }
        }

        private void HandleTickSize(object sender, IB.TickSizeEventArgs e)
        {
            SubscriptionEntry entry;
            if (!_subscribedTickers.TryGetValue(e.TickerId, out entry))
            {
                return;
            }

            var symbol = entry.Symbol;

            var securityType = symbol.ID.SecurityType;

            // negative size (-1) means no quantity available, normalize to zero
            var quantity = e.Size < 0 ? 0 : AdjustQuantity(securityType, e.Size);

            Tick tick;
            switch (e.Field)
            {
                case IBApi.TickType.BID_SIZE:
                case IBApi.TickType.DELAYED_BID_SIZE:

                    tick = entry.LastQuoteTick;

                    if (tick == null)
                    {
                        // tick size message must be preceded by a tick price message
                        return;
                    }

                    tick.BidSize = quantity;

                    if (tick.BidPrice == 0)
                    {
                        // no bid price, do not emit tick
                        return;
                    }

                    if (tick.BidPrice > 0 && tick.AskPrice > 0 && tick.BidPrice >= tick.AskPrice)
                    {
                        // new bid price jumped at or above previous ask price, wait for new ask price
                        return;
                    }

                    if (tick.AskPrice == 0)
                    {
                        // we have a bid price but no ask price, use bid price as value
                        tick.Value = tick.BidPrice;
                    }
                    else
                    {
                        // we have both bid price and ask price, use mid price as value
                        tick.Value = (tick.BidPrice + tick.AskPrice) / 2;
                    }
                    break;

                case IBApi.TickType.ASK_SIZE:
                case IBApi.TickType.DELAYED_ASK_SIZE:

                    tick = entry.LastQuoteTick;

                    if (tick == null)
                    {
                        // tick size message must be preceded by a tick price message
                        return;
                    }

                    tick.AskSize = quantity;

                    if (tick.AskPrice == 0)
                    {
                        // no ask price, do not emit tick
                        return;
                    }

                    if (tick.BidPrice > 0 && tick.AskPrice > 0 && tick.BidPrice >= tick.AskPrice)
                    {
                        // new ask price jumped at or below previous bid price, wait for new bid price
                        return;
                    }

                    if (tick.BidPrice == 0)
                    {
                        // we have an ask price but no bid price, use ask price as value
                        tick.Value = tick.AskPrice;
                    }
                    else
                    {
                        // we have both bid price and ask price, use mid price as value
                        tick.Value = (tick.BidPrice + tick.AskPrice) / 2;
                    }
                    break;

                case IBApi.TickType.LAST_SIZE:
                case IBApi.TickType.DELAYED_LAST_SIZE:

                    tick = entry.LastTradeTick;

                    if (tick == null)
                    {
                        // tick size message must be preceded by a tick price message
                        return;
                    }

                    // set the traded quantity
                    tick.Quantity = quantity;
                    break;

                case IBApi.TickType.OPEN_INTEREST:
                case IBApi.TickType.OPTION_CALL_OPEN_INTEREST:
                case IBApi.TickType.OPTION_PUT_OPEN_INTEREST:

                    if (symbol.ID.SecurityType != SecurityType.Option && symbol.ID.SecurityType != SecurityType.Future)
                    {
                        return;
                    }

                    if (entry.LastOpenInterestTick == null)
                    {
                        entry.LastOpenInterestTick = new Tick { Symbol = symbol, TickType = TickType.OpenInterest };
                    }

                    tick = entry.LastOpenInterestTick;

                    tick.Value = e.Size;
                    break;

                default:
                    return;
            }

            if (tick.IsValid())
            {
                tick = new Tick(tick)
                {
                    Time = GetRealTimeTickTime(symbol)
                };

                _aggregator.Update(tick);

                if (_underlyings.ContainsKey(tick.Symbol))
                {
                    var underlyingTick = tick.Clone() as Tick;
                    underlyingTick.Symbol = _underlyings[tick.Symbol];
                    _aggregator.Update(underlyingTick);
                }
            }
        }

        /// <summary>
        /// Method returns a collection of Symbols that are available at the broker.
        /// </summary>
        /// <param name="lookupName">String representing the name to lookup</param>
        /// <param name="securityType">Expected security type of the returned symbols (if any)</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <param name="securityExchange">Expected security exchange name(if any)</param>
        /// <returns></returns>
        public IEnumerable<Symbol> LookupSymbols(string lookupName, SecurityType securityType, bool includeExpired, string securityCurrency = null, string securityExchange = null)
        {
            // setting up exchange defaults and filters
            var exchangeSpecifier = securityType == SecurityType.Future ? securityExchange ?? "" : securityExchange ?? "Smart";
            var futuresExchanges = _futuresExchanges.Values.Reverse().ToArray();
            Func<string, int> exchangeFilter = exchange => securityType == SecurityType.Future ? Array.IndexOf(futuresExchanges, exchange) : 0;

            // setting up lookup request
            var contract = new Contract
            {
                Symbol = _symbolMapper.GetBrokerageRootSymbol(lookupName),
                Currency = securityCurrency ?? Currencies.USD,
                Exchange = exchangeSpecifier,
                SecType = ConvertSecurityType(securityType),
                IncludeExpired = includeExpired
            };

            Log.Trace($"InteractiveBrokersBrokerage.LookupSymbols(): Requesting symbol list for {contract.Symbol} ...");

            var symbols = new List<Symbol>();

            if (securityType == SecurityType.Option)
            {
                // IB requests for full option chains are rate limited and responses can be delayed up to a minute for each underlying,
                // so we fetch them from the OCC website instead of using the IB API.
                var underlyingSymbol = Symbol.Create(contract.Symbol, SecurityType.Equity, Market.USA);
                symbols.AddRange(_algorithm.OptionChainProvider.GetOptionContractList(underlyingSymbol, DateTime.Today));
            }
            else if (securityType == SecurityType.Future)
            {
                // processing request
                var results = FindContracts(contract, contract.Symbol);

                // filtering results
                var filteredResults =
                    results
                        .Select(x => x.Contract)
                        .GroupBy(x => x.Exchange)
                        .OrderByDescending(g => exchangeFilter(g.Key))
                        .FirstOrDefault();

                if (filteredResults != null)
                {
                    symbols.AddRange(filteredResults.Select(MapSymbol));
                }
            }

            // Try to remove options or futures contracts that have expired
            if (!includeExpired)
            {
                if (securityType == SecurityType.Option || securityType == SecurityType.Future)
                {
                    var removedSymbols = symbols.Where(x => x.ID.Date < GetRealTimeTickTime(x).Date).ToHashSet();

                    if (symbols.RemoveAll(x => removedSymbols.Contains(x)) > 0)
                    {
                        Log.Trace("InteractiveBrokersBrokerage.LookupSymbols(): Removed contract(s) for having expiry in the past: {0}", string.Join(",", removedSymbols.Select(x => x.Value)));
                    }
                }
            }

            Log.Trace($"InteractiveBrokersBrokerage.LookupSymbols(): Returning {symbols.Count} contract(s) for {contract.Symbol}");

            return symbols;
        }

        /// <summary>
        /// Returns whether the time can be advanced or not.
        /// </summary>
        /// <param name="securityType">The security type</param>
        /// <returns>true if the time can be advanced</returns>
        public bool CanAdvanceTime(SecurityType securityType)
        {
            if (securityType == SecurityType.Future)
            {
                // we need to call the IB API only for futures
                return !_ibAutomater.IsWithinScheduledServerResetTimes() && IsConnected;
            }

            return true;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        /// <remarks>For IB history limitations see https://www.interactivebrokers.com/en/software/api/apiguide/tables/historical_data_limitations.htm </remarks>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            // skipping universe and canonical symbols
            if (!CanSubscribe(request.Symbol) ||
                (request.Symbol.ID.SecurityType == SecurityType.Option && request.Symbol.IsCanonical()) ||
                (request.Symbol.ID.SecurityType == SecurityType.Future && request.Symbol.IsCanonical()))
            {
                yield break;
            }

            // skip invalid security types
            if (request.Symbol.SecurityType != SecurityType.Equity &&
                request.Symbol.SecurityType != SecurityType.Forex &&
                request.Symbol.SecurityType != SecurityType.Cfd &&
                request.Symbol.SecurityType != SecurityType.Future &&
                request.Symbol.SecurityType != SecurityType.Option)
            {
                yield break;
            }

            // tick resolution not supported for now
            if (request.Resolution == Resolution.Tick)
            {
                // TODO: upgrade IB C# API DLL
                // In IB API version 973.04, the reqHistoricalTicks function has been added,
                // which would now enable us to support history requests at Tick resolution.
                yield break;
            }

            // preparing the data for IB request
            var contract = CreateContract(request.Symbol, true);
            var resolution = ConvertResolution(request.Resolution);
            var duration = ConvertResolutionToDuration(request.Resolution);
            var startTime = request.Resolution == Resolution.Daily ? request.StartTimeUtc.Date : request.StartTimeUtc;
            var endTime = request.Resolution == Resolution.Daily ? request.EndTimeUtc.Date : request.EndTimeUtc;

            Log.Trace($"InteractiveBrokersBrokerage::GetHistory(): Submitting request: {request.Symbol.Value} ({contract}): {request.Resolution}/{request.TickType} {startTime} UTC -> {endTime} UTC");

            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(request.Symbol, out exchangeTimeZone))
            {
                // read the exchange time zone from market-hours-database
                exchangeTimeZone = MarketHoursDatabase.FromDataFolder().GetExchangeHours(request.Symbol.ID.Market, request.Symbol, request.Symbol.SecurityType).TimeZone;
                _symbolExchangeTimeZones.Add(request.Symbol, exchangeTimeZone);
            }

            IEnumerable<BaseData> history;
            if (request.TickType == TickType.Quote)
            {
                // Quotes need two separate IB requests for Bid and Ask,
                // each pair of TradeBars will be joined into a single QuoteBar
                var historyBid = GetHistory(request, contract, startTime, endTime, exchangeTimeZone, duration, resolution, HistoricalDataType.Bid);
                var historyAsk = GetHistory(request, contract, startTime, endTime, exchangeTimeZone, duration, resolution, HistoricalDataType.Ask);

                history = historyBid.Join(historyAsk,
                    bid => bid.Time,
                    ask => ask.Time,
                    (bid, ask) => new QuoteBar(
                        bid.Time,
                        bid.Symbol,
                        new Bar(bid.Open, bid.High, bid.Low, bid.Close),
                        0,
                        new Bar(ask.Open, ask.High, ask.Low, ask.Close),
                        0,
                        bid.Period));
            }
            else
            {
                // other assets will have TradeBars
                history = GetHistory(request, contract, startTime, endTime, exchangeTimeZone, duration, resolution, HistoricalDataType.Trades);
            }

            // cleaning the data before returning it back to user
            var requestStartTime = request.StartTimeUtc.ConvertFromUtc(exchangeTimeZone);
            var requestEndTime = request.EndTimeUtc.ConvertFromUtc(exchangeTimeZone);

            foreach (var bar in history.Where(bar => bar.Time >= requestStartTime && bar.EndTime <= requestEndTime))
            {
                yield return bar;
            }

            Log.Trace($"InteractiveBrokersBrokerage::GetHistory(): Download completed: {request.Symbol.Value} ({contract})");
        }

        private IEnumerable<TradeBar> GetHistory(
            HistoryRequest request,
            Contract contract,
            DateTime startTime,
            DateTime endTime,
            DateTimeZone exchangeTimeZone,
            string duration,
            string resolution,
            string dataType)
        {
            const int timeOut = 60; // seconds timeout

            var history = new List<TradeBar>();
            var dataDownloading = new AutoResetEvent(false);
            var dataDownloaded = new AutoResetEvent(false);

            var useRegularTradingHours = Convert.ToInt32(!request.IncludeExtendedMarketHours);

            // making multiple requests if needed in order to download the history
            while (endTime >= startTime)
            {
                var pacing = false;
                var historyPiece = new List<TradeBar>();
                var historicalTicker = GetNextTickerId();

                _requestInformation[historicalTicker] = $"GetHistory: {request.Symbol.Value} ({contract})";

                EventHandler<IB.HistoricalDataEventArgs> clientOnHistoricalData = (sender, args) =>
                {
                    if (args.RequestId == historicalTicker)
                    {
                        var bar = ConvertTradeBar(request.Symbol, request.Resolution, args);
                        if (request.Resolution != Resolution.Daily)
                        {
                            bar.Time = bar.Time.ConvertFromUtc(exchangeTimeZone);
                        }

                        historyPiece.Add(bar);
                        dataDownloading.Set();
                    }
                };

                EventHandler<IB.HistoricalDataEndEventArgs> clientOnHistoricalDataEnd = (sender, args) =>
                {
                    if (args.RequestId == historicalTicker)
                    {
                        dataDownloaded.Set();
                    }
                };

                EventHandler<IB.ErrorEventArgs> clientOnError = (sender, args) =>
                {
                    if (args.Id == historicalTicker)
                    {
                        if (args.Code == 162 && args.Message.Contains("pacing violation"))
                        {
                            // pacing violation happened
                            pacing = true;
                        }
                        else
                        {
                            dataDownloaded.Set();
                        }
                    }
                };

                Client.Error += clientOnError;
                Client.HistoricalData += clientOnHistoricalData;
                Client.HistoricalDataEnd += clientOnHistoricalDataEnd;

                CheckRateLimiting();

                Client.ClientSocket.reqHistoricalData(historicalTicker, contract, endTime.ToStringInvariant("yyyyMMdd HH:mm:ss UTC"),
                    duration, resolution, dataType, useRegularTradingHours, 2, false, new List<TagValue>());

                var waitResult = 0;
                while (waitResult == 0)
                {
                    waitResult = WaitHandle.WaitAny(new WaitHandle[] { dataDownloading, dataDownloaded }, timeOut * 1000);
                }

                Client.Error -= clientOnError;
                Client.HistoricalData -= clientOnHistoricalData;
                Client.HistoricalDataEnd -= clientOnHistoricalDataEnd;

                if (waitResult == WaitHandle.WaitTimeout)
                {
                    if (pacing)
                    {
                        // we received 'pacing violation' error from IB. So we had to wait
                        Log.Trace("InteractiveBrokersBrokerage::GetHistory() Pacing violation. Paused for {0} secs.", timeOut);
                        continue;
                    }

                    Log.Trace("InteractiveBrokersBrokerage::GetHistory() History request timed out ({0} sec)", timeOut);
                    break;
                }

                // if no data has been received this time, we exit
                if (!historyPiece.Any())
                {
                    break;
                }

                var filteredPiece = historyPiece.OrderBy(x => x.Time);

                history.InsertRange(0, filteredPiece);

                // moving endTime to the new position to proceed with next request (if needed)
                endTime = filteredPiece.First().Time;
            }

            return history;
        }

        /// <summary>
        /// Returns whether the brokerage should perform the cash synchronization
        /// </summary>
        /// <param name="currentTimeUtc">The current time (UTC)</param>
        /// <returns>True if the cash sync should be performed</returns>
        public override bool ShouldPerformCashSync(DateTime currentTimeUtc)
        {
            return base.ShouldPerformCashSync(currentTimeUtc) &&
                   !_ibAutomater.IsWithinScheduledServerResetTimes();
        }

        private void CheckRateLimiting()
        {
            if (!_messagingRateLimiter.WaitToProceed(TimeSpan.Zero))
            {
                Log.Trace("The IB API request has been rate limited.");

                _messagingRateLimiter.WaitToProceed();
            }
        }

        private void OnIbAutomaterOutputDataReceived(object sender, OutputDataReceivedEventArgs e)
        {
            if (e.Data == null) return;

            Log.Trace($"InteractiveBrokersBrokerage.OnIbAutomaterOutputDataReceived(): {e.Data}");
        }

        private void OnIbAutomaterErrorDataReceived(object sender, ErrorDataReceivedEventArgs e)
        {
            if (e.Data == null) return;

            Log.Trace($"InteractiveBrokersBrokerage.OnIbAutomaterErrorDataReceived(): {e.Data}");
        }

        private void OnIbAutomaterExited(object sender, ExitedEventArgs e)
        {
            Log.Trace($"InteractiveBrokersBrokerage.OnIbAutomaterExited(): Exit code: {e.ExitCode}");

            // check if IBGateway was closed because of an IBAutomater error
            var result = _ibAutomater.GetLastStartResult();
            CheckIbAutomaterError(result, false);

            if (!result.HasError)
            {
                // IBGateway was closed by the v978+ automatic logoff or it was closed manually (less likely)
                Log.Trace("InteractiveBrokersBrokerage.OnIbAutomaterExited(): IBGateway close detected, restarting IBAutomater and reconnecting...");

                Disconnect();
                CheckIbAutomaterError(_ibAutomater.Start(false));
                Connect();
            }
        }

        private void CheckIbAutomaterError(StartResult result, bool throwException = true)
        {
            if (result.HasError)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, result.ErrorCode.ToString(), result.ErrorMessage));

                if (throwException)
                {
                    throw new Exception($"InteractiveBrokersBrokerage.CheckIbAutomaterError(): {result.ErrorCode} - {result.ErrorMessage}");
                }
            }
        }

        private readonly ConcurrentDictionary<Symbol, int> _subscribedSymbols = new ConcurrentDictionary<Symbol, int>();
        private readonly ConcurrentDictionary<int, SubscriptionEntry> _subscribedTickers = new ConcurrentDictionary<int, SubscriptionEntry>();
        private readonly Dictionary<Symbol, Symbol> _underlyings = new Dictionary<Symbol, Symbol>();

        private class SubscriptionEntry
        {
            public Symbol Symbol { get; set; }
            public Tick LastTradeTick { get; set; }
            public Tick LastQuoteTick { get; set; }
            public Tick LastOpenInterestTick { get; set; }
        }

        private static class AccountValueKeys
        {
            public const string CashBalance = "CashBalance";
            // public const string AccruedCash = "AccruedCash";
            // public const string NetLiquidationByCurrency = "NetLiquidationByCurrency";
        }

        // these are fatal errors from IB
        private static readonly HashSet<int> ErrorCodes = new HashSet<int>
        {
            100, 101, 103, 138, 139, 142, 143, 144, 145, 200, 203, 300,301,302,306,308,309,310,311,316,317,320,321,322,323,324,326,327,330,331,332,333,344,346,354,357,365,366,381,384,401,414,431,432,438,501,502,503,504,505,506,507,508,510,511,512,513,514,515,516,517,518,519,520,521,522,523,524,525,526,527,528,529,530,531,10000,10001,10005,10013,10015,10016,10021,10022,10023,10024,10025,10026,10027,1300
        };

        // these are warning messages from IB
        private static readonly HashSet<int> WarningCodes = new HashSet<int>
        {
            102, 104, 105, 106, 107, 109, 110, 111, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 129, 131, 132, 133, 134, 135, 136, 137, 140, 141, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 201, 303,313,314,315,319,325,328,329,334,335,336,337,338,339,340,341,342,343,345,347,348,349,350,352,353,355,356,358,359,360,361,362,363,364,367,368,369,370,371,372,373,374,375,376,377,378,379,380,382,383,385,386,387,388,389,390,391,392,393,394,395,396,397,398,399,400,402,403,404,405,406,407,408,409,410,411,412,413,417,418,419,420,421,422,423,424,425,426,427,428,429,430,433,434,435,436,437,439,440,441,442,443,444,445,446,447,448,449,450,1100,10002,10003,10006,10007,10008,10009,10010,10011,10012,10014,10018,10019,10020,10052,10147,10148,10149,1101,1102,2100,2101,2102,2103,2105,2109,2110,2148
        };

        // these require us to issue invalidated order events
        private static readonly HashSet<int> InvalidatingCodes = new HashSet<int>
        {
            105, 106, 107, 109, 110, 111, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 129, 131, 132, 133, 134, 135, 136, 137, 140, 141, 146, 147, 148, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 167, 168, 201, 313,314,315,325,328,329,334,335,336,337,338,339,340,341,342,343,345,347,348,349,350,352,353,355,356,358,359,360,361,362,363,364,367,368,369,370,371,372,373,374,375,376,377,378,379,380,382,383,387,388,389,390,391,392,393,394,395,396,397,398,400,401,402,403,405,406,407,408,409,410,411,412,413,417,418,419,421,423,424,427,428,429,433,434,435,436,437,439,440,441,442,443,444,445,446,447,448,449,10002,10006,10007,10008,10009,10010,10011,10012,10014,10020,2102
        };
    }

}
