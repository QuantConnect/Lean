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
using System.Collections.ObjectModel;
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
using IB = Krs.Ats.IBNet;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// The Interactive Brokers brokerage
    /// </summary>
    public sealed class InteractiveBrokersBrokerage : Brokerage, IDataQueueHandler
    {
        // next valid order id for this client
        private int _nextValidID;
        // next valid client id for the gateway/tws
        private static int _nextClientID = 0;
        // next valid request id for queries
        private int _nextRequestID = 0;
        private int _nextTickerID = 0;
        private volatile bool _disconnected1100Fired = false;

        private readonly int _port;
        private readonly string _account;
        private readonly string _host;
        private readonly int _clientID;
        private readonly IOrderProvider _orderProvider;
        private ISecurityProvider _securityProvider;
        private readonly IB.IBClient _client;
        private readonly IB.AgentDescription _agentDescription;

        private readonly ManualResetEvent _waitForNextValidID = new ManualResetEvent(false);
        private readonly ManualResetEvent _accountHoldingsResetEvent = new ManualResetEvent(false);

        // IB likes to duplicate/triplicate some events, keep track of them and swallow the dupes
        // we're keeping track of the .ToString() of the order event here
        private readonly FixedSizeHashQueue<string> _recentOrderEvents = new FixedSizeHashQueue<string>(50);

        private readonly object _orderFillsLock = new object();
        private readonly ConcurrentDictionary<int, int> _orderFills = new ConcurrentDictionary<int, int>(); 
        private readonly ConcurrentDictionary<string, decimal> _cashBalances = new ConcurrentDictionary<string, decimal>(); 
        private readonly ConcurrentDictionary<string, string> _accountProperties = new ConcurrentDictionary<string, string>();
        // number of shares per symbol
        private readonly ConcurrentDictionary<string, Holding> _accountHoldings = new ConcurrentDictionary<string, Holding>();

        private readonly ConcurrentDictionary<string, IB.ContractDetails> _contractDetails = new ConcurrentDictionary<string, IB.ContractDetails>();

        private readonly InteractiveBrokersSymbolMapper _symbolMapper = new InteractiveBrokersSymbolMapper();

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
        /// <param name="orderProvider">An instance of IOrderProvider used to fetch Order objects by brokerage ID</param>
        /// <param name="securityProvider">The security provider used to give access to algorithm securities</param>
        public InteractiveBrokersBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
            : this(
                orderProvider,
                securityProvider,
                Config.Get("ib-account"),
                Config.Get("ib-host", "LOCALHOST"),
                Config.GetInt("ib-port", 4001),
                Config.GetValue("ib-agent-description", IB.AgentDescription.Individual)
                )
        {
        }

        /// <summary>
        /// Creates a new InteractiveBrokersBrokerage for the specified account
        /// </summary>
        /// <param name="orderProvider">An instance of IOrderProvider used to fetch Order objects by brokerage ID</param>
        /// <param name="securityProvider">The security provider used to give access to algorithm securities</param>
        /// <param name="account">The account used to connect to IB</param>
        public InteractiveBrokersBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, string account)
            : this(orderProvider,
                securityProvider,
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
        /// <param name="orderProvider">An instance of IOrderProvider used to fetch Order objects by brokerage ID</param>
        /// <param name="securityProvider">The security provider used to give access to algorithm securities</param>
        /// <param name="account">The Interactive Brokers account name</param>
        /// <param name="host">host name or IP address of the machine where TWS is running. Leave blank to connect to the local host.</param>
        /// <param name="port">must match the port specified in TWS on the Configure&gt;API&gt;Socket Port field.</param>
        /// <param name="agentDescription">Used for Rule 80A describes the type of trader.</param>
        public InteractiveBrokersBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, string account, string host, int port, IB.AgentDescription agentDescription = IB.AgentDescription.Individual)
            : base("Interactive Brokers Brokerage")
        {
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
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
            _client.TickPrice += HandleTickPrice;
            _client.TickSize += HandleTickSize;
            _client.CurrentTime += HandleBrokerTime;

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
                Log.Trace("InteractiveBrokersBrokerage.PlaceOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

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

                IBPlaceOrder(order, false);
            }
            catch (Exception err)
            {
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

                // this could be better
                foreach (var id in order.BrokerId)
                {
                    _client.CancelOrder(int.Parse(id));
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
            if (!manualResetEvent.WaitOne(15000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetOpenOrders(): Operation took longer than 15 seconds.");
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
            var holdings = _accountHoldings.Select(x => (Holding) ObjectActivator.Clone(x.Value)).Where(x => x.Quantity != 0).ToList();

            // fire up tasks to resolve the conversion rates so we can do them in parallel
            var tasks = holdings.Select(local =>
            {
                // we need to resolve the conversion rate for non-USD currencies
                if (local.Type != SecurityType.Forex)
                {
                    // this assumes all non-forex are us denominated, we should add the currency to 'holding'
                    local.ConversionRate = 1m;
                    return null;
                }
                // if quote currency is in USD don't bother making the request
                string currency = local.Symbol.Value.Substring(3);
                if (currency == "USD")
                {
                    local.ConversionRate = 1m;
                    return null;
                }

                // this will allow us to do this in parallel
                return Task.Factory.StartNew(() => local.ConversionRate = GetUsdConversion(currency));
            }).Where(x => x != null).ToArray();

            Task.WaitAll(tasks, 5000);

            return holdings;
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<Cash> GetCashBalance()
        {
            return _cashBalances.Select(x => new Cash(x.Key, x.Value, GetUsdConversion(x.Key))).ToList();
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
                Time = timeSince ?? DateTime.MinValue,
                Side = side ?? IB.ActionSide.Undefined
            };

            var details = new List<IB.ExecDetailsEventArgs>();
            using (var client = new IB.IBClient())
            {
                client.Connect(_host, _port, IncrementClientID());

                var manualResetEvent = new ManualResetEvent(false);

                int requestID = GetNextRequestID();

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
            const int maxAttempts = 65;
            while (true)
            {
                try
                {
                    Log.Trace("InteractiveBrokersBrokerage.Connect(): Attempting to connect ({0}/{1}) ...", attempt, maxAttempts);

                    // we're going to try and connect several times, if successful break
                    _client.Connect(_host, _port, _clientID);

                    if (!_client.Connected) throw new Exception("InteractiveBrokersBrokerage.Connect(): Connection returned but was not in connected state.");
                    break;
                }
                catch (Exception err)
                {
                    // max out at 65 attempts to connect ~1 minute
                    if (attempt++ < maxAttempts)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    // we couldn't connect after several attempts, log the error and throw an exception
                    Log.Error(err);

                    // add a blurb about TWS for connection refused errors
                    if (err.Message.Contains("Connection refused"))
                    {
                        throw new Exception(err.Message + ". Be sure to logout of Trader Workstation. " +
                            "IB only allows one active log in at a time. " +
                            "This can also be caused by requiring two-factor authentication. " +
                            "Be sure to disable this in IB Account Management > Security > SLS Opt out.", err);
                    }

                    throw;
                }
            }

            // pause for a moment to receive next valid ID message from gateway
            if (!_waitForNextValidID.WaitOne(15000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.Connect(): Operation took longer than 15 seconds.");
            }

            // define our event handler, this acts as stop to make sure when we leave Connect we have downloaded the full account
            EventHandler<IB.AccountDownloadEndEventArgs> clientOnAccountDownloadEnd = (sender, args) =>
            {
                Log.Trace("InteractiveBrokersBrokerage.AccountDownloadEnd(): Finished account download for " + args.AccountName);
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
            _client.RequestAccountUpdates(true, _account);

            // wait to see the first account value update
            firstAccountUpdateReceived.WaitOne(2500);

            // take pause to ensure the account is downloaded before continuing, this was added because running in
            // linux there appears to be different behavior where the account download end fires immediately.
            Thread.Sleep(2500);

            if (!_accountHoldingsResetEvent.WaitOne(15000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetAccountHoldings(): Operation took longer than 15 seconds.");
            }

            // remove our end handler
            _client.AccountDownloadEnd -= clientOnAccountDownloadEnd;
            _client.UpdateAccountValue -= clientOnUpdateAccountValue;
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
        /// Gets the raw account values sent from IB
        /// </summary>
        public Dictionary<string, string> GetAccountValues()
        {
            return new Dictionary<string, string>(_accountProperties);
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

            var contract = CreateContract(order.Symbol, exchange);

            int ibOrderID = 0;
            if (needsNewID)
            {
                // the order ids are generated for us by the SecurityTransactionManaer
                int id = GetNextBrokerageOrderID();
                order.BrokerId.Add(id.ToString());
                ibOrderID = id;
            }
            else if (order.BrokerId.Any())
            {
                // this is *not* perfect code
                ibOrderID = int.Parse(order.BrokerId[0]);
            }
            else
            {
                throw new ArgumentException("Expected order with populated BrokerId for updating orders.");
            }

            var ibOrder = ConvertOrder(order, contract, ibOrderID);
            _client.PlaceOrder(ibOrder.OrderId, contract, ibOrder);
        }

        private string GetPrimaryExchange(IB.Contract contract)
        {
            IB.ContractDetails details;
            if (_contractDetails.TryGetValue(contract.Symbol, out details))
            {
                return details.Summary.PrimaryExchange;
            }

            details = GetContractDetails(contract);
            if (details == null)
            {
                // we were unable to find the contract details
                return null;
            }

            return details.Summary.PrimaryExchange;
        }

        private decimal GetMinTick(IB.Contract contract)
        {
            IB.ContractDetails details;
            if (_contractDetails.TryGetValue(contract.Symbol, out details))
            {
                return (decimal) details.MinTick;
            }

            details = GetContractDetails(contract);
            if (details == null)
            {
                // we were unable to find the contract details
                return 0;
            }

            return (decimal) details.MinTick;
        }

        private IB.ContractDetails GetContractDetails(IB.Contract contract)
        {
            IB.ContractDetails details = null;
            var requestID = GetNextRequestID();

            var manualResetEvent = new ManualResetEvent(false);

            // define our event handlers
            EventHandler<IB.ContractDetailsEventArgs> clientOnContractDetails = (sender, args) =>
            {
                // ignore other requests
                if (args.RequestId != requestID) return;
                details = args.ContractDetails;
                _contractDetails.TryAdd(contract.Symbol, details);
                manualResetEvent.Set();
            };

            _client.ContractDetails += clientOnContractDetails;

            // make the request for data
            _client.RequestContractDetails(requestID, contract);

            // we'll wait a second, but it may not exist so just pass through
            manualResetEvent.WaitOne(1000);

            // be sure to remove our event handlers
            _client.ContractDetails -= clientOnContractDetails;

            return details;
        }

        /// <summary>
        /// Gets the current conversion rate into USD
        /// </summary>
        /// <remarks>Synchronous, blocking</remarks>
        private decimal GetUsdConversion(string currency)
        {
            if (currency == "USD")
            {
                return 1m;
            }

            // determine the correct symbol to choose
            string invertedSymbol = "USD" + currency;
            string normalSymbol = currency + "USD";
            var currencyPair = Currencies.CurrencyPairs.FirstOrDefault(x => x == invertedSymbol || x == normalSymbol);
            if (currencyPair == null)
            {
                throw new Exception("Unable to resolve currency conversion pair for currency: " + currency);
            }

            // is it XXXUSD or USDXXX
            bool inverted = invertedSymbol == currencyPair;
            var symbol = Symbol.Create(currencyPair, SecurityType.Forex, Market.FXCM);
            var contract = CreateContract(symbol);
            var details = GetContractDetails(contract);
            if (details == null)
            {
                Log.Error("InteractiveBrokersBrokerage.GetUsdConversion(): Unable to resolve conversion for currency: " + currency);
                return 1m;
            }

            // if this stays zero then we haven't received the conversion rate
            var rate = 0m; 
            var manualResetEvent = new ManualResetEvent(false);

            // we're going to request both history and active ticks, we'll use the ticks first
            // and if not present, we'll use the latest from the history request

            var data = new List<IB.HistoricalDataEventArgs>();
            int historicalTicker = GetNextTickerID();
            var lastHistoricalData = DateTime.MaxValue;
            EventHandler<IB.HistoricalDataEventArgs> clientOnHistoricalData = (sender, args) =>
            {
                if (args.RequestId == historicalTicker)
                {
                    data.Add(args);
                    lastHistoricalData = DateTime.UtcNow;
                }
            };

            _client.HistoricalData += clientOnHistoricalData;

            // request some historical data, IB's api takes into account weekends/market opening hours
            var requestSpan = TimeSpan.FromSeconds(100);
            _client.RequestHistoricalData(historicalTicker, contract, DateTime.UtcNow, requestSpan, IB.BarSize.OneSecond, IB.HistoricalDataType.Ask, 0);

            // define and add our tick handler for the ticks
            var marketDataTicker = GetNextTickerID();
            var priceTick = new Collection<IB.GenericTickType>();
            EventHandler<IB.TickPriceEventArgs> clientOnTickPrice = (sender, args) =>
            {
                if (args.TickerId == marketDataTicker && args.TickType == IB.TickType.AskPrice)
                {
                    rate = args.Price;
                    manualResetEvent.Set();
                }
            };

            _client.TickPrice += clientOnTickPrice;

            _client.RequestMarketData(marketDataTicker, contract, priceTick, true, false);

            manualResetEvent.WaitOne(2500);

            _client.TickPrice -= clientOnTickPrice;

            // check to see if ticks returned something
            if (rate == 0)
            {
                // history doesn't have a completed event, so we'll just wait for it to not have been called for a second
                while (DateTime.UtcNow - lastHistoricalData < Time.OneSecond) Thread.Sleep(20);

                // check for history
                var ordered = data.OrderByDescending(x => x.Date);
                var mostRecentQuote = ordered.FirstOrDefault();
                if (mostRecentQuote == null)
                {
                    throw new Exception("Unable to get recent quote for " + currencyPair);
                }
                rate = mostRecentQuote.Close;
            }

            // be sure to unwire our history handler as well
            _client.HistoricalData -= clientOnHistoricalData;

            if (inverted)
            {
                return 1/rate;
            }
            return rate;
        }

        /// <summary>
        /// Handles error messages from IB
        /// </summary>
        private void HandleError(object sender, IB.ErrorEventArgs e)
        {
            // https://www.interactivebrokers.com/en/software/api/apiguide/tables/api_message_codes.htm

            // rewrite these messages to be single lined
            e.ErrorMsg = e.ErrorMsg.Replace("\r\n", ". ").Replace("\r", ". ").Replace("\n", ". ");
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

            // code 1100 is a connection failure, we'll wait a minute before exploding gracefully
            if ((int) e.ErrorCode == 1100 && !_disconnected1100Fired)
            {
                _disconnected1100Fired = true;

                // begin the try wait logic
                TryWaitForReconnect();
            }
            else if ((int) e.ErrorCode == 1102)
            {
                // we've reconnected
                _disconnected1100Fired = false;
                OnMessage(BrokerageMessageEvent.Reconnected(e.ErrorMsg));
            }

            if (InvalidatingCodes.Contains((int)e.ErrorCode))
            {
                Log.Trace(string.Format("InteractiveBrokersBrokerage.HandleError.InvalidateOrder(): Order: {0} ErrorCode: {1} - {2}", e.TickerId, e.ErrorCode, e.ErrorMsg));

                // invalidate the order
                var order = _orderProvider.GetOrderByBrokerageId(e.TickerId);
                const int orderFee = 0;
                var orderEvent = new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Invalid };
                OnOrderEvent(orderEvent);
            }

            OnMessage(new BrokerageMessageEvent(brokerageMessageType, (int) e.ErrorCode, e.ErrorMsg));
        }

        /// <summary>
        /// If we lose connection to TWS/IB servers we don't want to send the Error event if it is within
        /// the scheduled server reset times
        /// </summary>
        private void TryWaitForReconnect()
        {
            // IB has server reset schedule: https://www.interactivebrokers.com/en/?f=%2Fen%2Fsoftware%2FsystemStatus.php%3Fib_entity%3Dllc
            
            if (_disconnected1100Fired && !IsWithinScheduledServerResetTimes())
            {
                // if we were disconnected and we're nothing within the reset times, send the error event
                OnMessage(BrokerageMessageEvent.Disconnected("Connection with Interactive Brokers lost. " +
                    "This could be because of internet connectivity issues or a log in from another location."
                    ));
            }
            else if (_disconnected1100Fired && IsWithinScheduledServerResetTimes())
            {
                Log.Trace("InteractiveBrokersBrokerage.TryWaitForReconnect(): Within server reset times, trying to wait for reconnect...");
                // we're still not connected but we're also within the schedule reset time, so just keep polling
                Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => TryWaitForReconnect());
            }
        }

        /// <summary>
        /// Stores all the account values
        /// </summary>
        private void HandleUpdateAccountValue(object sender, IB.UpdateAccountValueEventArgs e)
        {
            //https://www.interactivebrokers.com/en/software/api/apiguide/activex/updateaccountvalue.htm

            try
            {
                _accountProperties[e.Currency + ":" + e.Key] = e.Value;

                // we want to capture if the user's cash changes so we can reflect it in the algorithm
                if (e.Key == AccountValueKeys.CashBalance && e.Currency != "BASE")
                {
                    var cashBalance = decimal.Parse(e.Value, CultureInfo.InvariantCulture);
                    _cashBalances.AddOrUpdate(e.Currency, cashBalance);
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
                var order = _orderProvider.GetOrderByBrokerageId(update.OrderId);
                if (order == null)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Unable to locate order with BrokerageID " + update.OrderId);
                    return;
                }


                var status = ConvertOrderStatus(update.Status);
                if (order.Status == OrderStatus.Filled && update.Filled == 0 && update.Remaining == 0)
                {
                    // we're done with this order, remove from our state
                    int value;
                    _orderFills.TryRemove(order.Id, out value);
                }

                var orderFee = 0m;
                int filledThisTime;
                lock (_orderFillsLock)
                {
                    // lock since we're getting and updating in multiple operations
                    var currentFilled = _orderFills.GetOrAdd(order.Id, 0);
                    if (currentFilled == 0)
                    {
                        // apply order fees on the first fill event TODO: What about partial filled orders that get cancelled?
                        var security = _securityProvider.GetSecurity(order.Symbol);
                        orderFee = security.FeeModel.GetOrderFee(security, order);
                    }
                    filledThisTime = update.Filled - currentFilled;
                    _orderFills.AddOrUpdate(order.Id, currentFilled, (sym, filled) => update.Filled);
                }

                if (status == OrderStatus.Invalid)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): ERROR -- " + update.OrderId);
                }

                // set status based on filled this time
                if (filledThisTime != 0)
                {
                    status = update.Remaining != 0 ? OrderStatus.PartiallyFilled : OrderStatus.Filled;
                }
                // don't send empty fill events
                else if (status == OrderStatus.PartiallyFilled || status == OrderStatus.Filled)
                {
                    Log.Trace("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Ignored zero fill event: OrderId: " + update.OrderId + " Remaining: " + update.Remaining);
                    return;
                }

                // mark sells as negative quantities
                var fillQuantity = order.Direction == OrderDirection.Buy ? filledThisTime : -filledThisTime;
                order.PriceCurrency = _securityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;
                var orderEvent = new OrderEvent(order, DateTime.UtcNow, orderFee, "Interactive Brokers Fill Event")
                {
                    Status = status,
                    FillPrice = update.LastFillPrice,
                    FillQuantity = fillQuantity
                };
                if (update.Remaining != 0)
                {
                    orderEvent.Message += " - " + update.Remaining + " remaining";
                }

                // if we're able to add to our fixed length, unique queue then send the event
                // otherwise it is a duplicate, so skip it
                if (_recentOrderEvents.Add(orderEvent.ToString() + update.Remaining))
                {
                    OnOrderEvent(orderEvent);
                }
            }
            catch(InvalidOperationException err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Unable to resolve executions for BrokerageID: " + update.OrderId + " - " + err);
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): " + err);
            }
        }

        /// <summary>
        /// Handle portfolio changed events from IB
        /// </summary>
        private void HandlePortfolioUpdates(object sender, IB.UpdatePortfolioEventArgs e)
        {
            _accountHoldingsResetEvent.Reset();
            var holding = CreateHolding(e);
            _accountHoldings[holding.Symbol.Value] = holding;
        }

        /// <summary>
        /// Converts a QC order to an IB order
        /// </summary>
        private IB.Order ConvertOrder(Order order, IB.Contract contract, int ibOrderID)
        {
            var ibOrder = new IB.Order
            {
                ClientId = _clientID,
                OrderId = ibOrderID,
                Account = _account,
                Action = ConvertOrderDirection(order.Direction),
                TotalQuantity = Math.Abs(order.Quantity),
                OrderType = ConvertOrderType(order.Type),
                AllOrNone = false,
                Tif = IB.TimeInForce.GoodTillCancel,
                Transmit = true,
                Rule80A = _agentDescription
            };

            if (order.Type == OrderType.MarketOnOpen)
            {
                ibOrder.Tif = IB.TimeInForce.MarketOnOpen;
            }

            var limitOrder = order as LimitOrder;
            var stopMarketOrder = order as StopMarketOrder;
            var stopLimitOrder = order as StopLimitOrder;
            if (limitOrder != null)
            {
                ibOrder.LimitPrice = RoundPrice(limitOrder.LimitPrice, GetMinTick(contract));
            }
            else if (stopMarketOrder != null)
            {
                ibOrder.AuxPrice = RoundPrice(stopMarketOrder.StopPrice, GetMinTick(contract));
            }
            else if (stopLimitOrder != null)
            {
                var minTick = GetMinTick(contract);
                ibOrder.LimitPrice = RoundPrice(stopLimitOrder.LimitPrice, minTick);
                ibOrder.AuxPrice = RoundPrice(stopLimitOrder.StopPrice, minTick);
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
            var orderType = ConvertOrderType(ibOrder);
            switch (orderType)
            {
                case OrderType.Market:
                    order = new MarketOrder(mappedSymbol,
                        ibOrder.TotalQuantity,
                        new DateTime() // not sure how to get this data
                        );
                    break;

                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(mappedSymbol, 
                        ibOrder.TotalQuantity,
                        new DateTime());
                    break;

                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(mappedSymbol,
                        ibOrder.TotalQuantity,
                        new DateTime()
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

            order.BrokerId.Add(ibOrder.OrderId.ToString());

            return order;
        }

        /// <summary>
        /// Creates an IB contract from the order.
        /// </summary>
        /// <param name="symbol">The symbol whose contract we need to create</param>
        /// <param name="exchange">The exchange where the order will be placed, defaults to 'Smart'</param>
        /// <returns>A new IB contract for the order</returns>
        private IB.Contract CreateContract(Symbol symbol, string exchange = null)
        {
            var securityType = ConvertSecurityType(symbol.ID.SecurityType);
            var ibSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var contract = new IB.Contract(ibSymbol, exchange ?? "Smart", securityType, "USD");
            if (symbol.ID.SecurityType == SecurityType.Forex)
            {
                // forex is special, so rewrite some of the properties to make it work
                contract.Exchange = "IDEALPRO";
                contract.Symbol = ibSymbol.Substring(0, 3);
                contract.Currency = ibSymbol.Substring(3);
            }

            if (symbol.ID.SecurityType == SecurityType.Option)
            {
                contract.Expiry = symbol.ID.Date.ToString(DateFormat.EightCharacter);
                contract.Right = symbol.ID.OptionRight == OptionRight.Call ? IB.RightType.Call : IB.RightType.Put;
                contract.Strike = Convert.ToDouble(symbol.ID.StrikePrice);
                contract.Symbol = symbol.ID.Symbol;
            }

            // some contracts require this, such as MSFT
            contract.PrimaryExchange = GetPrimaryExchange(contract);

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
        private OrderType ConvertOrderType(IB.Order order)
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
                    throw new InvalidEnumArgumentException("order.OrderType", (int)order.OrderType, typeof(OrderType));
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
        private static IB.SecurityType ConvertSecurityType(SecurityType type)
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
        private static SecurityType ConvertSecurityType(IB.SecurityType type)
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
            string currencySymbol;
            if (!Currencies.CurrencySymbols.TryGetValue(e.Contract.Currency, out currencySymbol))
            {
                currencySymbol = "$";
            }

            return new Holding
            {
                Symbol = MapSymbol(e.Contract),
                Type = ConvertSecurityType(e.Contract.SecurityType),
                Quantity = e.Position,
                AveragePrice = e.AverageCost,
                MarketPrice = e.MarketPrice,
                ConversionRate = 1m, // this will be overwritten when GetAccountHoldings is called to ensure fresh values
                CurrencySymbol =  currencySymbol
            };
        }

        /// <summary>
        /// Maps the IB Contract's symbol to a QC symbol
        /// </summary>
        private Symbol MapSymbol(IB.Contract contract)
        {
            var securityType = ConvertSecurityType(contract.SecurityType);
            var ibSymbol = securityType == SecurityType.Forex ? contract.Symbol + contract.Currency : contract.Symbol;
            var market = securityType == SecurityType.Forex ? Market.FXCM : Market.USA;

            return _symbolMapper.GetLeanSymbol(ibSymbol, securityType, market);
        }

        private decimal RoundPrice(decimal input, decimal minTick)
        {
            if (minTick == 0) return minTick;
            return Math.Round(input/minTick)*minTick;
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

        private int GetNextRequestID()
        {
            return Interlocked.Increment(ref _nextRequestID);
        }

        private int GetNextTickerID()
        {
            return Interlocked.Increment(ref _nextTickerID);
        }

        /// <summary>
        /// Increments the client ID for communication with the gateway
        /// </summary>
        private static int IncrementClientID()
        {
            return Interlocked.Increment(ref _nextClientID);
        }

        /// <summary>
        /// This function is used to decide whether or not we should kill an algorithm
        /// when we lose contact with IB servers. IB performs server resets nightly
        /// and on Fridays they take everything down, so we'll prevent killing algos
        /// on Saturdays completely for the time being.
        /// </summary>
        private static bool IsWithinScheduledServerResetTimes()
        {
            bool result;
            var time = DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork);
            
            // don't kill algos on Saturdays if we don't have a connection
            if (time.DayOfWeek == DayOfWeek.Saturday)
            {
                result = true;
            }
            else
            {
                var timeOfDay = time.TimeOfDay;
                // from 11:45 -> 12:45 is the IB reset times, we'll go from 11:00pm->1:30am for safety margin
                result = timeOfDay > new TimeSpan(23, 0, 0) || timeOfDay < new TimeSpan(1, 30, 0);
            }

            Log.Trace("InteractiveBrokersBrokerage.IsWithinScheduledServerRestTimes(): " + result);

            return result;
        }

        private DateTime GetBrokerTime()
        {
            return DateTime.UtcNow.ConvertFromUtc(TimeZones.NewYork).Add(_brokerTimeDiff);
        }
        void HandleBrokerTime(object sender, IB.CurrentTimeEventArgs e)
        {
            // keep track of clock drift
            _brokerTimeDiff = e.Time.Subtract(DateTime.UtcNow);
        }
        TimeSpan _brokerTimeDiff = new TimeSpan(0);


        /// <summary>
        /// IDataQueueHandler interface implementaion 
        /// </summary>
        /// 
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (_ticks)
            {
                var copy = _ticks.ToArray();
                _ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols.Where(CanSubscribe))
            {
                var id = GetNextRequestID();
                var contract = CreateContract(symbol);
                Client.RequestMarketData(id, contract, null, false, false);

                _subscribedSymbols[symbol] = id;
                _subscribedTickets[id] = symbol;
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                var res = default(int);

                if (_subscribedSymbols.TryRemove(symbol, out res))
                {
                    Client.CancelMarketData(res);

                    var secRes = default(Symbol);
                    _subscribedTickets.TryRemove(res, out secRes);
                }
            }
        }

        /// <summary>
        /// Returns true if this brokerage supports the specified symbol
        /// </summary>
        private static bool CanSubscribe(Symbol symbol)
        {
            // ignore unsupported security types
            if (symbol.ID.SecurityType != SecurityType.Equity && symbol.ID.SecurityType != SecurityType.Forex &&
                symbol.ID.SecurityType != SecurityType.Option && symbol.ID.SecurityType != SecurityType.Future)
                return false;

            // ignore universe symbols
            return !symbol.Value.Contains("-UNIVERSE-");
        }

        void HandleTickPrice(object sender, IB.TickPriceEventArgs e)
        {
            var symbol = default(Symbol);

            if (!_subscribedTickets.TryGetValue(e.TickerId, out symbol)) return;

            var tick = new Tick();
            // in the event of a symbol change this will break since we'll be assigning the
            // new symbol to the permtick which won't be known by the algorithm
            tick.Symbol = symbol;
            tick.Time = GetBrokerTime();
            var securityType = symbol.ID.SecurityType;
            if (securityType == SecurityType.Forex)
            {
                // forex exchange hours are specified in UTC-05
                tick.Time = tick.Time.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
            }
            tick.Value = e.Price;

            if (e.Price <= 0 &&
                securityType != SecurityType.Future &&
                securityType != SecurityType.Option)
                return;

            switch (e.TickType)
            {
                case IB.TickType.BidPrice:

                    tick.TickType = TickType.Quote;
                    tick.BidPrice = e.Price;
                    _lastBidSizes.TryGetValue(symbol, out tick.Quantity);
                    _lastBidPrices[symbol] = e.Price;
                    break;

                case IB.TickType.AskPrice:

                    tick.TickType = TickType.Quote;
                    tick.AskPrice = e.Price;
                    _lastAskSizes.TryGetValue(symbol, out tick.Quantity);
                    _lastAskPrices[symbol] = e.Price;
                    break;

                case IB.TickType.LastPrice:

                    tick.TickType = TickType.Trade;
                    tick.Value = e.Price;
                    _lastPrices[symbol] = e.Price;
                    break;

                case IB.TickType.HighPrice:
                case IB.TickType.LowPrice:
                case IB.TickType.ClosePrice:
                case IB.TickType.OpenPrice:
                default:
                    return;
            }

            lock (_ticks)
                if (tick.IsValid()) _ticks.Add(tick);

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

        void HandleTickSize(object sender, IB.TickSizeEventArgs e)
        {
            var symbol = default(Symbol);

            if (!_subscribedTickets.TryGetValue(e.TickerId, out symbol)) return;

            var tick = new Tick();
            // in the event of a symbol change this will break since we'll be assigning the
            // new symbol to the permtick which won't be known by the algorithm
            tick.Symbol = symbol;
            var securityType = symbol.ID.SecurityType;
            tick.Quantity = AdjustQuantity(securityType, e.Size);
            tick.Time = GetBrokerTime();
            if (securityType == SecurityType.Forex)
            {
                // forex exchange hours are specified in UTC-05
                tick.Time = tick.Time.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
            }

            if (tick.Quantity == 0) return;

            switch (e.TickType)
            { 
                case IB.TickType.BidSize:

                    tick.TickType = TickType.Quote;

                    _lastBidPrices.TryGetValue(symbol, out tick.BidPrice);
                    _lastBidSizes[symbol] = tick.Quantity;

                    tick.Value = tick.BidPrice;
                    tick.BidSize = tick.Quantity;
                    break;

                case IB.TickType.AskSize:

                    tick.TickType = TickType.Quote;

                    _lastAskPrices.TryGetValue(symbol, out tick.AskPrice);
                    _lastAskSizes[symbol] = tick.Quantity;

                    tick.Value = tick.AskPrice;
                    tick.AskSize = tick.Quantity;
                    break;
                
                
                case IB.TickType.LastSize:
                    tick.TickType = TickType.Trade;

                    decimal lastPrice;
                    _lastPrices.TryGetValue(symbol, out lastPrice);
                    _lastVolumes[symbol] = tick.Quantity;

                    tick.Value = lastPrice;
                        
                    break;

                default:
                    return;
            }
            lock (_ticks)
                if (tick.IsValid()) _ticks.Add(tick);

        }

        private ConcurrentDictionary<Symbol, int> _subscribedSymbols = new ConcurrentDictionary<Symbol, int>();
        private ConcurrentDictionary<int, Symbol> _subscribedTickets = new ConcurrentDictionary<int, Symbol>();
        private ConcurrentDictionary<Symbol, decimal> _lastPrices = new ConcurrentDictionary<Symbol, decimal>();
        private ConcurrentDictionary<Symbol, int> _lastVolumes = new ConcurrentDictionary<Symbol, int>();
        private ConcurrentDictionary<Symbol, decimal> _lastBidPrices = new ConcurrentDictionary<Symbol, decimal>();
        private ConcurrentDictionary<Symbol, int> _lastBidSizes = new ConcurrentDictionary<Symbol, int>();
        private ConcurrentDictionary<Symbol, decimal> _lastAskPrices = new ConcurrentDictionary<Symbol, decimal>();
        private ConcurrentDictionary<Symbol, int> _lastAskSizes = new ConcurrentDictionary<Symbol, int>();
        private List<Tick> _ticks = new List<Tick>();


        private static class AccountValueKeys
        {
            public const string CashBalance = "CashBalance";
            public const string AccruedCash = "AccruedCash";
            public const string NetLiquidationByCurrency = "NetLiquidationByCurrency";
        }

        // these are fatal errors from IB
        private static readonly HashSet<int> ErrorCodes = new HashSet<int>
        {
            100, 101, 103, 138, 139, 142, 143, 144, 145, 200, 203, 300,301,302,306,308,309,310,311,316,317,320,321,322,323,324,326,327,330,331,332,333,344,346,354,357,365,366,381,384,401,414,431,432,438,501,502,503,504,505,506,507,508,510,511,512,513,514,515,516,517,518,519,520,521,522,523,524,525,526,527,528,529,530,531,10000,10001,10005,10013,10015,10016,10021,10022,10023,10024,10025,10026,10027,1300
        };

        // these are warning messages from IB
        private static readonly HashSet<int> WarningCodes = new HashSet<int>
        {
            102, 104, 105, 106, 107, 109, 110, 111, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 129, 131, 132, 133, 134, 135, 136, 137, 140, 141, 146, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 201, 202, 303,313,314,315,319,325,328,329,334,335,336,337,338,339,340,341,342,343,345,347,348,349,350,352,353,355,356,358,359,360,361,362,363,364,367,368,369,370,371,372,373,374,375,376,377,378,379,380,382,383,385,386,387,388,389,390,391,392,393,394,395,396,397,398,399,400,402,403,404,405,406,407,408,409,410,411,412,413,417,418,419,420,421,422,423,424,425,426,427,428,429,430,433,434,435,436,437,439,440,441,442,443,444,445,446,447,448,449,450,1100,10002,10003,10006,10007,10008,10009,10010,10011,10012,10014,10018,10019,10020,1101,1102,2100,2101,2102,2103,2104,2105,2106,2107,2108,2109,2110
        };

        // these require us to issue invalidated order events
        private static readonly HashSet<int> InvalidatingCodes = new HashSet<int>
        {
            104, 105, 106, 107, 109, 110, 111, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 129, 131, 132, 133, 134, 135, 136, 137, 140, 141, 146, 147, 148, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 167, 168, 201, 202,313,314,315,325,328,329,334,335,336,337,338,339340,341,342,343,345,347,348,349,350,352,353,355,356,358,359,360,361,362,363,364,367,368,369,370,371,372,373,374,375,376,377,378,379,380,382,383,387,388,389,390,391,392,393,394,395,396,397,398,400,401,402,403,404,405,406,407,408,409,410,411,412,413,417,418,419,421,423,424,427,428,429,433,434,435,436,437,439,440,441,442,443,444,445,446,447,448,449,10002,10006,10007,10008,10009,10010,10011,10012,10014,10020,2102
        };
    }
}
