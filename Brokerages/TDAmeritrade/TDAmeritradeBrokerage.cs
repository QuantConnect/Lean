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
 *
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Util;
using RestSharp;
using TDAmeritradeApi.Client;
using TDAmeritradeApi.Client.Models;
using TDAmeritradeApi.Client.Models.AccountsAndTrading;
using TDAmeritradeApi.Client.Models.MarketData;
using AccountsAndTrading = TDAmeritradeApi.Client.Models.AccountsAndTrading;

namespace QuantConnect.Brokerages.TDAmeritrade
{
    /// <summary>
    /// Tradier Class:
    ///  - Handle authentication.
    ///  - Data requests.
    ///  - Rate limiting.
    ///  - Placing orders.
    ///  - Getting user data.
    /// </summary>
    [BrokerageFactory(typeof(TDAmeritradeBrokerageFactory))]
    public partial class TDAmeritradeBrokerage : Brokerage, IDataQueueHandler, IDataQueueUniverseProvider, IHistoryProvider, IOptionChainProvider
    {
        private readonly string _accountId;
        private readonly string _clientId;
        private readonly string _redirectUri;

        // we're reusing the equity exchange here to grab typical exchange hours
        private static readonly EquityExchange Exchange =
            new EquityExchange(MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, null, SecurityType.Equity));

        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

        // polling timer for checking for fill events
        private readonly Timer _orderFillTimer;
        private static TDAmeritradeClient tdClient;

        private readonly IAlgorithm _algorithm;
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;
        private readonly IDataAggregator _aggregator;

        private readonly EventBasedDataQueueHandlerSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Returns the brokerage account's base currency
        /// </summary>
        public override string AccountBaseCurrency => Currencies.USD;

        /// <summary>
        /// Create a new Tradier Object:
        /// </summary>
        public TDAmeritradeBrokerage(
            IAlgorithm algorithm,
            IOrderProvider orderProvider,
            ISecurityProvider securityProvider,
            IDataAggregator aggregator,
            string accountId,
            string clientId,
            string redirectUri,
            ICredentials tdCredentials)
            : base("TD Ameritrade Brokerage")
        {
            _algorithm = algorithm;
            _orderProvider = orderProvider;
            _securityProvider = securityProvider;
            _aggregator = aggregator;
            _accountId = accountId;
            _clientId = clientId;
            _redirectUri = redirectUri;


            _subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            _subscriptionManager.SubscribeImpl += Subscribe;
            _subscriptionManager.UnsubscribeImpl += Unsubscribe;
            InitializeClient(clientId, redirectUri, tdCredentials);
        }

        public static void InitializeClient(string clientId, string redirectUri, ICredentials tdCredentials)
        {
            if (tdClient == null)
            {
                tdClient = new TDAmeritradeClient(clientId, redirectUri);
                tdClient.LogIn(tdCredentials).Wait();
            }
        }

        #region IBrokerage implementation

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected => _isDataQueueHandlerInitialized && tdClient.LiveMarketDataStreamer.IsConnected;

        /// <summary>
        /// Gets all open orders on the account.
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        public override List<Order> GetOpenOrders()
        {
            var orders = new List<Order>();
            var openOrders = tdClient.AccountsAndTradingApi.GetAllOrdersAsync(_accountId, OrderStrategyStatusType.QUEUED).Result;

            foreach (var openOrder in openOrders)
            {
                var order = TDAmeritradeToLeanMapper.ConvertOrder(openOrder);
                orders.Add(order);
            }

            return orders;
        }

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdings = GetPositions().Select(ConvertToHolding).Where(x => x.Quantity != 0).ToList();
            var tickers = holdings.Select(x => TDAmeritradeToLeanMapper.GetBrokerageSymbol(x.Symbol)).ToList();

