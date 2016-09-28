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
using System.Globalization;
using IBApi;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using Order = IBApi.Order;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    public partial class InteractiveBrokersBrokerage : EWrapper
    {
        /// <summary>
        /// Handles error messages from IB
        /// </summary>
        /// <param name="e">Error Exception Message</param>
        public virtual void error(Exception e)
        {
            error(-1, -1, e.ToString());
        }

        /// <summary>
        /// Handles error messages from IB
        /// </summary>
        /// <param name="str">Error Message</param>
        public virtual void error(string str)
        {
            error(-1, -1, str);
        }

        /// <summary>
        /// Handles error messages from IB
        /// </summary>
        /// <param name="id">Error id</param>
        /// <param name="errorCode">Error Code</param>
        /// <param name="errorMsg">Error Message</param>
        public virtual void error(int id, int errorCode, string errorMsg)
        {
            // https://www.interactivebrokers.com/en/software/api/apiguide/tables/api_message_codes.htm

            // rewrite these messages to be single lined
            errorMsg = errorMsg.Replace("\r\n", ". ").Replace("\r", ". ").Replace("\n", ". ");
            Log.Trace(string.Format("InteractiveBrokersBrokerage.HandleError(): Order: {0} ErrorCode: {1} - {2}", id, errorCode, errorMsg));

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
            if (errorCode == 1100 && !_disconnected1100Fired)
            {
                _disconnected1100Fired = true;

                // begin the try wait logic
                TryWaitForReconnect();
            }
            else if (errorCode == 1102)
            {
                // we've reconnected
                _disconnected1100Fired = false;
                OnMessage(BrokerageMessageEvent.Reconnected(errorMsg));
            }

            if (InvalidatingCodes.Contains(errorCode))
            {
                Log.Trace(string.Format("InteractiveBrokersBrokerage.HandleError.InvalidateOrder(): Order: {0} ErrorCode: {1} - {2}", id, errorCode, errorMsg));

                // invalidate the order
                var order = _orderProvider.GetOrderByBrokerageId(id);
                const int orderFee = 0;
                var orderEvent = new OrderEvent(order, DateTime.UtcNow, orderFee) { Status = OrderStatus.Invalid };
                OnOrderEvent(orderEvent);
            }

            OnMessage(new BrokerageMessageEvent(brokerageMessageType, errorCode, errorMsg));
        }

        /// <summary>
        /// Gets the current brokerage time
        /// </summary>
        /// <param name="time">Time</param>
        public virtual void currentTime(long time)
        {
            // keep track of clock drift
            var dateTime = new DateTime(time);
            _brokerTimeDiff = dateTime.Subtract(DateTime.UtcNow);
        }

        /// <summary>
        /// Gets the Tick price
        /// </summary>
        /// <param name="tickerId">Ticker id</param>
        /// <param name="field">Tick Type</param>
        /// <param name="price">Tick price</param>
        /// <param name="canAutoExecute">Specifies whether price tick is available for automatic execution</param>
        public virtual void tickPrice(int tickerId, int field, double price, int canAutoExecute)
        {
            var symbol = default(Symbol);

            if (!_subscribedTickets.TryGetValue(tickerId, out symbol)) return;

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
            tick.Value = Convert.ToDecimal(price);

            if (price <= 0 &&
                securityType != SecurityType.Future &&
                securityType != SecurityType.Option)
                return;

            switch (field)
            {
                case IBApi.TickType.BID: 

                    tick.TickType = TickType.Quote;
                    tick.BidPrice = (decimal) price;
                    _lastBidSizes.TryGetValue(symbol, out tick.Quantity);
                    _lastBidPrices[symbol] = (decimal) price;
                    break;

                case IBApi.TickType.ASK:

                    tick.TickType = TickType.Quote;
                    tick.AskPrice = (decimal) price;
                    _lastAskSizes.TryGetValue(symbol, out tick.Quantity);
                    _lastAskPrices[symbol] = (decimal) price;
                    break;

                case IBApi.TickType.LAST:

                    tick.TickType = TickType.Trade;
                    tick.Value = (decimal) price;
                    _lastPrices[symbol] = (decimal) price;
                    break;

                case IBApi.TickType.HIGH:
                case IBApi.TickType.LOW:
                case IBApi.TickType.CLOSE:
                case IBApi.TickType.OPEN:
                default:
                    return;
            }
            lock (_ticks)
                if (tick.IsValid()) _ticks.Add(tick);

            if (_isClientOnTickPriceSet)
            {
                if (tickerId == _ibMarketDataTicker && field == IBApi.TickType.ASK)
                {
                    _ibConversionRate = Convert.ToDecimal(price);
                    _ibClientOnTickPriceResetEvent.Set();
                }
            }
        }

        /// <summary>
        /// Returns the size of the tick
        /// </summary>
        /// <param name="tickerId">Ticker id</param>
        /// <param name="field">Tick Type</param>
        /// <param name="size">Tick size</param>
        public virtual void tickSize(int tickerId, int field, int size)
        {
            var symbol = default(Symbol);

            if (!_subscribedTickets.TryGetValue(tickerId, out symbol)) return;

            var tick = new Tick();
            // in the event of a symbol change this will break since we'll be assigning the
            // new symbol to the permtick which won't be known by the algorithm
            tick.Symbol = symbol;
            var securityType = symbol.ID.SecurityType;
            tick.Quantity = AdjustQuantity(securityType, size);
            tick.Time = GetBrokerTime();
            if (securityType == SecurityType.Forex)
            {
                // forex exchange hours are specified in UTC-05
                tick.Time = tick.Time.ConvertTo(TimeZones.NewYork, TimeZones.EasternStandard);
            }

            if (tick.Quantity == 0) return;

            switch (field)
            {
                case IBApi.TickType.BID_SIZE:

                    tick.TickType = TickType.Quote;

                    _lastBidPrices.TryGetValue(symbol, out tick.BidPrice);
                    _lastBidSizes[symbol] = tick.Quantity;

                    tick.Value = tick.BidPrice;
                    tick.BidSize = tick.Quantity;
                    break;

                case IBApi.TickType.ASK_SIZE:

                    tick.TickType = TickType.Quote;

                    _lastAskPrices.TryGetValue(symbol, out tick.AskPrice);
                    _lastAskSizes[symbol] = tick.Quantity;

                    tick.Value = tick.AskPrice;
                    tick.AskSize = tick.Quantity;
                    break;


                case IBApi.TickType.LAST_SIZE:
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

        /// <summary>
        /// Returns the next valid id
        /// </summary>
        /// <param name="orderId">Order id</param>
        public virtual void nextValidId(int orderId)
        {
            // only grab this id when we initialize, and we'll manually increment it here to avoid threading issues
            if (_nextValidID == 0)
            {
                _nextValidID = orderId;
                _waitForNextValidId.Set();
            }
            Log.Trace("InteractiveBrokersBrokerage.Callbacks.nextValidId(): " + orderId);
        }

        /// <summary>
        /// Returns the active account list
        /// </summary>
        /// <param name="accountsList">List of accounts</param>
        public virtual void managedAccounts(string accountsList)
        {
            Log.Trace("InteractiveBrokersBrokerage.Callbacks.managedAccounts(): Account list: " + accountsList + "\n");
        }

        /// <summary>
        /// Acknowledges the closing of the connection
        /// </summary>
        public virtual void connectionClosed()
        {
            Log.Debug("TWS Connection Closed.");
        }
        
        /// <summary>
        /// Stores all the account values
        /// </summary>
        /// <param name="key">Type of account key</param>
        /// <param name="value">Value associated with the key</param>
        /// <param name="currency">Currency type</param>
        /// <param name="accountName">Name of the account</param>
        public virtual void updateAccountValue(string key, string value, string currency, string accountName)
        {
            //https://www.interactivebrokers.com/en/software/api/apiguide/activex/updateaccountvalue.htm
            
            try
            {
                _accountProperties[currency + ":" + key] = value;

                // we want to capture if the user's cash changes so we can reflect it in the algorithm
                if (key == AccountValueKeys.CashBalance && currency != "BASE")
                {
                    var cashBalance = decimal.Parse(value, CultureInfo.InvariantCulture);
                    _cashBalances.AddOrUpdate(currency, cashBalance);
                    OnAccountChanged(new AccountEvent(currency, cashBalance));
                }
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleUpdateAccountValue(): " + err);
            }

            if (_isAccountUpdateSet) _ibFirstAccountUpdateReceived.Set();

        }

        /// <summary>
        /// Handle portfolio changed events from IB
        /// </summary>
        /// <param name="contract">Contract</param>
        /// <param name="position">Number of positions hekd</param>
        /// <param name="marketPrice">Price of the instrument</param>
        /// <param name="marketValue">Total market value of the instrument</param>
        /// <param name="averageCost">Average cost per share</param>
        /// <param name="unrealisedPNL">Difference between current market value and average cost</param>
        /// <param name="realisedPNL">Porfits on closed positions</param>
        /// <param name="accountName">Name of the account</param>
        public virtual void updatePortfolio(Contract contract, int position, double marketPrice, double marketValue, double averageCost,
            double unrealisedPNL, double realisedPNL, string accountName)
        {
            _accountHoldingsResetEvent.Reset();
            var holding = CreateHolding(contract, Convert.ToDecimal(position), Convert.ToDecimal(averageCost), Convert.ToDecimal(marketPrice));
            _accountHoldings[holding.Symbol.Value] = holding;
        }
        
        /// <summary>
        /// Marks the end of Downloading of the Account
        /// </summary>
        /// <param name="account">Account id</param>
        public virtual void accountDownloadEnd(string account)
        {
            Log.Trace("InteractiveBrokersBrokerage.AccountDownloadEnd(): Finished account download for " + account);
            _accountHoldingsResetEvent.Set();
        }

        /// <summary>
        /// Handle order events from IB
        /// </summary>
        /// <param name="orderId">Order id</param>
        /// <param name="status">Order Status</param>
        /// <param name="filled">Number of shares executed</param>
        /// <param name="remaining">Number of shares still outstanding</param>
        /// <param name="avgFillPrice">Average price of the shares</param>
        /// <param name="permId">TWS id used to identify the order</param>
        /// <param name="parentId">Order id of the parent order</param>
        /// <param name="lastFillPrice">Last price of the shares</param>
        /// <param name="clientId">Client id</param>
        /// <param name="whyHeld">Identidies the order held by TWS</param>
        public virtual void orderStatus(int orderId, string status, int filled, int remaining, double avgFillPrice, int permId, int parentId,
            double lastFillPrice, int clientId, string whyHeld)
        {
            try
            {
                var order = _orderProvider.GetOrderByBrokerageId(orderId);
                if (order == null)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Unable to locate order with BrokerageID " + orderId);
                    return;
                }


                var orderStatus = ConvertOrderStatus(status);
                if (order.Status == OrderStatus.Filled && Convert.ToInt32(filled) == 0 && Convert.ToInt32(remaining) == 0)
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
                    filledThisTime = Convert.ToInt32(filled) - currentFilled;
                    _orderFills.AddOrUpdate(order.Id, currentFilled, (sym, orderFilled) => Convert.ToInt32(filled));
                }

                if (orderStatus == OrderStatus.Invalid)
                {
                    Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): ERROR -- " + orderId);
                }

                // set status based on filled this time
                if (filledThisTime != 0)
                {
                    orderStatus = Convert.ToInt32(remaining) != 0 ? OrderStatus.PartiallyFilled : OrderStatus.Filled;
                }
                // don't send empty fill events
                else if (orderStatus == OrderStatus.PartiallyFilled || orderStatus == OrderStatus.Filled)
                {
                    Log.Trace("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Ignored zero fill event: OrderId: " + orderId + " Remaining: " + remaining);
                    return;
                }

                // mark sells as negative quantities
                var fillQuantity = order.Direction == OrderDirection.Buy ? filledThisTime : -filledThisTime;
                order.PriceCurrency = _securityProvider.GetSecurity(order.Symbol).SymbolProperties.QuoteCurrency;
                var orderEvent = new OrderEvent(order, DateTime.UtcNow, orderFee, "Interactive Brokers Fill Event")
                {
                    Status = orderStatus,
                    FillPrice = Convert.ToDecimal(lastFillPrice),
                    FillQuantity = fillQuantity
                };
                if (Convert.ToInt32(remaining) != 0)
                {
                    orderEvent.Message += " - " + remaining + " remaining";
                }

                // if we're able to add to our fixed length, unique queue then send the event
                // otherwise it is a duplicate, so skip it
                if (_recentOrderEvents.Add(orderEvent.ToString() + remaining))
                {
                    OnOrderEvent(orderEvent);
                }
            }
            catch (InvalidOperationException err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): Unable to resolve executions for BrokerageID: " + orderId + " - " + err);
            }
            catch (Exception err)
            {
                Log.Error("InteractiveBrokersBrokerage.HandleOrderStatusUpdates(): " + err);
            }
        }

        /// <summary>
        /// Handles the current Open Orders
        /// </summary>
        /// <param name="orderId">Order id</param>
        /// <param name="contract">Contract</param>
        /// <param name="order">Order</param>
        /// <param name="orderState">The state of the order</param>
        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            // convert IB order objects returned from RequestOpenOrders\
            var convertedOrder = ConvertOrder(order, contract);
            if (convertedOrder == null)
            {
                Log.Error("InteractiveBrokersBrokerage.openOrder(): Order Conversion Failed.");
            }
            _ibOpenOrders.Add(convertedOrder);
        }

        /// <summary>
        /// Marks the end of downloading open orders
        /// </summary>
        public virtual void openOrderEnd()
        {
            // this signals the end of our RequestOpenOrders call
            _openOrderManualResetEvent.Set();
        }

        /// <summary>
        /// Handles the contract Details
        /// </summary>
        /// <param name="reqId">Request id</param>
        /// <param name="contractDetails">Contract Details</param>
        public virtual void contractDetails(int reqId, ContractDetails contractDetails)
        {
            // ignore other requests
            if (reqId != _ibRequestId) return;
            _ibContractDetails = contractDetails;
            _contractDetails.TryAdd(_ibContract.Symbol, _ibContractDetails);
        }

        /// <summary>
        /// Marks the end of downloading the contract details
        /// </summary>
        /// <param name="reqId">Request id</param>
        public virtual void contractDetailsEnd(int reqId)
        {
            _ibGetContractDetailsResetEvent.Set();
        }

        /// <summary>
        /// Handles the execution details
        /// </summary>
        /// <param name="reqId">Request id</param>
        /// <param name="contract">Contract</param>
        /// <param name="execution">Execution Details</param>
        public virtual void execDetails(int reqId, Contract contract, Execution execution)
        {
            var executionDetails = new ExecutionDetails(reqId, contract, execution);
            if (reqId == _ibExecutionDetailsRequestId) _executionDetails.TryAdd(reqId, executionDetails);
        }

        /// <summary>
        /// Marks the end of downloading the executions
        /// </summary>
        /// <param name="reqId">Request id</param>
        public virtual void execDetailsEnd(int reqId)
        {
            if (reqId == _ibExecutionDetailsRequestId) _executionDetails[reqId].ExecutionDetailsResetEvent.Set();
        }
        
        /// <summary>
        /// Handles the historical data
        /// </summary>
        /// <param name="reqId">Request id for the data requested</param>
        /// <param name="date">Date for the request</param>
        /// <param name="open">Open price</param>
        /// <param name="high">High price</param>
        /// <param name="low">Low price</param>
        /// <param name="close">Close price</param>
        /// <param name="volume">Volume</param>
        /// <param name="count">Number of trades</param>
        /// <param name="WAP">Weighted average covered by the bar</param>
        /// <param name="hasGaps">Whether or not there are gaps in the data</param>
        public virtual void historicalData(int reqId, string date, double open, double high, double low, double close, int volume, int count,
            double WAP, bool hasGaps)
        {
            if (reqId == _historicalTickerId)
            {
                var _hisroticalData = new HistoricalDataDetails(reqId, DateTime.ParseExact(date, "yyyyMMdd  HH:mm:ss", null), Convert.ToDecimal(open), Convert.ToDecimal(high), Convert.ToDecimal(low), Convert.ToDecimal(close), volume, count, WAP, hasGaps);
                _historicalDataList.Add(_hisroticalData);
                _ibLastHistoricalData = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Marks the end of downloading the historical data
        /// </summary>
        /// <param name="reqId">Request id for the data requested</param>
        /// <param name="start">Start date</param>
        /// <param name="end">End date</param>
        public virtual void historicalDataEnd(int reqId, string start, string end)
        {
            _ibHistorialDataResetEvent.Set();
        }

#region Empty Interface methods
        public virtual void marketDataType(int reqId, int marketDataType)
        {
            
        }

        public virtual void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            
        }

        public virtual void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
          
        }

        public virtual void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
           
        }

        public virtual void position(string account, Contract contract, int pos, double avgCost)
        {
            
        }

        public virtual void positionEnd()
        {
            
        }

        public virtual void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP,
            int count)
        {
            
        }

        public virtual void scannerParameters(string xml)
        {
            
        }

        public virtual void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark,
            string projection, string legsStr)
        {
            
        }

        public virtual void scannerDataEnd(int reqId)
        {
            
        }

        public virtual void receiveFA(int faDataType, string faXmlData)
        {
            
        }

        public virtual void verifyMessageAPI(string apiData)
        {
            
        }

        public virtual void verifyCompleted(bool isSuccessful, string errorText)
        {
            
        }

        public virtual void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
           
        }

        public virtual void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            
        }

        public virtual void displayGroupList(int reqId, string groups)
        {
           
        }

        public virtual void displayGroupUpdated(int reqId, string contractInfo)
        {
            
        }

        public virtual void connectAck()
        {
        }

        public virtual void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
        {
           
        }

        public virtual void positionMultiEnd(int requestId)
        {
           
        }

        public virtual void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            
        }

        public virtual void accountUpdateMultiEnd(int requestId)
        {
            
        }

        public virtual void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass,
            string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
           
        }

        public virtual void securityDefinitionOptionParameterEnd(int reqId)
        {
            
        }

        public virtual void tickString(int tickerId, int field, string value)
        {

        }

        public virtual void tickGeneric(int tickerId, int field, double value)
        {

        }

        public virtual void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture,
            int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {

        }

        public virtual void deltaNeutralValidation(int reqId, UnderComp underComp)
        {

        }

        public virtual void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice,
            double pvDividend, double gamma, double vega, double theta, double undPrice)
        {

        }

        public virtual void tickSnapshotEnd(int tickerId)
        {

        }

        public virtual void commissionReport(CommissionReport commissionReport)
        {

        }

        public virtual void fundamentalData(int reqId, string data)
        {

        }

        public virtual void accountSummary(int reqId, string account, string tag, string value, string currency)
        {

        }

        public virtual void accountSummaryEnd(int reqId)
        {

        }

        public virtual void bondContractDetails(int reqId, ContractDetails contract)
        {

        }

        public virtual void updateAccountTime(string timestamp)
        {

        }
#endregion

    }
}
