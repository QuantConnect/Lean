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
using System.Threading.Tasks;
using IBApi;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using Contract = IBApi.Contract;
using ContractDetails = IBApi.ContractDetails;
using ExecutionFilter = IBApi.ExecutionFilter;
using Order = QuantConnect.Orders.Order;
using OrderStatus = QuantConnect.Orders.OrderStatus;
using OrderType = QuantConnect.Orders.OrderType;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// The Interactive Brokers brokerage
    /// </summary>
    public  partial class InteractiveBrokersBrokerage : Brokerage, IDataQueueHandler
    {
        // next valid order id for this client
        private int _nextValidID;
        // next valid client id for the gateway/tws
        private static int _nextClientID = 0;
        // next valid request id for queries
        private int _nextRequestID = 0;
        private int _nextTickerID = 0;
        private volatile bool _disconnected1100Fired = false;
        private bool _isAccountUpdateSet = false;
        private int _ibRequestId;
        private bool _isClientOnTickPriceSet = false;
        private int _ibMarketDataTicker;
        private decimal _ibConversionRate;
        private int _ibExecutionDetailsRequestId;
        private DateTime _ibLastHistoricalData;
        private int _historicalTickerId;

        private readonly int _port;
        private readonly string _account;
        private readonly string _host;
        private readonly int _clientID;
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;
        private readonly string _agentDescription;
        private readonly EClientSocket _ibClient;
        private ContractDetails _ibContractDetails;
        private Contract _ibContract;
        
        private readonly ManualResetEvent _waitForNextValidId = new ManualResetEvent(false);
        private readonly ManualResetEvent _accountHoldingsResetEvent = new ManualResetEvent(false);
        private ManualResetEvent _openOrderManualResetEvent = new ManualResetEvent(false);
        private ManualResetEvent _ibFirstAccountUpdateReceived = new ManualResetEvent(false);
        private ManualResetEvent _ibGetContractDetailsResetEvent = new ManualResetEvent(false);
        private ManualResetEvent _ibClientOnTickPriceResetEvent = new ManualResetEvent(false);
        private ManualResetEvent _ibHistorialDataResetEvent = new ManualResetEvent(false);

        // IB likes to duplicate/triplicate some events, keep track of them and swallow the dupes
        // we're keeping track of the .ToString() of the order event here
        private readonly FixedSizeHashQueue<string> _recentOrderEvents = new FixedSizeHashQueue<string>(50);
        private List<HistoricalDataDetails> _historicalDataList = new List<HistoricalDataDetails>();
        private readonly List<Order> _ibOpenOrders = new List<Order>();
        private readonly object _orderFillsLock = new object();
        private readonly ConcurrentDictionary<int, int> _orderFills = new ConcurrentDictionary<int, int>(); 
        private readonly ConcurrentDictionary<string, decimal> _cashBalances = new ConcurrentDictionary<string, decimal>(); 
        private readonly ConcurrentDictionary<string, string> _accountProperties = new ConcurrentDictionary<string, string>();
        // number of shares per symbol
        private readonly ConcurrentDictionary<string, Holding> _accountHoldings = new ConcurrentDictionary<string, Holding>();

        private readonly ConcurrentDictionary<string, ContractDetails> _contractDetails = new ConcurrentDictionary<string, ContractDetails>();

        private readonly ConcurrentDictionary<int, ExecutionDetails> _executionDetails = new ConcurrentDictionary<int, ExecutionDetails>();

        private readonly InteractiveBrokersSymbolMapper _symbolMapper = new InteractiveBrokersSymbolMapper();

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                if (_ibClient == null) return false;
                return _ibClient.IsConnected();
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
                Config.GetValue("ib-agent-description", "I")
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
                Config.GetValue("ib-agent-description", "I")
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
        public InteractiveBrokersBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider, string account, string host, int port, string agentDescription = "I")
            : base("Interactive Brokers Brokerage")
        {
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
            _account = account;
            _host = host;
            _port = port;
            _clientID = IncrementClientID();
            _agentDescription = agentDescription;
            _ibClient = new EClientSocket(this);
            Thread.Sleep(10000);
            // we need to wait until we receive the next valid id from the server
        }

        /// <summary>
        /// Provides public access to the underlying IBClient instance
        /// </summary>
        public EClientSocket Client
        {
            get { return _ibClient; }
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
                    Client.cancelOrder(int.Parse(id));
                }
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
            _ibOpenOrders.Clear();
            _openOrderManualResetEvent.Reset();

            Client.reqAllOpenOrders();

            // wait for our end signal
            if (!_openOrderManualResetEvent.WaitOne(15000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetOpenOrders(): Operation took longer than 15 seconds.");
            }

            return _ibOpenOrders;
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
        public ConcurrentDictionary<int, ExecutionDetails> GetExecutions(string symbol, string type, string exchange, DateTime timeSince, string side)
        {
            
            var filter = new ExecutionFilter()
            {
                AcctCode = _account,
                ClientId = _clientID,
                Exchange = exchange,
                SecType = type ?? "",
                Symbol = symbol,
                Time = timeSince.ToString("yyyymmdd hh:mm:ss") ?? DateTime.MinValue.ToString("yyyymmdd hh:mm:ss"),
                Side = side ?? ""
            };
            var client = new EClientSocket(this);
            client.eConnect(_host, _port, IncrementClientID());
            _ibExecutionDetailsRequestId = GetNextRequestID();
            Client.reqExecutions(_ibExecutionDetailsRequestId, filter);
            
            if (!_executionDetails[_ibExecutionDetailsRequestId].ExecutionDetailsResetEvent.WaitOne(5000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetExecutions(): Operation took longer than 1 second.");
            }
            return _executionDetails;
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
                    Client.eConnect(_host, _port, _clientID, false);
                    
                    if (!Client.IsConnected()) throw new Exception("InteractiveBrokersBrokerage.Connect(): Connection returned but was not in connected state.");
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
            if (!_waitForNextValidId.WaitOne(15000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.Connect(): Operation took longer than 15 seconds.");
            }

            // we'll wait to get our first account update, we need to be absolutely sure we 
            // have downloaded the entire account before leaving this function
            _ibFirstAccountUpdateReceived.Reset();
            
            _isAccountUpdateSet = true;
            // first we won't subscribe, wait for this to finish, below we'll subscribe for continuous updates

            Client.reqAccountUpdates(true, _account);
            // wait to see the first account value update

            _ibFirstAccountUpdateReceived.WaitOne(2500);

            // take pause to ensure the account is downloaded before continuing, this was added because running in
            // linux there appears to be different behavior where the account download end fires immediately.
            Thread.Sleep(2500);

            if (!_accountHoldingsResetEvent.WaitOne(15000))
            {
                throw new TimeoutException("InteractiveBrokersBrokerage.GetAccountHoldings(): Operation took longer than 15 seconds.");
            }
            _isAccountUpdateSet = false;
        }

        /// <summary>
        /// Disconnects the client from the IB gateway
        /// </summary>
        public override void Disconnect()
        {
            if (!IsConnected) return;
            Client.eDisconnect();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Client != null)
            {
                Client.eDisconnect();
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
            Client.placeOrder(ibOrder.OrderId, contract, ibOrder);
        }

        private string GetPrimaryExchange(Contract contract)
        {
            ContractDetails details;
            if (_contractDetails.TryGetValue(contract.Symbol, out details))
            {
                return details.Summary.PrimaryExch;
            }

            details = GetContractDetails(contract);
            if (details == null)
            {
                // we were unable to find the contract details
                return null;
            }

            return details.Summary.PrimaryExch;
        }

        private decimal GetMinTick(Contract contract)
        {
            ContractDetails details;
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

        private ContractDetails GetContractDetails(Contract contract)
        {
            _ibContractDetails = null;
            _ibRequestId = GetNextRequestID();
            _ibContract = contract;

            _ibGetContractDetailsResetEvent.Reset();

            // make the request for data
            Client.reqContractDetails(_ibRequestId, contract);

            // we'll wait a second, but it may not exist so just pass through
            _ibGetContractDetailsResetEvent.WaitOne(1000);

            return _ibContractDetails;
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
            _ibConversionRate = 0m; 
            _ibClientOnTickPriceResetEvent.Reset();
            _ibHistorialDataResetEvent.Reset();

            // we're going to request both history and active ticks, we'll use the ticks first
            // and if not present, we'll use the latest from the history request
            
            _historicalTickerId = GetNextTickerID();
            _ibLastHistoricalData = DateTime.MaxValue;

            // request some historical data, IB's api takes into account weekends/market opening hours
            var requestSpan = "100 S";

            Client.reqHistoricalData(_historicalTickerId, contract, DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), requestSpan, "1 secs", "ASK", 1,1,null);  //"20130701 23:59:59 GMT"

            //Wait for unitl historical data to arrives successfully
            _ibHistorialDataResetEvent.WaitOne(15000);

            // define and add our tick handler for the ticks
            _ibMarketDataTicker = GetNextTickerID();

            _isClientOnTickPriceSet = true;

            Client.reqMktData(_ibMarketDataTicker, contract, null, true, null);

            _ibClientOnTickPriceResetEvent.WaitOne(2500);

            _isClientOnTickPriceSet = false;

            // check to see if ticks returned something
            if (_ibConversionRate == 0)
            {
                // history doesn't have a completed event, so we'll just wait for it to not have been called for a second
                while (DateTime.UtcNow - _ibLastHistoricalData < Time.OneSecond) Thread.Sleep(20);

                // check for history
                var ordered = _historicalDataList.OrderByDescending(x => x.Date);
                var mostRecentQuote = ordered.FirstOrDefault();
                if (mostRecentQuote == null)
                {
                    throw new Exception("Unable to get recent quote for " + currencyPair);
                }
                _ibConversionRate = mostRecentQuote.Close;
            }

            if (inverted)
            {
                return 1/_ibConversionRate;
            }
            return _ibConversionRate;
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
        /// Converts a QC order to an IB order
        /// </summary>
        private IBApi.Order ConvertOrder(Order order, Contract contract, int ibOrderID)
        {
            var ibOrder = new IBApi.Order()
            {
                ClientId = _clientID,
                OrderId = ibOrderID,
                Account = _account,
                Action = ConvertOrderDirection(order.Direction),
                TotalQuantity = Math.Abs(order.Quantity),
                OrderType = ConvertOrderType(order.Type),
                AllOrNone = false,
                Tif = "GTC", //IB.TimeInForce.GoodTillCancel
                Transmit = true,
                Rule80A = _agentDescription
            };
            if (order.Type == OrderType.MarketOnOpen)
            {
                ibOrder.Tif = "OPG";
            }

            var limitOrder = order as LimitOrder;
            var stopMarketOrder = order as StopMarketOrder;
            var stopLimitOrder = order as StopLimitOrder;
            if (limitOrder != null)
            {
                ibOrder.LmtPrice = Convert.ToDouble(RoundPrice(limitOrder.LimitPrice, GetMinTick(contract)));
            }
            else if (stopMarketOrder != null)
            {
                ibOrder.AuxPrice = Convert.ToDouble(RoundPrice(stopMarketOrder.StopPrice, GetMinTick(contract)));
            }
            else if (stopLimitOrder != null)
            {
                var minTick = GetMinTick(contract);
                ibOrder.LmtPrice = Convert.ToDouble(RoundPrice(stopLimitOrder.LimitPrice, minTick));
                ibOrder.AuxPrice = Convert.ToDouble(RoundPrice(stopLimitOrder.StopPrice, minTick));
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
            var orderType = ConvertOrderType(ibOrder);
            switch (orderType)
            {
                case OrderType.Market:
                    order = new MarketOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity),
                        new DateTime() // not sure how to get this data
                        );
                    break;

                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(mappedSymbol, 
                        Convert.ToInt32(ibOrder.TotalQuantity),
                        new DateTime());
                    break;

                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity),
                        new DateTime()
                        );
                    break;

                case OrderType.Limit:
                    order = new LimitOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity),
                        Convert.ToDecimal(ibOrder.LmtPrice),
                        new DateTime()
                        );
                    break;

                case OrderType.StopMarket:
                    order = new StopMarketOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity),
                        Convert.ToDecimal(ibOrder.AuxPrice),
                        new DateTime()
                        );
                    break;

                case OrderType.StopLimit:
                    order = new StopLimitOrder(mappedSymbol,
                        Convert.ToInt32(ibOrder.TotalQuantity),
                        Convert.ToDecimal(ibOrder.AuxPrice),
                        Convert.ToDecimal(ibOrder.LmtPrice),
                        new DateTime()
                        );
                    break;

                default:
                    throw new InvalidEnumArgumentException("ibOrder", (int) orderType, typeof (OrderType));
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
        private Contract CreateContract(Symbol symbol, string exchange = null)
        {
            var securityType = ConvertSecurityType(symbol.ID.SecurityType);
            var ibSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var contract = new Contract
            {
                Symbol = ibSymbol,
                SecType = securityType,
                Currency = "USD",
                Exchange = exchange ?? "Smart"
            };

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
                contract.Right = symbol.ID.OptionRight == OptionRight.Call ? "CALL" : "PUT";
                contract.Strike = Convert.ToDouble(symbol.ID.StrikePrice);
                contract.Symbol = symbol.ID.Symbol;
            }

            // some contracts require this, such as MSFT
            contract.PrimaryExch = GetPrimaryExchange(contract);

            return contract;
        }

        /// <summary>
        /// Maps OrderDirection enumeration
        /// </summary>
        private string ConvertOrderDirection(OrderDirection direction)
        {
            switch (direction)
            {
                case OrderDirection.Buy:  return "BUY";
                case OrderDirection.Sell: return "SELL";
                case OrderDirection.Hold: return "UNDEFINED"; //Not sure if undefined exists and what may be a substitute for it. itsjust buy and sell
                default:
                    throw new InvalidEnumArgumentException("direction", (int) direction, typeof (OrderDirection));
            }
        }

        /// <summary>
        /// Maps OrderType enum
        /// </summary>
        private string ConvertOrderType(OrderType type)
        {
            // Refer https://www.interactivebrokers.com/en/software/api/apiguide/tables/supported_order_types.htm 
            switch (type)
            {
                case OrderType.Market:          return "MKT";
                case OrderType.Limit:           return "LMT";
                case OrderType.StopMarket:      return "STP";
                case OrderType.StopLimit:       return "STP LMT";
                case OrderType.MarketOnOpen:    return "MOO"; //IB.OrderType.Market
                case OrderType.MarketOnClose:   return "MOC";
                default:
                    throw new InvalidEnumArgumentException("type", (int)type, typeof(OrderType));
            }
        }

        /// <summary>
        /// Maps OrderType enum
        /// </summary>
        private OrderType ConvertOrderType(global::IBApi.Order order)
        {
            switch (order.OrderType)
            {
                case "LMT": return OrderType.Limit;
                case "STP": return OrderType.StopMarket;
                case "STP LMT": return OrderType.StopLimit;
                case "MOC": return OrderType.MarketOnClose;

                case "MKT":
                    if (order.Tif == "OPG")
                    {
                        return OrderType.MarketOnOpen;
                    }
                    return OrderType.Market;

                default:
                    throw new InvalidEnumArgumentException("order.OrderType Invalid : " + order.OrderType);
            }
        }

        /// <summary>
        /// Maps IB's OrderStats enum
        /// </summary>
        private OrderStatus ConvertOrderStatus(string status)
        {
            switch (status)
            {
                case "ApiPending":
                case "PendingSubmit":
                case "PreSubmitted": 
                    return OrderStatus.New;

                case "ApiCancelled":
                case "PendingCancel":
                case "Cancelled": 
                    return OrderStatus.Canceled;

                case "Submitted": 
                    return OrderStatus.Submitted;

                case "Filled": 
                    return OrderStatus.Filled;

                case "PartiallyFilled": 
                    return OrderStatus.PartiallyFilled;

                case "Error": 
                    return OrderStatus.Invalid;

                case "Inactive":
                    Log.Error("InteractiveBrokersBrokerage.ConvertOrderStatus(): Inactive order");
                    return OrderStatus.None;

                case "None": 
                    return OrderStatus.None;
                    
                // not sure how to map these guys
                default:
                    throw new InvalidEnumArgumentException("status of " + status + " is invalid");
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
                    return "STK";

                case SecurityType.Option:
                    return "OPT";

                case SecurityType.Commodity:
                    return "CMDTY";

                case SecurityType.Forex:
                    return "CASH";

                case SecurityType.Future:
                    return "FUT";

                case SecurityType.Base:
                    throw new ArgumentException("InteractiveBrokers does not support SecurityType.Base");

                default:
                    throw new InvalidEnumArgumentException("type", (int)type, typeof(SecurityType));
            }
        }
        
        /// <summary>
        /// Maps SecurityType enum
        /// </summary>
        private static SecurityType ConvertSecurityType(string type)
        {
            switch (type)
            {
                case "STK":
                    return SecurityType.Equity;

                case "OPT":
                    return SecurityType.Option;

                case "CMDTY":
                    return SecurityType.Commodity;

                case "CASH":
                    return SecurityType.Forex;

                case "FUT":
                    return SecurityType.Future;

                // we don't map these security types to anything specific yet, load them as custom data instead of throwing
                case "IND":
                case "FOP":
                case "BAG":
                case "BOND":
                case "WAR":
                case "BILL":
                case "":
                    return SecurityType.Base;

                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        /// <summary>
        /// Creates a holding object from te UpdatePortfolioEventArgs
        /// </summary>
        private Holding CreateHolding(Contract contract, decimal position, decimal averageCost, decimal marketPrice)
        {
            string currencySymbol;
            if (!Currencies.CurrencySymbols.TryGetValue(contract.Currency, out currencySymbol))
            {
                currencySymbol = "$";
            }

            return new Holding
            {
                Symbol = MapSymbol(contract),
                Type = ConvertSecurityType(contract.SecType),
                Quantity = position,
                AveragePrice = averageCost,
                MarketPrice = marketPrice,
                ConversionRate = 1m, // this will be overwritten when GetAccountHoldings is called to ensure fresh values
                CurrencySymbol =  currencySymbol
            };
        }

        /// <summary>
        /// Maps the IB Contract's symbol to a QC symbol
        /// </summary>
        private Symbol MapSymbol(Contract contract)
        {
            var securityType = ConvertSecurityType(contract.SecType);
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
                Client.reqMktData(id, contract, null, false, null);

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
                    Client.cancelMktData(res);
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