            var quotes = GetQuotes(tickers);
            foreach (var holding in holdings)
            {
                var ticker = TDAmeritradeToLeanMapper.GetBrokerageSymbol(holding.Symbol);

                if (quotes.TryGetValue(ticker, out MarketQuote quote))
                {
                    holding.MarketPrice = quote.LastPrice;
                }
            }
            return holdings;
        }

        private Dictionary<string, MarketQuote> GetQuotes(List<string> tickers)
        {
            return tdClient.MarketDataApi.GetQuotes(tickers.ToArray()).Result;
        }

        private Holding ConvertToHolding(Position position)
        {
            var symbol = TDAmeritradeToLeanMapper.GetSymbolFrom(position.instrument);

            var averagePrice = position.averagePrice;
            if (symbol.SecurityType == SecurityType.Option)
            {
                var multiplier = _symbolPropertiesDatabase.GetSymbolProperties(
                        symbol.ID.Market,
                        symbol,
                        symbol.SecurityType,
                        _algorithm.Portfolio.CashBook.AccountCurrency)
                    .ContractMultiplier;

                averagePrice /= multiplier;
            }

            return new Holding
            {
                Symbol = symbol,
                AveragePrice = averagePrice,
                CurrencySymbol = "$",
                MarketPrice = 0m, //--> GetAccountHoldings does a call to GetQuotes to fill this data in
                Quantity = position.shortQuantity == 0 ? position.longQuantity : position.shortQuantity
            };
        }

        private IEnumerable<AccountsAndTrading.Position> GetPositions()
        {
            var account = tdClient.AccountsAndTradingApi.GetAccountAsync(_accountId).Result;

            return account.positions ?? Enumerable.Empty<AccountsAndTrading.Position>();
        }

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        public override List<CashAmount> GetCashBalance()
        {
            return new List<CashAmount>
            {
                new CashAmount(GetCurrentCashBalance(), Currencies.USD)
            };
        }

        private decimal GetCurrentCashBalance()
        {
            var account = tdClient.AccountsAndTradingApi.GetAccountAsync(_accountId).Result;

            if (account is CashAccount cashAccount)
                return cashAccount.currentBalances.totalCash;
            else if (account is MarginAccount marginAccount)
                return marginAccount.currentBalances.cashBalance;
            else
                return 0;
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
                Log.Trace($"{nameof(TDAmeritradeBrokerage)}.PlaceOrder(): {order}");

                var holdingQuantity = _securityProvider.GetHoldingsQuantity(order.Symbol);

                var orderStrategy = TDAmeritradeToLeanMapper.ConvertToOrderStrategy(order, holdingQuantity);

                tdClient.AccountsAndTradingApi.PlaceOrderAsync(_accountId, orderStrategy).Wait();

                return true;
            }
            catch(Exception ex)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "PlaceOrderError", ex.Message));

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
            Log.Trace($"{nameof(TDAmeritradeBrokerage)}.UpdateOrder(): {order}");

            try
            {
                var replaceOrder = TDAmeritradeToLeanMapper.ConvertToOrderStrategy(order, order.Quantity);

                tdClient.AccountsAndTradingApi.ReplaceOrderAsync(_accountId, long.Parse(order.BrokerId.First(), CultureInfo.InvariantCulture), replaceOrder).Wait();

                return true;
            }
            catch (Exception ex)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "UpdateOrderError", ex.Message));

                return false;
            }
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            Log.Trace($"{nameof(TDAmeritradeBrokerage)}.CancelOrder(): {order}");

            try
            {

                tdClient.AccountsAndTradingApi.CancelOrderAsync(_accountId, long.Parse(order.BrokerId.First(), CultureInfo.InvariantCulture)).Wait();

                return true;
            }
            catch (Exception ex)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "CancelOrderError", ex.Message));

                return false;
            }
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            tdClient.LiveMarketDataStreamer.LoginAsync(_accountId).Wait();
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            tdClient.LiveMarketDataStreamer.LogoutAsync().Wait();
        }

        /// <summary>
        /// Dispose of the brokerage instance
        /// </summary>
        public override void Dispose()
        {
            _orderFillTimer.DisposeSafely();
        }

        private readonly HashSet<string> ErrorsDuringMarketHours = new HashSet<string>
        {
            "CheckForFillsError", "UnknownIdResolution", "ContingentOrderError", "NullResponse", "PendingOrderNotReturned"
        };

        /// <summary>
        /// Event invocator for the Message event
        /// </summary>
        /// <param name="e">The error</param>
        protected override void OnMessage(BrokerageMessageEvent e)
        {
            var message = e;
            if (Exchange.DateTimeIsOpen(DateTime.Now) && ErrorsDuringMarketHours.Contains(e.Code))
            {
                // elevate this to an error
                message = new BrokerageMessageEvent(BrokerageMessageType.Error, e.Code, e.Message);
            }
            base.OnMessage(message);
        }
        #endregion
    }
}
