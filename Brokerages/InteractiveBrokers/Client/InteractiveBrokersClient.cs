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
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event based implementation of Interactive Brokers <see cref="EWrapper"/> interface
    /// </summary>
    public class InteractiveBrokersClient : DefaultEWrapper, IDisposable
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
        /// NextValidId event handler
        /// </summary>
        public event EventHandler<NextValidIdEventArgs> NextValidId;

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
        /// HistoricalData event handler
        /// </summary>
        public event EventHandler<HistoricalDataEventArgs> HistoricalData;

        /// <summary>
        /// HistoricalDataEnd event handler
        /// </summary>
        public event EventHandler<HistoricalDataEndEventArgs> HistoricalDataEnd;

        /// <summary>
        /// PositionEnd event handler
        /// </summary>
        public event EventHandler PositionEnd;

        /// <summary>
        /// ReceiveFa event handler
        /// </summary>
        public event EventHandler<ReceiveFaEventArgs> ReceiveFa;

        /// <summary>
        /// ConnectAck event handler
        /// </summary>
        public event EventHandler ConnectAck;

        /// <summary>
        /// ManagedAccounts event handler
        /// </summary>
        public event EventHandler<ManagedAccountsEventArgs> ManagedAccounts;

        /// <summary>
        /// FamilyCodes event handler
        /// </summary>
        public event EventHandler<FamilyCodesEventArgs> FamilyCodes;

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
        public override void error(Exception e)
        {
            error(-1, -1, e.ToString());
        }

        /// <summary>
        /// This method is called when TWS wants to send an error message to the client. (V1).
        /// </summary>
        /// <param name="str">This is the text of the error message.</param>
        public override void error(string str)
        {
            error(-1, -1, str);
        }

        /// <summary>
        /// This method is called when there is an error with the communication or when TWS wants to send a message to the client.
        /// </summary>
        /// <param name="id">The request identifier that generated the error.</param>
        /// <param name="errorCode">The code identifying the error.</param>
        /// <param name="errorMsg">The description of the error.</param>
        public override void error(int id, int errorCode, string errorMsg)
        {
            OnError(new ErrorEventArgs(id, errorCode, errorMsg));
        }

        /// <summary>
        /// This method receives the current system time on IB's server as a result of calling reqCurrentTime().
        /// </summary>
        /// <param name="time">The current system time on the IB server.</param>
        public override void currentTime(long time)
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
        public override void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            OnTickPrice(new TickPriceEventArgs(tickerId, field, price, attribs));
        }

        /// <summary>
        /// Market data tick size callback, handles all size-related ticks.
        /// </summary>
        /// <param name="tickerId">The request's unique identifier.</param>
        /// <param name="field">The type of size being received.</param>
        /// <param name="size">The actual size.</param>
        public override void tickSize(int tickerId, int field, int size)
        {
            OnTickSize(new TickSizeEventArgs(tickerId, field, size));
        }

        /// <summary>
        /// Receives the next valid Order ID.
        /// </summary>
        /// <param name="orderId">The next available order ID received from TWS upon connection. Increment all successive orders by one based on this Id.</param>
        public override void nextValidId(int orderId)
        {
            OnNextValidId(new NextValidIdEventArgs(orderId));
        }

        /// <summary>
        /// This method is called when TWS closes the sockets connection, or when TWS is shut down.
        /// </summary>
        public override void connectionClosed()
        {
            OnConnectionClosed();
        }

        /// <summary>
        /// Receives the account information.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="account">The account id</param>
        /// <param name="tag">The account's attribute being received.</param>
        /// <param name="value">The account's attribute's value.</param>
        /// <param name="currency">The currency on which the value is expressed.</param>
        public override void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            OnAccountSummary(new AccountSummaryEventArgs(reqId, account, tag, value, currency));
        }

        /// <summary>
        /// This is called once all account information for a given reqAccountSummary() request are received.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        public override void accountSummaryEnd(int reqId)
        {
            OnAccountSummaryEnd(new RequestEndEventArgs(reqId));
        }

        /// <summary>
        /// Sends bond contract data when the reqContractDetails() method has been called for bonds.
        /// </summary>
        /// <param name="reqId">The ID of the data request.</param>
        /// <param name="contract">This structure contains a full description of the bond contract being looked up.</param>
        public override void bondContractDetails(int reqId, ContractDetails contract)
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
        public override void updateAccountValue(string key, string value, string currency, string accountName)
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
        public override void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost,
            double unrealisedPnl, double realisedPnl, string accountName)
        {
            var positionValue = Convert.ToInt32(position);
            OnUpdatePortfolio(new UpdatePortfolioEventArgs(contract, positionValue, marketPrice, marketValue, averageCost, unrealisedPnl, realisedPnl,
                accountName));
        }

        /// <summary>
        /// This event is called when the receipt of an account's information has been completed.
        /// </summary>
        /// <param name="account">The account ID.</param>
        public override void accountDownloadEnd(string account)
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
        /// <param name="mktCapPrice">If an order has been capped, this indicates the current capped price. Requires TWS 967+ and API v973.04+. Python API specifically requires API v973.06+.</param>
        public override void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId,
            int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            var filledValue = Convert.ToInt32(filled);
            var remainingValue = Convert.ToInt32(remaining);
            OnOrderStatus(new OrderStatusEventArgs(orderId, status, filledValue, remainingValue, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice));
        }

        /// <summary>
        /// This callback feeds in open orders.
        /// </summary>
        /// <param name="orderId">The order Id assigned by TWS. Used to cancel or update the order.</param>
        /// <param name="contract">The Contract class attributes describe the contract.</param>
        /// <param name="order">The Order class attributes define the details of the order.</param>
        /// <param name="orderState">The orderState attributes include margin and commissions fields for both pre and post trade data.</param>
        public override void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            OnOpenOrder(new OpenOrderEventArgs(orderId, contract, order, orderState));
        }

        /// <summary>
        /// This is called at the end of a given request for open orders.
        /// </summary>
        public override void openOrderEnd()
        {
            OnOpenOrderEnd();
        }

        /// <summary>
        /// Returns all contracts matching the requested parameters in reqContractDetails(). For example, you can receive an entire option chain.
        /// </summary>
        /// <param name="reqId">The ID of the data request. Ensures that responses are matched to requests if several requests are in process.</param>
        /// <param name="contractDetails">This structure contains a full description of the contract being looked up.</param>
        public override void contractDetails(int reqId, ContractDetails contractDetails)
        {
            OnContractDetails(new ContractDetailsEventArgs(reqId, contractDetails));
        }

        /// <summary>
        /// This method is called once all contract details for a given request are received. This helps to define the end of an option chain.
        /// </summary>
        /// <param name="reqId">The Id of the data request.</param>
        public override void contractDetailsEnd(int reqId)
        {
            OnContractDetailsEnd(new RequestEndEventArgs(reqId));
        }

        /// <summary>
        /// Returns executions from the last 24 hours as a response to reqExecutions(), or when an order is filled.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="contract">This structure contains a full description of the contract that was executed.</param>
        /// <param name="execution">This structure contains addition order execution details.</param>
        public override void execDetails(int reqId, Contract contract, Execution execution)
        {
            OnExecutionDetails(new ExecutionDetailsEventArgs(reqId, contract, execution));
        }

        /// <summary>
        /// This method is called once all executions have been sent to a client in response to reqExecutions().
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        public override void execDetailsEnd(int reqId)
        {
            OnExecutionDetailsEnd(new RequestEndEventArgs(reqId));
        }

        /// <summary>
        /// This callback returns the commission report portion of an execution and is triggered immediately after a trade execution, or by calling reqExecution().
        /// </summary>
        /// <param name="commissionReport">The structure that contains commission details.</param>
        public override void commissionReport(CommissionReport commissionReport)
        {
            OnCommissionReport(new CommissionReportEventArgs(commissionReport));
        }

        /// <summary>
        /// Receives the historical data in response to reqHistoricalData().
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="bar">The bar data.</param>
        public override void historicalData(int reqId, Bar bar)
        {
            OnHistoricalData(new HistoricalDataEventArgs(reqId, bar));
        }

        /// <summary>
        /// Marks the ending of the historical bars reception.
        /// </summary>
        /// <param name="reqId">The request's identifier.</param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public override void historicalDataEnd(int reqId, string start, string end)
        {
            OnHistoricalDataEnd(new HistoricalDataEndEventArgs(reqId, start, end));
        }

        /// <summary>
        /// This is called once all position data for a given request are received and functions as an end marker for the position() data.
        /// </summary>
        public override void positionEnd()
        {
            OnPositionEnd();
        }

        /// <summary>
        /// This method receives Financial Advisor configuration information from TWS.
        /// </summary>
        /// <param name="faDataType">Specifies the type of Financial Advisor configuration data being received from TWS.</param>
        /// <param name="faXmlData">The XML string containing the previously requested FA configuration information.</param>
        public override void receiveFA(int faDataType, string faXmlData)
        {
            OnReceiveFa(new ReceiveFaEventArgs(faDataType, faXmlData));
        }

        /// <summary>
        /// Callback signifying completion of successful connection.
        /// </summary>
        public override void connectAck()
        {
            OnConnectAck();
        }

        /// <summary>
        /// Receives a comma-separated string with the managed account ids. Occurs automatically on initial API client connection.
        /// </summary>
        /// <param name="accountList">A comma-separated string with the managed account ids.</param>
        public override void managedAccounts(string accountList)
        {
            OnManagedAccounts(new ManagedAccountsEventArgs(accountList));
        }

        /// <summary>
        /// Returns array of family codes
        /// </summary>
        /// <param name="familyCodes">An array of family codes.</param>
        public override void familyCodes(FamilyCode[] familyCodes)
        {
            OnFamilyCodes(new FamilyCodesEventArgs(familyCodes));
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
        /// NextValidId event invocator
        /// </summary>
        protected virtual void OnNextValidId(NextValidIdEventArgs e)
        {
            NextValidId?.Invoke(this, e);
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
        /// HistoricalData event invocator
        /// </summary>
        protected virtual void OnHistoricalData(HistoricalDataEventArgs e)
        {
            HistoricalData?.Invoke(this, e);
        }

        /// <summary>
        /// HistoricalDataEnd event invocator
        /// </summary>
        protected virtual void OnHistoricalDataEnd(HistoricalDataEndEventArgs e)
        {
            HistoricalDataEnd?.Invoke(this, e);
        }

        /// <summary>
        /// PositionEnd event invocator
        /// </summary>
        protected virtual void OnPositionEnd()
        {
            PositionEnd?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ReceiveFa event invocator
        /// </summary>
        protected virtual void OnReceiveFa(ReceiveFaEventArgs e)
        {
            ReceiveFa?.Invoke(this, e);
        }

        /// <summary>
        /// ConnectAck event invocator
        /// </summary>
        protected virtual void OnConnectAck()
        {
            ConnectAck?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ManagedAccounts event invocator
        /// </summary>
        protected virtual void OnManagedAccounts(ManagedAccountsEventArgs e)
        {
            ManagedAccounts?.Invoke(this, e);
        }

        /// <summary>
        /// FamilyCodes event invocator
        /// </summary>
        protected virtual void OnFamilyCodes(FamilyCodesEventArgs e)
        {
            FamilyCodes?.Invoke(this, e);
        }

        #endregion
    }
}
