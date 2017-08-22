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
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event based implementation of Interactive Brokers <see cref="EWrapper"/> interface
    /// </summary>
    public class InteractiveBrokersClient : EWrapper, IDisposable
    {
        #region Event Declarations

        /// <summary>
        /// Error event handler
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// CurrentTimeUtc event handler
        /// </summary>
        public event EventHandler<CurrentTimeUtcEventArgs> CurrentTimeUtc;

        /// <summary>
        /// TickPrice event handler
        /// </summary>
        public event EventHandler<TickPriceEventArgs> TickPrice;

        /// <summary>
        /// TickSize event handler
        /// </summary>
        public event EventHandler<TickSizeEventArgs> TickSize;

        /// <summary>
        /// TickString event handler
        /// </summary>
        public event EventHandler<TickStringEventArgs> TickString;

        /// <summary>
        /// TickGeneric event handler
        /// </summary>
        public event EventHandler<TickGenericEventArgs> TickGeneric;

        /// <summary>
        /// TickEfp event handler
        /// </summary>
        public event EventHandler<TickEfpEventArgs> TickEfp;

        /// <summary>
        /// DeltaNeutralValidation event handler
        /// </summary>
        public event EventHandler<DeltaNeutralValidationEventArgs> DeltaNeutralValidation;

        /// <summary>
        /// TickOptionComputation event handler
        /// </summary>
        public event EventHandler<TickOptionComputationEventArgs> TickOptionComputation;

        /// <summary>
        /// TickSnapshotEnd event handler
        /// </summary>
        public event EventHandler<TickSnapshotEndEventArgs> TickSnapshotEnd;

        /// <summary>
        /// NextValidId event handler
        /// </summary>
        public event EventHandler<NextValidIdEventArgs> NextValidId;

        /// <summary>
        /// ManagedAccounts event handler
        /// </summary>
        public event EventHandler<ManagedAccountsEventArgs> ManagedAccounts;

        /// <summary>
        /// ConnectionClosed event handler
        /// </summary>
        public event EventHandler ConnectionClosed;

        /// <summary>
        /// AccountSummary event handler
        /// </summary>
        public event EventHandler<AccountSummaryEventArgs> AccountSummary;

        /// <summary>
        /// AccountSummaryEnd event handler
        /// </summary>
        public event EventHandler<RequestEndEventArgs> AccountSummaryEnd;

        /// <summary>
        /// BondContractDetails event handler
        /// </summary>
        public event EventHandler<ContractDetailsEventArgs> BondContractDetails;

        /// <summary>
        /// UpdateAccountValue event handler
        /// </summary>
        public event EventHandler<UpdateAccountValueEventArgs> UpdateAccountValue;

        /// <summary>
        /// UpdatePortfolio event handler
        /// </summary>
        public event EventHandler<UpdatePortfolioEventArgs> UpdatePortfolio;

        /// <summary>
        /// UpdateAccountTime event handler
        /// </summary>
        public event EventHandler<UpdateAccountTimeEventArgs> UpdateAccountTime;

        /// <summary>
        /// AccountDownloadEnd event handler
        /// </summary>
        public event EventHandler<AccountDownloadEndEventArgs> AccountDownloadEnd;

        /// <summary>
        /// OrderStatus event handler
        /// </summary>
        public event EventHandler<OrderStatusEventArgs> OrderStatus;

        /// <summary>
        /// OpenOrder event handler
        /// </summary>
        public event EventHandler<OpenOrderEventArgs> OpenOrder;

        /// <summary>
        /// OpenOrderEnd event handler
        /// </summary>
        public event EventHandler OpenOrderEnd;

        /// <summary>
        /// ContractDetails event handler
        /// </summary>
        public event EventHandler<ContractDetailsEventArgs> ContractDetails;

        /// <summary>
        /// ContractDetailsEnd event handler
        /// </summary>
        public event EventHandler<RequestEndEventArgs> ContractDetailsEnd;

        /// <summary>
        /// ExecutionDetails event handler
        /// </summary>
        public event EventHandler<ExecutionDetailsEventArgs> ExecutionDetails;

        /// <summary>
        /// ExecutionDetailsEnd event handler
        /// </summary>
        public event EventHandler<RequestEndEventArgs> ExecutionDetailsEnd;

        /// <summary>
        /// CommissionReport event handler
        /// </summary>
        public event EventHandler<CommissionReportEventArgs> CommissionReport;

        /// <summary>
        /// FundamentalData event handler
        /// </summary>
        public event EventHandler<FundamentalDataEventArgs> FundamentalData;

        /// <summary>
        /// HistoricalData event handler
        /// </summary>
        public event EventHandler<HistoricalDataEventArgs> HistoricalData;

        /// <summary>
        /// HistoricalDataUpdate event handler
        /// </summary>
        public event EventHandler<HistoricalDataUpdateEventArgs> HistoricalDataUpdate;

        /// <summary>
        /// HistoricalDataEnd event handler
        /// </summary>
        public event EventHandler<HistoricalDataEndEventArgs> HistoricalDataEnd;

        /// <summary>
        /// MarketDataType event handler
        /// </summary>
        public event EventHandler<MarketDataTypeEventArgs> MarketDataType;

        /// <summary>
        /// UpdateMarketDepth event handler
        /// </summary>
        public event EventHandler<UpdateMarketDepthEventArgs> UpdateMarketDepth;

        /// <summary>
        /// UpdateMarketDepthLevel2 event handler
        /// </summary>
        public event EventHandler<UpdateMarketDepthLevel2EventArgs> UpdateMarketDepthLevel2;

        /// <summary>
        /// UpdateNewsBulletin event handler
        /// </summary>
        public event EventHandler<UpdateNewsBulletinEventArgs> UpdateNewsBulletin;

        /// <summary>
        /// Position event handler
        /// </summary>
        public event EventHandler<PositionEventArgs> Position;

        /// <summary>
        /// PositionEnd event handler
        /// </summary>
        public event EventHandler PositionEnd;

        /// <summary>
        /// RealtimeBar event handler
        /// </summary>
        public event EventHandler<RealtimeBarEventArgs> RealtimeBar;

        /// <summary>
        /// ScannerParameters event handler
        /// </summary>
        public event EventHandler<ScannerParametersEventArgs> ScannerParameters;

        /// <summary>
        /// ScannerData event handler
        /// </summary>
        public event EventHandler<ScannerDataEventArgs> ScannerData;

        /// <summary>
        /// ScannerDataEnd event handler
        /// </summary>
        public event EventHandler<RequestEndEventArgs> ScannerDataEnd;

        /// <summary>
        /// ReceiveFa event handler
        /// </summary>
        public event EventHandler<ReceiveFaEventArgs> ReceiveFa;

        /// <summary>
        /// VerifyMessageApi event handler
        /// </summary>
        public event EventHandler<VerifyMessageApiEventArgs> VerifyMessageApi;

        /// <summary>
        /// VerifyCompleted event handler
        /// </summary>
        public event EventHandler<VerifyCompletedEventArgs> VerifyCompleted;

        /// <summary>
        /// VerifyAndAuthMessageApi event handler
        /// </summary>
        public event EventHandler<VerifyAndAuthMessageApiEventArgs> VerifyAndAuthMessageApi;

        /// <summary>
        /// VerifyAndAuthCompleted event handler
        /// </summary>
        public event EventHandler<VerifyAndAuthCompletedEventArgs> VerifyAndAuthCompleted;

        /// <summary>
        /// DisplayGroupList event handler
        /// </summary>
        public event EventHandler<DisplayGroupListEventArgs> DisplayGroupList;

        /// <summary>
        /// DisplayGroupUpdated event handler
        /// </summary>
        public event EventHandler<DisplayGroupUpdatedEventArgs> DisplayGroupUpdated;

        /// <summary>
        /// ConnectAck event handler
        /// </summary>
        public event EventHandler ConnectAck;

        /// <summary>
        /// PositionMulti event handler
        /// </summary>
        public event EventHandler<PositionMultiEventArgs> PositionMulti;

        /// <summary>
        /// PositionMultiEnd event handler
        /// </summary>
        public event EventHandler<RequestEndEventArgs> PositionMultiEnd;

        /// <summary>
        /// AccountUpdateMulti event handler
        /// </summary>
        public event EventHandler<AccountUpdateMultiEventArgs> AccountUpdateMulti;

        /// <summary>
        /// AccountUpdateMultiEnd event handler
        /// </summary>
        public event EventHandler<RequestEndEventArgs> AccountUpdateMultiEnd;

        /// <summary>
        /// SecurityDefinitionOptionParameter event handler
        /// </summary>
        public event EventHandler<SecurityDefinitionOptionParameterEventArgs> SecurityDefinitionOptionParameter;

        /// <summary>
        /// SecurityDefinitionOptionParameterEnd event handler
        /// </summary>
        public event EventHandler<RequestEndEventArgs> SecurityDefinitionOptionParameterEnd;

        /// <summary>
        /// SoftDollarTiers event handler
        /// </summary>
        public event EventHandler<SoftDollarTiersEventArgs> SoftDollarTiers;

        /// <summary>
        /// FamilyCodes event handler
        /// </summary>
        public event EventHandler<FamilyCodesEventArgs> FamilyCodes;

        /// <summary>
        /// SymbolSamples event handler
        /// </summary>
        public event EventHandler<SymbolSamplesEventArgs> SymbolSamples;

        /// <summary>
        /// MktDepthExchanges event handler
        /// </summary>
        public event EventHandler<MktDepthExchangesEventArgs> MktDepthExchanges;

        /// <summary>
        /// TickNews event handler
        /// </summary>
        public event EventHandler<TickNewsEventArgs> TickNews;

        /// <summary>
        /// SmartComponents event handler
        /// </summary>
        public event EventHandler<SmartComponentsEventArgs> SmartComponents;

        /// <summary>
        /// TickReqParams event handler
        /// </summary>
        public event EventHandler<TickReqParamsEventArgs> TickReqParams;

        /// <summary>
        /// NewsProviders event handler
        /// </summary>
        public event EventHandler<NewsProvidersEventArgs> NewsProviders;

        /// <summary>
        /// NewsArticle event handler
        /// </summary>
        public event EventHandler<NewsArticleEventArgs> NewsArticle;

        /// <summary>
        /// HistoricalNews event handler
        /// </summary>
        public event EventHandler<HistoricalNewsEventArgs> HistoricalNews;

        /// <summary>
        /// HistoricalNewsEnd event handler
        /// </summary>
        public event EventHandler<HistoricalNewsEndEventArgs> HistoricalNewsEnd;

        /// <summary>
        /// HeadTimestamp event handler
        /// </summary>
        public event EventHandler<HeadTimestampEventArgs> HeadTimestamp;

        /// <summary>
        /// HistogramData event handler
        /// </summary>
        public event EventHandler<HistogramDataEventArgs> HistogramData;

        /// <summary>
        /// RerouteMktDataReq event handler
        /// </summary>
        public event EventHandler<RerouteMktDataReqEventArgs> RerouteMktDataReq;

        /// <summary>
        /// RerouteMktDepthReq event handler
        /// </summary>
        public event EventHandler<RerouteMktDepthReqEventArgs> RerouteMktDepthReq;

        /// <summary>
        /// MarketRule event handler
        /// </summary>
        public event EventHandler<MarketRuleEventArgs> MarketRule;

        /// <summary>
        /// Pnl event handler
        /// </summary>
        public event EventHandler<PnlEventArgs> Pnl;

        /// <summary>
        /// PnlSingle event handler
        /// </summary>
        public event EventHandler<PnlSingleEventArgs> PnlSingle;

        #endregion

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public bool Connected => ClientSocket.IsConnected();

        /// <summary>
        /// Gets the instance of <see cref="EClientSocket"/> to access IB API methods
        /// </summary>
        public EClientSocket ClientSocket
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractiveBrokersClient"/> class
        /// </summary>
        public InteractiveBrokersClient(EReaderSignal signal)
        {
            ClientSocket = new EClientSocket(this, signal);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // disconnect on dispose
            ClientSocket.eDisconnect();
        }

        #region EWrapper Implementation

        /// <summary>
        /// This method is called when an exception occurs while handling a request.
        /// </summary>
        /// <param name="e">The exception that occurred.</param>
        public void error(Exception e)
        {
            error(-1, -1, e.ToString());
        }

        /// <summary>
        /// This method is called when TWS wants to send an error message to the client. (V1).
        /// </summary>
        /// <param name="str">This is the text of the error message.</param>
        public void error(string str)
        {
            error(-1, -1, str);
        }

        /// <summary>
        /// This method is called when there is an error with the communication or when TWS wants to send a message to the client.
        /// </summary>
        /// <param name="id">The request identifier that generated the error.</param>
        /// <param name="errorCode">The code identifying the error.</param>
        /// <param name="errorMsg">The description of the error.</param>
        public void error(int id, int errorCode, string errorMsg)
        {
            OnError(new ErrorEventArgs(id, errorCode, errorMsg));
        }

        /// <summary>
        /// This method receives the current system time on IB's server as a result of calling reqCurrentTime().
        /// </summary>
        /// <param name="time">The current system time on the IB server.</param>
        public void currentTime(long time)
        {
            var currentTimeUtc = new DateTime(time, DateTimeKind.Utc);
            OnCurrentTimeUtc(new CurrentTimeUtcEventArgs(currentTimeUtc));
        }

        /// <summary>
        /// Market data tick price callback, handles all price-related ticks.
        /// </summary>
        /// <param name="tickerId">The request's unique identifier.</param>
        /// <param name="field">Specifies the type of price.</param>
        /// <param name="price">The actual price.</param>
        /// <param name="attribs">Tick attributes.</param>
        public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            OnTickPrice(new TickPriceEventArgs(tickerId, field, price, attribs));
        }

        /// <summary>
        /// Market data tick size callback, handles all size-related ticks.
        /// </summary>
        /// <param name="tickerId">The request's unique identifier.</param>
        /// <param name="field">The type of size being received.</param>
        /// <param name="size">The actual size.</param>
        public void tickSize(int tickerId, int field, int size)
        {
            OnTickSize(new TickSizeEventArgs(tickerId, field, size));
        }

        /// <summary>
        /// Market data callback.
        /// </summary>
        /// <param name="tickerId">The request's unique identifier.</param>
        /// <param name="field">Specifies the type of tick being received.</param>
        /// <param name="value">The value of the specified field.</param>
        public void tickString(int tickerId, int field, string value)
        {
            OnTickString(new TickStringEventArgs(tickerId, field, value));
        }

        /// <summary>
        /// Market data callback.
        /// </summary>
        /// <param name="tickerId">The request's unique identifier.</param>
        /// <param name="field">Specifies the type of tick being received.</param>
        /// <param name="value">The value of the specified field.</param>
        public void tickGeneric(int tickerId, int field, double value)
        {
            OnTickGeneric(new TickGenericEventArgs(tickerId, field, value));
        }

        /// <summary>
        /// Market data callback for Exchange for Physicals.
        /// </summary>
        /// <param name="tickerId">The request's unique identifier.</param>
        /// <param name="tickType">Specifies the type of tick being received.</param>
        /// <param name="basisPoints">Annualized basis points, which is representative of the financing rate that can be directly compared to broker rates.</param>
        /// <param name="formattedBasisPoints">Annualized basis points as a formatted string that depicts them in percentage form.</param>
        /// <param name="impliedFuture">Implied futures price.</param>
        /// <param name="holdDays">The number of hold days until the expiry of the EFP.</param>
        /// <param name="futureExpiry">The expiration date of the single stock future.</param>
        /// <param name="dividendImpact">The dividend impact upon the annualized basis points interest rate.</param>
        /// <param name="dividendsToExpiry">The dividends expected until the expiration of the single stock future.</param>
        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays,
            string futureExpiry, double dividendImpact, double dividendsToExpiry)
        {
            OnTickEfp(new TickEfpEventArgs(tickerId, tickType, basisPoints, formattedBasisPoints, impliedFuture, holdDays, futureExpiry, dividendImpact,
                dividendsToExpiry));
        }

        /// <summary>
        /// Upon accepting a Delta-Neutral RFQ(request for quote), the server sends a deltaNeutralValidation() message with the UnderComp structure. 
        /// If the delta and price fields are empty in the original request, the confirmation will contain the current values from the server. 
        /// These values are locked when the RFQ is processed and remain locked until the RFQ is canceled.
        /// </summary>
        /// <param name="reqId">The ID of the data request.</param>
        /// <param name="underComp">Underlying component.</param>
        public void deltaNeutralValidation(int reqId, UnderComp underComp)
        {
            OnDeltaNeutralValidation(new DeltaNeutralValidationEventArgs(reqId, underComp));
        }

        /// <summary>
        /// This method is called when the market in an option or its underlying moves. 
        /// TWS’s option model volatilities, prices, and deltas, along with the present value of dividends expected on that option's underlying are received.
        /// </summary>
        /// <param name="tickerId">The request's unique identifier.</param>
        /// <param name="field">Specifies the type of option computation.</param>
        /// <param name="impliedVolatility">The implied volatility calculated by the TWS option modeler, using the specified tick type value.</param>
        /// <param name="delta">The option delta value.</param>
        /// <param name="optPrice">The option price.</param>
        /// <param name="pvDividend">The present value of dividends expected on the option's underlying.</param>
        /// <param name="gamma">The option gamma value.</param>
        /// <param name="vega">The option vega value.</param>
        /// <param name="theta">The option theta value.</param>
        /// <param name="undPrice">The price of the underlying.</param>
        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend,
            double gamma, double vega, double theta, double undPrice)
        {
            OnTickOptionComputation(new TickOptionComputationEventArgs(tickerId, field, impliedVolatility, delta, optPrice, pvDividend, gamma, vega, theta,
                undPrice));
        }

        /// <summary>
        /// This is called when a snapshot market data subscription has been fully received and there is nothing more to wait for. 
        /// This also covers the timeout case.
        /// </summary>
        /// <param name="tickerId">The request's unique identifier.</param>
        public void tickSnapshotEnd(int tickerId)
        {
            OnTickSnapshotEnd(new TickSnapshotEndEventArgs(tickerId));
        }

        /// <summary>
        /// Receives the next valid Order ID.
        /// </summary>
        /// <param name="orderId">The next available order ID received from TWS upon connection. Increment all successive orders by one based on this Id.</param>
        public void nextValidId(int orderId)
        {
            OnNextValidId(new NextValidIdEventArgs(orderId));
        }

        /// <summary>
        /// Receives a comma-separated string containing IDs of managed accounts.
        /// </summary>
        /// <param name="accountsList">The comma delimited list of FA managed accounts.</param>
        public void managedAccounts(string accountsList)
        {
            OnManagedAccounts(new ManagedAccountsEventArgs(accountsList));
        }

        /// <summary>
        /// This method is called when TWS closes the sockets connection, or when TWS is shut down.
        /// </summary>
        public void connectionClosed()
        {
            OnConnectionClosed();
        }

        /// <summary>
        /// Returns the account information from TWS in response to reqAccountSummary().
        /// </summary>
        /// <param name="reqId">The request's unique identifier.</param>
        /// <param name="account">The account ID.</param>
        /// <param name="tag">The account attribute being received.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <param name="currency">The currency in which the attribute is expressed.</param>
        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            OnAccountSummary(new AccountSummaryEventArgs(reqId, account, tag, value, currency));
        }

        /// <summary>
        /// This is called once all account information for a given reqAccountSummary() request are received.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        public void accountSummaryEnd(int reqId)
        {
            OnAccountSummaryEnd(new RequestEndEventArgs(reqId));
        }

        /// <summary>
        /// Sends bond contract data when the reqContractDetails() method has been called for bonds.
        /// </summary>
        /// <param name="reqId">The ID of the data request.</param>
        /// <param name="contract">This structure contains a full description of the bond contract being looked up.</param>
        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            OnBondContractDetails(new ContractDetailsEventArgs(reqId, contract));
        }

        /// <summary>
        /// This callback receives the subscribed account's information in response to reqAccountUpdates().
        /// You can only subscribe to one account at a time.
        /// </summary>
        /// <param name="key">A string that indicates one type of account value.</param>
        /// <param name="value">The value associated with the key.</param>
        /// <param name="currency">Defines the currency type, in case the value is a currency type.</param>
        /// <param name="accountName">The account. Useful for Financial Advisor sub-account messages.</param>
        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            OnUpdateAccountValue(new UpdateAccountValueEventArgs(key, value, currency, accountName));
        }

        /// <summary>
        /// Receives the subscribed account's portfolio in response to reqAccountUpdates().
        /// If you want to receive the portfolios of all managed accounts, use reqPositions().
        /// </summary>
        /// <param name="contract">This structure contains a description of the contract which is being traded. The exchange field in a contract is not set for portfolio update.</param>
        /// <param name="position">The number of positions held. If the position is 0, it means the position has just cleared.</param>
        /// <param name="marketPrice">The unit price of the instrument.</param>
        /// <param name="marketValue">The total market value of the instrument.</param>
        /// <param name="averageCost">The average cost per share is calculated by dividing your cost (execution price + commission) by the quantity of your position.</param>
        /// <param name="unrealisedPnl">The difference between the current market value of your open positions and the average cost, or Value - Average Cost.</param>
        /// <param name="realisedPnl">Shows your profit on closed positions, which is the difference between your entry execution cost (execution price + commissions to open the position) and exit execution cost ((execution price + commissions to close the position)</param>
        /// <param name="accountName">The name of the account to which the message applies.  Useful for Financial Advisor sub-account messages.</param>
        public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost,
            double unrealisedPnl, double realisedPnl, string accountName)
        {
            var positionValue = Convert.ToInt32(position);
            OnUpdatePortfolio(new UpdatePortfolioEventArgs(contract, positionValue, marketPrice, marketValue, averageCost, unrealisedPnl, realisedPnl,
                accountName));
        }

        /// <summary>
        /// Receives the last time at which the account was updated.
        /// </summary>
        /// <param name="timestamp">The last update system time.</param>
        public void updateAccountTime(string timestamp)
        {
            OnUpdateAccountTime(new UpdateAccountTimeEventArgs(timestamp));
        }

        /// <summary>
        /// This event is called when the receipt of an account's information has been completed.
        /// </summary>
        /// <param name="account">The account ID.</param>
        public void accountDownloadEnd(string account)
        {
            OnAccountDownloadEnd(new AccountDownloadEndEventArgs(account));
        }

        /// <summary>
        /// This method is called whenever the status of an order changes. It is also called after reconnecting to TWS if the client has any open orders.
        /// </summary>
        /// <param name="orderId">The order Id that was specified previously in the call to placeOrder()</param>
        /// <param name="status">The order status.</param>
        /// <param name="filled">Specifies the number of shares that have been executed.</param>
        /// <param name="remaining">Specifies the number of shares still outstanding.</param>
        /// <param name="avgFillPrice">The average price of the shares that have been executed. This parameter is valid only if the filled parameter value is greater than zero. Otherwise, the price parameter will be zero.</param>
        /// <param name="permId">The TWS id used to identify orders. Remains the same over TWS sessions.</param>
        /// <param name="parentId">The order ID of the parent order, used for bracket and auto trailing stop orders.</param>
        /// <param name="lastFillPrice">The last price of the shares that have been executed. This parameter is valid only if the filled parameter value is greater than zero. Otherwise, the price parameter will be zero.</param>
        /// <param name="clientId">The ID of the client (or TWS) that placed the order. Note that TWS orders have a fixed clientId and orderId of 0 that distinguishes them from API orders.</param>
        /// <param name="whyHeld">This field is used to identify an order held when TWS is trying to locate shares for a short sell. The value used to indicate this is 'locate'.</param>
        public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId,
            int parentId, double lastFillPrice, int clientId, string whyHeld)
        {
            var filledValue = Convert.ToInt32(filled);
            var remainingValue = Convert.ToInt32(remaining);
            OnOrderStatus(new OrderStatusEventArgs(orderId, status, filledValue, remainingValue, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld));
        }

        /// <summary>
        /// This callback feeds in open orders.
        /// </summary>
        /// <param name="orderId">The order Id assigned by TWS. Used to cancel or update the order.</param>
        /// <param name="contract">The Contract class attributes describe the contract.</param>
        /// <param name="order">The Order class attributes define the details of the order.</param>
        /// <param name="orderState">The orderState attributes include margin and commissions fields for both pre and post trade data.</param>
        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            OnOpenOrder(new OpenOrderEventArgs(orderId, contract, order, orderState));
        }

        /// <summary>
        /// This is called at the end of a given request for open orders.
        /// </summary>
        public void openOrderEnd()
        {
            OnOpenOrderEnd();
        }

        /// <summary>
        /// Returns all contracts matching the requested parameters in reqContractDetails(). For example, you can receive an entire option chain.
        /// </summary>
        /// <param name="reqId">The ID of the data request. Ensures that responses are matched to requests if several requests are in process.</param>
        /// <param name="contractDetails">This structure contains a full description of the contract being looked up.</param>
        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            OnContractDetails(new ContractDetailsEventArgs(reqId, contractDetails));
        }

        /// <summary>
        /// This method is called once all contract details for a given request are received. This helps to define the end of an option chain.
        /// </summary>
        /// <param name="reqId">The Id of the data request.</param>
        public void contractDetailsEnd(int reqId)
        {
            OnContractDetailsEnd(new RequestEndEventArgs(reqId));
        }

        /// <summary>
        /// Returns executions from the last 24 hours as a response to reqExecutions(), or when an order is filled.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="contract">This structure contains a full description of the contract that was executed.</param>
        /// <param name="execution">This structure contains addition order execution details.</param>
        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            OnExecutionDetails(new ExecutionDetailsEventArgs(reqId, contract, execution));
        }

        /// <summary>
        /// This method is called once all executions have been sent to a client in response to reqExecutions().
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        public void execDetailsEnd(int reqId)
        {
            OnExecutionDetailsEnd(new RequestEndEventArgs(reqId));
        }

        /// <summary>
        /// This callback returns the commission report portion of an execution and is triggered immediately after a trade execution, or by calling reqExecution().
        /// </summary>
        /// <param name="commissionReport">The structure that contains commission details.</param>
        public void commissionReport(CommissionReport commissionReport)
        {
            OnCommissionReport(new CommissionReportEventArgs(commissionReport));
        }

        /// <summary>
        /// This method is called to receive Reuters global fundamental market data. 
        /// There must be a subscription to Reuters Fundamental set up in Account Management before you can receive this data.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="data">One of these XML reports: Company overview,Financial summary,Financial ratios,Financial statements,Analyst estimates,Company calendar</param>
        public void fundamentalData(int reqId, string data)
        {
            OnFundamentalData(new FundamentalDataEventArgs(reqId, data));
        }

        /// <summary>
        /// Receives the historical data in response to reqHistoricalData().
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="bar">The bar data.</param>
        public void historicalData(int reqId, Bar bar)
        {
            OnHistoricalData(new HistoricalDataEventArgs(reqId, bar));
        }

        /// <summary>
        /// Receives historical data updates to reqHistoricalData().
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="bar">The bar data.</param>
        public void historicalDataUpdate(int reqId, Bar bar)
        {
            OnHistoricalDataUpdate(new HistoricalDataUpdateEventArgs(reqId, bar));
        }

        /// <summary>
        /// Marks the ending of the historical bars reception.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void historicalDataEnd(int reqId, string start, string end)
        {
            OnHistoricalDataEnd(new HistoricalDataEndEventArgs(reqId, start, end));
        }

        /// <summary>
        /// TWS sends a marketDataType(type) callback to the API, where type is set to Frozen or RealTime, to announce that market data has been switched between frozen and real-time. 
        /// This notification occurs only when market data switches between real-time and frozen. 
        /// The marketDataType() callback accepts a reqId parameter and is sent per every subscription because different contracts can generally trade on a different schedule.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="marketDataType">1 for real-time streaming market data or 2 for frozen market data.</param>
        public void marketDataType(int reqId, int marketDataType)
        {
            OnMarketDataType(new MarketDataTypeEventArgs(reqId, marketDataType));
        }

        /// <summary>
        /// Returns market depth (the order book) in response to reqMktDepth().
        /// </summary>
        /// <param name="tickerId">The request's identifier.</param>
        /// <param name="position">Specifies the row Id of this market depth entry.</param>
        /// <param name="operation">Identifies how this order should be applied to the market depth.</param>
        /// <param name="side">Identifies the side of the book that this order belongs to.</param>
        /// <param name="price">The order price.</param>
        /// <param name="size">The order size.</param>
        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            OnUpdateMarketDepth(new UpdateMarketDepthEventArgs(tickerId, position, operation, side, price, size));
        }

        /// <summary>
        /// Returns Level II market depth in response to reqMktDepth().
        /// </summary>
        /// <param name="tickerId">The request's identifier.</param>
        /// <param name="position">Specifies the row id of this market depth entry.</param>
        /// <param name="marketMaker">Specifies the exchange holding the order.</param>
        /// <param name="operation">Identifies how this order should be applied to the market depth.</param>
        /// <param name="side">Identifies the side of the book that this order belongs to.</param>
        /// <param name="price">The order price.</param>
        /// <param name="size">The order size.</param>
        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            OnUpdateMarketDepthLevel2(new UpdateMarketDepthLevel2EventArgs(tickerId, position, marketMaker, operation, side, price, size));
        }

        /// <summary>
        /// Provides news bulletins if the client has subscribed (i.e. by calling the reqNewsBulletins() method).
        /// </summary>
        /// <param name="msgId">The bulletin ID, incrementing for each new bulletin.</param>
        /// <param name="msgType">Specifies the type of bulletin.</param>
        /// <param name="message">The bulletin's message text.</param>
        /// <param name="origExchange">The exchange from which this message originated.</param>
        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            OnUpdateNewsBulletin(new UpdateNewsBulletinEventArgs(msgId, msgType, message, origExchange));
        }

        /// <summary>
        /// This event returns open positions for all accounts in response to the reqPositions() method.
        /// </summary>
        /// <param name="account">The account holding the positions.</param>
        /// <param name="contract">This structure contains a full description of the position's contract.</param>
        /// <param name="pos">The number of positions held.</param>
        /// <param name="avgCost">The average cost of the position.</param>
        public void position(string account, Contract contract, double pos, double avgCost)
        {
            var positionValue = Convert.ToInt32(pos);
            OnPosition(new PositionEventArgs(account, contract, positionValue, avgCost));
        }

        /// <summary>
        /// This is called once all position data for a given request are received and functions as an end marker for the position() data.
        /// </summary>
        public void positionEnd()
        {
            OnPositionEnd();
        }

        /// <summary>
        /// Updates real time 5-second bars.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="time">The date-time stamp of the start of the bar. The format is determined by the reqHistoricalData() formatDate parameter (either as a yyyymmss hh:mm:ss formatted string or as system time).</param>
        /// <param name="open">The bar opening price.</param>
        /// <param name="high">The high price during the time covered by the bar.</param>
        /// <param name="low">The low price during the time covered by the bar.</param>
        /// <param name="close">The bar closing price.</param>
        /// <param name="volume">The volume during the time covered by the bar.</param>
        /// <param name="wap">The weighted average price during the time covered by the bar.</param>
        /// <param name="count">When TRADES data is returned, represents the number of trades that occurred during the time period the bar covers.</param>
        public void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double wap, int count)
        {
            OnRealtimeBar(new RealtimeBarEventArgs(reqId, time, open, high, low, close, volume, wap, count));
        }

        /// <summary>
        /// This method receives an XML document that describes the valid parameters that a scanner subscription can have.
        /// </summary>
        /// <param name="xml">The xml-formatted string with the available parameters.</param>
        public void scannerParameters(string xml)
        {
            OnScannerParameters(new ScannerParametersEventArgs(xml));
        }

        /// <summary>
        /// This method receives the requested market scanner data results.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="rank">The ranking within the response of this bar.</param>
        /// <param name="contractDetails">This structure contains a full description of the contract that was executed.</param>
        /// <param name="distance">Varies based on query.</param>
        /// <param name="benchmark">Varies based on query.</param>
        /// <param name="projection">Varies based on query.</param>
        /// <param name="legsStr">Describes combo legs when scan is returning EFP.</param>
        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            OnScannerData(new ScannerDataEventArgs(reqId, rank, contractDetails, distance, benchmark, projection, legsStr));
        }

        /// <summary>
        /// Marks the end of one scan (the receipt of scanner data has ended).
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        public void scannerDataEnd(int reqId)
        {
            OnScannerDataEnd(new RequestEndEventArgs(reqId));
        }

        /// <summary>
        /// This method receives Financial Advisor configuration information from TWS.
        /// </summary>
        /// <param name="faDataType">Specifies the type of Financial Advisor configuration data being received from TWS.</param>
        /// <param name="faXmlData">The XML string containing the previously requested FA configuration information.</param>
        public void receiveFA(int faDataType, string faXmlData)
        {
            OnReceiveFa(new ReceiveFaEventArgs(faDataType, faXmlData));
        }

        /// <summary>
        /// Deprecated Function.
        /// </summary>
        /// <param name="apiData"></param>
        public void verifyMessageAPI(string apiData)
        {
            OnVerifyMessageApi(new VerifyMessageApiEventArgs(apiData));
        }

        /// <summary>
        /// DOC_TODO.
        /// </summary>
        /// <param name="isSuccessful"></param>
        /// <param name="errorText"></param>
        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            OnVerifyCompleted(new VerifyCompletedEventArgs(isSuccessful, errorText));
        }

        /// <summary>
        /// DOC_TODO.
        /// </summary>
        /// <param name="apiData"></param>
        /// <param name="xyzChallenge"></param>
        public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            OnVerifyAndAuthMessageApi(new VerifyAndAuthMessageApiEventArgs(apiData, xyzChallenge));
        }

        /// <summary>
        /// DOC_TODO.
        /// </summary>
        /// <param name="isSuccessful"></param>
        /// <param name="errorText"></param>
        public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            OnVerifyAndAuthCompleted(new VerifyAndAuthCompletedEventArgs(isSuccessful, errorText));
        }

        /// <summary>
        /// This callback is a one-time response to queryDisplayGroups().
        /// </summary>
        /// <param name="reqId">The requestId specified in queryDisplayGroups().</param>
        /// <param name="groups">A list of integers representing visible group ID separated by the “|” character, and sorted by most used group first. This list will not change during TWS session (in other words, user cannot add a new group; sorting can change though). Example: "3|1|2"</param>
        public void displayGroupList(int reqId, string groups)
        {
            OnDisplayGroupList(new DisplayGroupListEventArgs(reqId, groups));
        }

        /// <summary>
        /// This is sent by TWS to the API client once after receiving the subscription request subscribeToGroupEvents(), and will be sent again if the selected contract in the subscribed display group has changed.
        /// </summary>
        /// <param name="reqId">The requestId specified in subscribeToGroupEvents().</param>
        /// <param name="contractInfo">The encoded value that uniquely represents the contract in IB.</param>
        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            OnDisplayGroupUpdated(new DisplayGroupUpdatedEventArgs(reqId, contractInfo));
        }

        /// <summary>
        /// Callback signifying completion of successful connection.
        /// </summary>
        public void connectAck()
        {
            OnConnectAck();
        }

        /// <summary>
        /// Provides the portfolio's open positions.
        /// </summary>
        /// <param name="requestId">The id of the request.</param>
        /// <param name="account">The account holding the position.</param>
        /// <param name="modelCode">The model code holding the position.</param>
        /// <param name="contract">The position's Contract.</param>
        /// <param name="pos">The number of positions held.</param>
        /// <param name="avgCost">The average cost of the position.</param>
        public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
        {
            OnPositionMulti(new PositionMultiEventArgs(requestId, account, modelCode, contract, pos, avgCost));
        }

        /// <summary>
        /// Indicates all the positions have been transmitted.
        /// </summary>
        /// <param name="requestId">The id of the request.</param>
        public void positionMultiEnd(int requestId)
        {
            OnPositionMultiEnd(new RequestEndEventArgs(requestId));
        }

        /// <summary>
        /// Provides the account updates.
        /// </summary>
        /// <param name="requestId">The id of the request,</param>
        /// <param name="account">The account with updates.</param>
        /// <param name="modelCode">The model code with updates.</param>
        /// <param name="key">The name of parameter.</param>
        /// <param name="value">The value of parameter.</param>
        /// <param name="currency">The currency of parameter.</param>
        public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            OnAccountUpdateMulti(new AccountUpdateMultiEventArgs(requestId, account, modelCode, key, value, currency));
        }

        /// <summary>
        /// Indicates all the account updates have been transmitted.
        /// </summary>
        /// <param name="requestId">The id of the request.</param>
        public void accountUpdateMultiEnd(int requestId)
        {
            OnAccountUpdateMultiEnd(new RequestEndEventArgs(requestId));
        }

        /// <summary>
        /// Returns the option chain for an underlying on an exchange specified in reqSecDefOptParams.
        /// There will be multiple callbacks to securityDefinitionOptionParameter if multiple exchanges are specified in reqSecDefOptParams.
        /// </summary>
        /// <param name="reqId">ID of the request initiating the callback</param>
        /// <param name="exchange"></param>
        /// <param name="underlyingConId">The conID of the underlying security</param>
        /// <param name="tradingClass">The option trading class</param>
        /// <param name="multiplier">The option multiplier</param>
        /// <param name="expirations">A list of the expiries for the options of this underlying on this exchange</param>
        /// <param name="strikes">A list of the possible strikes for options of this underlying on this exchange</param>
        public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass,
            string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            OnSecurityDefinitionOptionParameter(new SecurityDefinitionOptionParameterEventArgs(reqId, exchange, underlyingConId, tradingClass,
                multiplier, expirations, strikes));
        }

        /// <summary>
        /// Called when all callbacks to securityDefinitionOptionParameter are complete
        /// </summary>
        /// <param name="reqId">the ID used in the call to securityDefinitionOptionParameter</param>
        public void securityDefinitionOptionParameterEnd(int reqId)
        {
            OnSecurityDefinitionOptionParameterEnd(new RequestEndEventArgs(reqId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId">The id of the request.</param>
        /// <param name="tiers"></param>
        public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
        {
            OnSoftDollarTiers(new SoftDollarTiersEventArgs(reqId, tiers));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="familyCodes"></param>
        public void familyCodes(FamilyCode[] familyCodes)
        {
            OnFamilyCodes(new FamilyCodesEventArgs(familyCodes));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="contractDescriptions"></param>
        public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            OnSymbolSamples(new SymbolSamplesEventArgs(reqId, contractDescriptions));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="depthMktDataDescriptions"></param>
        public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
        {
            OnMktDepthExchanges(new MktDepthExchangesEventArgs(depthMktDataDescriptions));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tickerId"></param>
        /// <param name="timeStamp"></param>
        /// <param name="providerCode"></param>
        /// <param name="articleId"></param>
        /// <param name="headline"></param>
        /// <param name="extraData"></param>
        public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            OnTickNews(new TickNewsEventArgs(tickerId, timeStamp, providerCode, articleId, headline, extraData));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="theMap"></param>
        public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
        {
            OnSmartComponents(new SmartComponentsEventArgs(reqId, theMap));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tickerId"></param>
        /// <param name="minTick"></param>
        /// <param name="bboExchange"></param>
        /// <param name="snapshotPermissions"></param>
        public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            OnTickReqParams(new TickReqParamsEventArgs(tickerId, minTick, bboExchange, snapshotPermissions));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newsProviders"></param>
        public void newsProviders(NewsProvider[] newsProviders)
        {
            OnNewsProviders(new NewsProvidersEventArgs(newsProviders));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="articleType"></param>
        /// <param name="articleText"></param>
        public void newsArticle(int requestId, int articleType, string articleText)
        {
            OnNewsArticle(new NewsArticleEventArgs(requestId, articleType, articleText));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="time"></param>
        /// <param name="providerCode"></param>
        /// <param name="articleId"></param>
        /// <param name="headline"></param>
        public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
        {
            OnHistoricalNews(new HistoricalNewsEventArgs(requestId, time, providerCode, articleId, headline));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="hasMore"></param>
        public void historicalNewsEnd(int requestId, bool hasMore)
        {
            OnHistoricalNewsEnd(new HistoricalNewsEndEventArgs(requestId, hasMore));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="headTimestamp"></param>
        public void headTimestamp(int reqId, string headTimestamp)
        {
            OnHeadTimestamp(new HeadTimestampEventArgs(reqId, headTimestamp));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="data"></param>
        public void histogramData(int reqId, HistogramEntry[] data)
        {
            OnHistogramData(new HistogramDataEventArgs(reqId, data));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="conId"></param>
        /// <param name="exchange"></param>
        public void rerouteMktDataReq(int reqId, int conId, string exchange)
        {
            OnRerouteMktDataReq(new RerouteMktDataReqEventArgs(reqId, conId, exchange));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="conId"></param>
        /// <param name="exchange"></param>
        public void rerouteMktDepthReq(int reqId, int conId, string exchange)
        {
            OnRerouteMktDepthReq(new RerouteMktDepthReqEventArgs(reqId, conId, exchange));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="marketRuleId"></param>
        /// <param name="priceIncrements"></param>
        public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            OnMarketRule(new MarketRuleEventArgs(marketRuleId, priceIncrements));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="dailyPnL"></param>
        /// <param name="unrealizedPnL"></param>
        public void pnl(int reqId, double dailyPnL, double unrealizedPnL)
        {
            OnPnl(new PnlEventArgs(reqId, dailyPnL, unrealizedPnL));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reqId"></param>
        /// <param name="pos"></param>
        /// <param name="dailyPnL"></param>
        /// <param name="unrealizedPnL"></param>
        /// <param name="value"></param>
        public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double value)
        {
            OnPnlSingle(new PnlSingleEventArgs(reqId, pos, dailyPnL, unrealizedPnL, value));
        }

        #endregion

        #region Event Invocators

        /// <summary>
        /// Error event invocator
        /// </summary>
        protected virtual void OnError(ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }

        /// <summary>
        /// CurrentTimeUtc event invocator
        /// </summary>
        protected virtual void OnCurrentTimeUtc(CurrentTimeUtcEventArgs e)
        {
            CurrentTimeUtc?.Invoke(this, e);
        }

        /// <summary>
        /// TickPrice event invocator
        /// </summary>
        protected virtual void OnTickPrice(TickPriceEventArgs e)
        {
            TickPrice?.Invoke(this, e);
        }

        /// <summary>
        /// TickSize event invocator
        /// </summary>
        protected virtual void OnTickSize(TickSizeEventArgs e)
        {
            TickSize?.Invoke(this, e);
        }

        /// <summary>
        /// TickString event invocator
        /// </summary>
        protected virtual void OnTickString(TickStringEventArgs e)
        {
            TickString?.Invoke(this, e);
        }

        /// <summary>
        /// TickGeneric event invocator
        /// </summary>
        protected virtual void OnTickGeneric(TickGenericEventArgs e)
        {
            TickGeneric?.Invoke(this, e);
        }

        /// <summary>
        /// TickEfp event invocator
        /// </summary>
        protected virtual void OnTickEfp(TickEfpEventArgs e)
        {
            TickEfp?.Invoke(this, e);
        }

        /// <summary>
        /// DeltaNeutralValidation event invocator
        /// </summary>
        protected virtual void OnDeltaNeutralValidation(DeltaNeutralValidationEventArgs e)
        {
            DeltaNeutralValidation?.Invoke(this, e);
        }

        /// <summary>
        /// TickOptionComputation event invocator
        /// </summary>
        protected virtual void OnTickOptionComputation(TickOptionComputationEventArgs e)
        {
            TickOptionComputation?.Invoke(this, e);
        }

        /// <summary>
        /// TickSnapshotEnd event invocator
        /// </summary>
        protected virtual void OnTickSnapshotEnd(TickSnapshotEndEventArgs e)
        {
            TickSnapshotEnd?.Invoke(this, e);
        }

        /// <summary>
        /// NextValidId event invocator
        /// </summary>
        protected virtual void OnNextValidId(NextValidIdEventArgs e)
        {
            NextValidId?.Invoke(this, e);
        }

        /// <summary>
        /// ManagedAccounts event invocator
        /// </summary>
        protected virtual void OnManagedAccounts(ManagedAccountsEventArgs e)
        {
            ManagedAccounts?.Invoke(this, e);
        }

        /// <summary>
        /// ConnectionClosed event invocator
        /// </summary>
        protected virtual void OnConnectionClosed()
        {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// AccountSummary event invocator
        /// </summary>
        protected virtual void OnAccountSummary(AccountSummaryEventArgs e)
        {
            AccountSummary?.Invoke(this, e);
        }

        /// <summary>
        /// AccountSummaryEnd event invocator
        /// </summary>
        protected virtual void OnAccountSummaryEnd(RequestEndEventArgs e)
        {
            AccountSummaryEnd?.Invoke(this, e);
        }

        /// <summary>
        /// BondContractDetails event invocator
        /// </summary>
        protected virtual void OnBondContractDetails(ContractDetailsEventArgs e)
        {
            BondContractDetails?.Invoke(this, e);
        }

        /// <summary>
        /// UpdateAccountValue event invocator
        /// </summary>
        protected virtual void OnUpdateAccountValue(UpdateAccountValueEventArgs e)
        {
            UpdateAccountValue?.Invoke(this, e);
        }

        /// <summary>
        /// UpdatePortfolio event invocator
        /// </summary>
        protected virtual void OnUpdatePortfolio(UpdatePortfolioEventArgs e)
        {
            UpdatePortfolio?.Invoke(this, e);
        }

        /// <summary>
        /// UpdateAccountTime event invocator
        /// </summary>
        protected virtual void OnUpdateAccountTime(UpdateAccountTimeEventArgs e)
        {
            UpdateAccountTime?.Invoke(this, e);
        }

        /// <summary>
        /// AccountDownloadEnd event invocator
        /// </summary>
        protected virtual void OnAccountDownloadEnd(AccountDownloadEndEventArgs e)
        {
            AccountDownloadEnd?.Invoke(this, e);
        }

        /// <summary>
        /// OrderStatus event invocator
        /// </summary>
        protected virtual void OnOrderStatus(OrderStatusEventArgs e)
        {
            OrderStatus?.Invoke(this, e);
        }

        /// <summary>
        /// OpenOrder event invocator
        /// </summary>
        protected virtual void OnOpenOrder(OpenOrderEventArgs e)
        {
            OpenOrder?.Invoke(this, e);
        }

        /// <summary>
        /// OpenOrderEnd event invocator
        /// </summary>
        protected virtual void OnOpenOrderEnd()
        {
            OpenOrderEnd?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ContractDetails event invocator
        /// </summary>
        protected virtual void OnContractDetails(ContractDetailsEventArgs e)
        {
            ContractDetails?.Invoke(this, e);
        }

        /// <summary>
        /// ContractDetailsEnd event invocator
        /// </summary>
        protected virtual void OnContractDetailsEnd(RequestEndEventArgs e)
        {
            ContractDetailsEnd?.Invoke(this, e);
        }

        /// <summary>
        /// ExecutionDetails event invocator
        /// </summary>
        protected virtual void OnExecutionDetails(ExecutionDetailsEventArgs e)
        {
            ExecutionDetails?.Invoke(this, e);
        }

        /// <summary>
        /// ExecutionDetailsEnd event invocator
        /// </summary>
        protected virtual void OnExecutionDetailsEnd(RequestEndEventArgs e)
        {
            ExecutionDetailsEnd?.Invoke(this, e);
        }

        /// <summary>
        /// CommissionReport event invocator
        /// </summary>
        protected virtual void OnCommissionReport(CommissionReportEventArgs e)
        {
            CommissionReport?.Invoke(this, e);
        }

        /// <summary>
        /// FundamentalData event invocator
        /// </summary>
        protected virtual void OnFundamentalData(FundamentalDataEventArgs e)
        {
            FundamentalData?.Invoke(this, e);
        }

        /// <summary>
        /// HistoricalData event invocator
        /// </summary>
        protected virtual void OnHistoricalData(HistoricalDataEventArgs e)
        {
            HistoricalData?.Invoke(this, e);
        }

        /// <summary>
        /// HistoricalDataUpdate event invocator
        /// </summary>
        protected virtual void OnHistoricalDataUpdate(HistoricalDataUpdateEventArgs e)
        {
            HistoricalDataUpdate?.Invoke(this, e);
        }

        /// <summary>
        /// HistoricalDataEnd event invocator
        /// </summary>
        protected virtual void OnHistoricalDataEnd(HistoricalDataEndEventArgs e)
        {
            HistoricalDataEnd?.Invoke(this, e);
        }

        /// <summary>
        /// MarketDataType event invocator
        /// </summary>
        protected virtual void OnMarketDataType(MarketDataTypeEventArgs e)
        {
            MarketDataType?.Invoke(this, e);
        }

        /// <summary>
        /// UpdateMarketDepth event invocator
        /// </summary>
        protected virtual void OnUpdateMarketDepth(UpdateMarketDepthEventArgs e)
        {
            UpdateMarketDepth?.Invoke(this, e);
        }

        /// <summary>
        /// UpdateMarketDepthLevel2 event invocator
        /// </summary>
        protected virtual void OnUpdateMarketDepthLevel2(UpdateMarketDepthLevel2EventArgs e)
        {
            UpdateMarketDepthLevel2?.Invoke(this, e);
        }

        /// <summary>
        /// UpdateNewsBulletin event invocator
        /// </summary>
        protected virtual void OnUpdateNewsBulletin(UpdateNewsBulletinEventArgs e)
        {
            UpdateNewsBulletin?.Invoke(this, e);
        }

        /// <summary>
        /// Position event invocator
        /// </summary>
        protected virtual void OnPosition(PositionEventArgs e)
        {
            Position?.Invoke(this, e);
        }

        /// <summary>
        /// PositionEnd event invocator
        /// </summary>
        protected virtual void OnPositionEnd()
        {
            PositionEnd?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// RealtimeBar event invocator
        /// </summary>
        protected virtual void OnRealtimeBar(RealtimeBarEventArgs e)
        {
            RealtimeBar?.Invoke(this, e);
        }

        /// <summary>
        /// ScannerParameters event invocator
        /// </summary>
        protected virtual void OnScannerParameters(ScannerParametersEventArgs e)
        {
            ScannerParameters?.Invoke(this, e);
        }

        /// <summary>
        /// ScannerData event invocator
        /// </summary>
        protected virtual void OnScannerData(ScannerDataEventArgs e)
        {
            ScannerData?.Invoke(this, e);
        }

        /// <summary>
        /// ScannerDataEnd event invocator
        /// </summary>
        protected virtual void OnScannerDataEnd(RequestEndEventArgs e)
        {
            ScannerDataEnd?.Invoke(this, e);
        }

        /// <summary>
        /// ReceiveFa event invocator
        /// </summary>
        protected virtual void OnReceiveFa(ReceiveFaEventArgs e)
        {
            ReceiveFa?.Invoke(this, e);
        }

        /// <summary>
        /// VerifyMessageApi event invocator
        /// </summary>
        protected virtual void OnVerifyMessageApi(VerifyMessageApiEventArgs e)
        {
            VerifyMessageApi?.Invoke(this, e);
        }

        /// <summary>
        /// VerifyCompleted event invocator
        /// </summary>
        protected virtual void OnVerifyCompleted(VerifyCompletedEventArgs e)
        {
            VerifyCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// VerifyAndAuthMessageApi event invocator
        /// </summary>
        protected virtual void OnVerifyAndAuthMessageApi(VerifyAndAuthMessageApiEventArgs e)
        {
            VerifyAndAuthMessageApi?.Invoke(this, e);
        }

        /// <summary>
        /// VerifyAndAuthCompleted event invocator
        /// </summary>
        protected virtual void OnVerifyAndAuthCompleted(VerifyAndAuthCompletedEventArgs e)
        {
            VerifyAndAuthCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// DisplayGroupList event invocator
        /// </summary>
        protected virtual void OnDisplayGroupList(DisplayGroupListEventArgs e)
        {
            DisplayGroupList?.Invoke(this, e);
        }

        /// <summary>
        /// DisplayGroupUpdated event invocator
        /// </summary>
        protected virtual void OnDisplayGroupUpdated(DisplayGroupUpdatedEventArgs e)
        {
            DisplayGroupUpdated?.Invoke(this, e);
        }

        /// <summary>
        /// ConnectAck event invocator
        /// </summary>
        protected virtual void OnConnectAck()
        {
            ConnectAck?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// PositionMulti event invocator
        /// </summary>
        protected virtual void OnPositionMulti(PositionMultiEventArgs e)
        {
            PositionMulti?.Invoke(this, e);
        }

        /// <summary>
        /// PositionMultiEnd event invocator
        /// </summary>
        protected virtual void OnPositionMultiEnd(RequestEndEventArgs e)
        {
            PositionMultiEnd?.Invoke(this, e);
        }

        /// <summary>
        /// AccountUpdateMulti event invocator
        /// </summary>
        protected virtual void OnAccountUpdateMulti(AccountUpdateMultiEventArgs e)
        {
            AccountUpdateMulti?.Invoke(this, e);
        }

        /// <summary>
        /// AccountUpdateMultiEnd event invocator
        /// </summary>
        protected virtual void OnAccountUpdateMultiEnd(RequestEndEventArgs e)
        {
            AccountUpdateMultiEnd?.Invoke(this, e);
        }

        /// <summary>
        /// SecurityDefinitionOptionParameter event invocator
        /// </summary>
        protected virtual void OnSecurityDefinitionOptionParameter(SecurityDefinitionOptionParameterEventArgs e)
        {
            SecurityDefinitionOptionParameter?.Invoke(this, e);
        }

        /// <summary>
        /// SecurityDefinitionOptionParameterEnd event invocator
        /// </summary>
        protected virtual void OnSecurityDefinitionOptionParameterEnd(RequestEndEventArgs e)
        {
            SecurityDefinitionOptionParameterEnd?.Invoke(this, e);
        }

        /// <summary>
        /// SoftDollarTiers event invocator
        /// </summary>
        protected virtual void OnSoftDollarTiers(SoftDollarTiersEventArgs e)
        {
            SoftDollarTiers?.Invoke(this, e);
        }

        /// <summary>
        /// FamilyCodes event invocator
        /// </summary>
        protected virtual void OnFamilyCodes(FamilyCodesEventArgs e)
        {
            FamilyCodes?.Invoke(this, e);
        }

        /// <summary>
        /// SymbolSamples event invocator
        /// </summary>
        protected virtual void OnSymbolSamples(SymbolSamplesEventArgs e)
        {
            SymbolSamples?.Invoke(this, e);
        }

        /// <summary>
        /// MktDepthExchanges event invocator
        /// </summary>
        protected virtual void OnMktDepthExchanges(MktDepthExchangesEventArgs e)
        {
            MktDepthExchanges?.Invoke(this, e);
        }

        /// <summary>
        /// TickNews event invocator
        /// </summary>
        protected virtual void OnTickNews(TickNewsEventArgs e)
        {
            TickNews?.Invoke(this, e);
        }

        /// <summary>
        /// SmartComponents event invocator
        /// </summary>
        protected virtual void OnSmartComponents(SmartComponentsEventArgs e)
        {
            SmartComponents?.Invoke(this, e);
        }

        /// <summary>
        /// TickReqParams event invocator
        /// </summary>
        protected virtual void OnTickReqParams(TickReqParamsEventArgs e)
        {
            TickReqParams?.Invoke(this, e);
        }

        /// <summary>
        /// NewsProviders event invocator
        /// </summary>
        protected virtual void OnNewsProviders(NewsProvidersEventArgs e)
        {
            NewsProviders?.Invoke(this, e);
        }

        /// <summary>
        /// NewsArticle event invocator
        /// </summary>
        protected virtual void OnNewsArticle(NewsArticleEventArgs e)
        {
            NewsArticle?.Invoke(this, e);
        }

        /// <summary>
        /// HistoricalNews event invocator
        /// </summary>
        protected virtual void OnHistoricalNews(HistoricalNewsEventArgs e)
        {
            HistoricalNews?.Invoke(this, e);
        }

        /// <summary>
        /// HistoricalNewsEnd event invocator
        /// </summary>
        protected virtual void OnHistoricalNewsEnd(HistoricalNewsEndEventArgs e)
        {
            HistoricalNewsEnd?.Invoke(this, e);
        }

        /// <summary>
        /// HeadTimestamp event invocator
        /// </summary>
        protected virtual void OnHeadTimestamp(HeadTimestampEventArgs e)
        {
            HeadTimestamp?.Invoke(this, e);
        }

        /// <summary>
        /// HistogramData event invocator
        /// </summary>
        protected virtual void OnHistogramData(HistogramDataEventArgs e)
        {
            HistogramData?.Invoke(this, e);
        }

        /// <summary>
        /// RerouteMktDataReq event invocator
        /// </summary>
        protected virtual void OnRerouteMktDataReq(RerouteMktDataReqEventArgs e)
        {
            RerouteMktDataReq?.Invoke(this, e);
        }

        /// <summary>
        /// RerouteMktDepthReq event invocator
        /// </summary>
        protected virtual void OnRerouteMktDepthReq(RerouteMktDepthReqEventArgs e)
        {
            RerouteMktDepthReq?.Invoke(this, e);
        }

        /// <summary>
        /// MarketRule event invocator
        /// </summary>
        protected virtual void OnMarketRule(MarketRuleEventArgs e)
        {
            MarketRule?.Invoke(this, e);
        }

        /// <summary>
        /// Pnl event invocator
        /// </summary>
        protected virtual void OnPnl(PnlEventArgs e)
        {
            Pnl?.Invoke(this, e);
        }

        /// <summary>
        /// PnlSingle event invocator
        /// </summary>
        protected virtual void OnPnlSingle(PnlSingleEventArgs e)
        {
            PnlSingle?.Invoke(this, e);
        }

        #endregion
    }
}
