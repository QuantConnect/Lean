using System;
using System.Reflection;
using System.Threading;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public class InteractiveBrokersClient : EClientSocket, EWrapper, IDisposable
    {
        public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler<CurrentTimeEventArgs> CurrentTimeUtc;
        public event EventHandler<TickPriceEventArgs> TickPrice;
        public event EventHandler<TickSizeEventArgs> TickSize;
        public event EventHandler<TickStringEventArgs> TickString;
        public event EventHandler<TickGenericEventArgs> TickGeneric;
        public event EventHandler<TickEfpEventArgs> TickEfp;
        public event EventHandler<DeltaNeutralValidationEventArgs> DeltaNeutralValidation;
        public event EventHandler<TickOptionComputationEventArgs> TickOptionComputation;
        public event EventHandler<TickSnapshotEndEventArgs> TickSnapshotEnd;
        public event EventHandler<NextValidIdEventArgs> NextValidId;
        public event EventHandler<ManagedAccountsEventArgs> ManagedAccounts;
        public event EventHandler ConnectionClosed;
        public event EventHandler<AccountSummaryEventArgs> AccountSummary;
        public event EventHandler<RequestEndEventArgs> AccountSummaryEnd;
        public event EventHandler<BondContractDetailsEventArgs> BondContractDetails;
        public event EventHandler<UpdateAccountValueEventArgs> UpdateAccountValue;
        public event EventHandler<UpdatePortfolioEventArgs> UpdatePortfolio;
        public event EventHandler<UpdateAccountTimeEventArgs> UpdateAccountTime;
        public event EventHandler<AccountDownloadEndEventArgs> AccountDownloadEnd;
        public event EventHandler<OrderStatusEventArgs> OrderStatus;
        public event EventHandler<OpenOrderEventArgs> OpenOrder;
        public event EventHandler OpenOrderEnd;
        public event EventHandler<ContractDetailsEventArgs> ContractDetails;
        public event EventHandler<RequestEndEventArgs> ContractDetailsEnd;
        public event EventHandler<ExecutionDetailsEventArgs> ExecutionDetails;
        public event EventHandler<RequestEndEventArgs> ExecutionDetailsEnd;
        public event EventHandler<CommissionReportEventArgs> CommissionReport;
        public event EventHandler<FundamentalDataEventArgs> FundamentalData;
        public event EventHandler<HistoricalDataEventArgs> HistoricalData;
        public event EventHandler<HistoricalDataEndEventArgs> HistoricalDataEnd;
        public event EventHandler<MarketDataTypeEventArgs> MarketDataType;
        public event EventHandler<UpdateMarketDepthEventArgs> UpdateMarketDepth;
        public event EventHandler<UpdateMarketDepthLevel2EventArgs> UpdateMarketDepthLevel2;
        public event EventHandler<UpdateNewsBulletinEventArgs> UpdateNewsBulletin;
        public event EventHandler<PositionEventArgs> Position;
        public event EventHandler PositionEnd;
        public event EventHandler<RealtimeBarEventArgs> RealtimeBar;

        public InteractiveBrokersClient()
            : base(null)
        {
            // ::HACK ALERT::
            // total hack, but we want ourselves to be the ewrapper and the base clases doesn't privde a means for us to do that
            typeof(EClientSocket).GetField("wrapper", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, this);
        }
        
        public void Dispose()
        {
            // disconnect on dispose
            eDisconnect();
        }

        #region EWrapper Implementation

        public void error(Exception e)
        {
            error(-1, -1, e.ToString());
        }

        public void error(string str)
        {
            error(-1, -1, str);
        }

        public void error(int id, int errorCode, string errorMsg)
        {
            OnError(new ErrorEventArgs(id, errorCode, errorMsg));
        }

        public void currentTime(long time)
        {
            var currentTimeUtc = new DateTime(time, DateTimeKind.Utc);
            OnCurrentTimeUtc(new CurrentTimeEventArgs(currentTimeUtc));
        }

        public void tickPrice(int tickerId, int field, double price, int canAutoExecute)
        {
            OnTickPrice(new TickPriceEventArgs(tickerId, field, price, canAutoExecute));
        }

        public void tickSize(int tickerId, int field, int size)
        {
            OnTickSize(new TickSizeEventArgs(tickerId, field, size));
        }

        public void tickString(int tickerId, int field, string value)
        {
            OnTickString(new TickStringEventArgs(tickerId, field, value));
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            OnTickGeneric(new TickGenericEventArgs(tickerId, field, value));
        }

        public void tickEFP(int tickerId,
            int tickType,
            double basisPoints,
            string formattedBasisPoints,
            double impliedFuture,
            int holdDays,
            string futureExpiry,
            double dividendImpact,
            double dividendsToExpiry)
        {
            OnTickEfp(new TickEfpEventArgs(tickerId, tickType, basisPoints, formattedBasisPoints, impliedFuture, holdDays, futureExpiry, dividendImpact,
                dividendsToExpiry));
        }

        public void deltaNeutralValidation(int reqId, UnderComp underComp)
        {
            OnDeltaNeutralValidation(new DeltaNeutralValidationEventArgs(reqId, underComp));
        }

        public void tickOptionComputation(int tickerId,
            int field,
            double impliedVolatility,
            double delta,
            double optPrice,
            double pvDividend,
            double gamma,
            double vega,
            double theta,
            double undPrice)
        {
            OnTickOptionComputation(new TickOptionComputationEventArgs(tickerId, field, impliedVolatility, delta, optPrice, pvDividend, gamma, vega, theta,
                undPrice));
        }

        public void tickSnapshotEnd(int tickerId)
        {
            OnTickSnapshotEnd(new TickSnapshotEndEventArgs(tickerId));
        }

        public void nextValidId(int orderId)
        {
            OnNextValidId(new NextValidIdEventArgs(orderId));
        }

        public void managedAccounts(string accountsList)
        {
            OnManagedAccounts(new ManagedAccountsEventArgs(accountsList));
        }

        public void connectionClosed()
        {
            OnConnectionClosed();
        }

        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            OnAccountSummary(new AccountSummaryEventArgs(reqId, account, tag, value, currency));
        }

        public void accountSummaryEnd(int reqId)
        {
            OnAccountSummaryEnd(new RequestEndEventArgs(reqId));
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            OnBondContractDetails(new BondContractDetailsEventArgs(reqId, contract));
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            OnUpdateAccountValue(new UpdateAccountValueEventArgs(key, value, currency, accountName));
        }

        public void updatePortfolio(Contract contract,
            int position,
            double marketPrice,
            double marketValue,
            double averageCost,
            double unrealisedPNL,
            double realisedPNL,
            string accountName)
        {
            OnUpdatePortfolio(new UpdatePortfolioEventArgs(contract, position, marketPrice, marketValue, averageCost, unrealisedPNL, realisedPNL,
                accountName));
        }

        public void updateAccountTime(string timestamp)
        {
            OnUpdateAccountTime(new UpdateAccountTimeEventArgs(timestamp));
        }

        public void accountDownloadEnd(string account)
        {
            OnAccountDownloadEnd(new AccountDownloadEndEventArgs(account));
        }

        public void orderStatus(int orderId,
            string status,
            int filled,
            int remaining,
            double avgFillPrice,
            int permId,
            int parentId,
            double lastFillPrice,
            int clientId,
            string whyHeld)
        {
            OnOrderStatus(new OrderStatusEventArgs(orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld));
        }

        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            OnOpenOrder(new OpenOrderEventArgs(orderId, contract, order, orderState));
        }

        public void openOrderEnd()
        {
            OnOpenOrderEnd();
        }

        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            OnContractDetails(new ContractDetailsEventArgs(reqId, contractDetails));
        }

        public void contractDetailsEnd(int reqId)
        {
            OnContractDetailsEnd(new RequestEndEventArgs(reqId));
        }

        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            OnExecutionDetails(new ExecutionDetailsEventArgs(reqId, contract, execution));
        }

        public void execDetailsEnd(int reqId)
        {
            OnExecutionDetailsEnd(new RequestEndEventArgs(reqId));
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            OnCommissionReport(new CommissionReportEventArgs(commissionReport));
        }

        public void fundamentalData(int reqId, string data)
        {
            OnFundamentalData(new FundamentalDataEventArgs(reqId, data));
        }

        public void historicalData(int reqId,
            string date,
            double open,
            double high,
            double low,
            double close,
            int volume,
            int count,
            double WAP,
            bool hasGaps)
        {
            OnHistoricalData(new HistoricalDataEventArgs(reqId, date, open, high, low, close, volume, count, WAP, hasGaps));
        }

        public void historicalDataEnd(int reqId, string start, string end)
        {
            OnHistoricalDataEnd(new HistoricalDataEndEventArgs(reqId, start, end));
        }

        public void marketDataType(int reqId, int marketDataType)
        {
            OnMarketDataType(new MarketDataTypeEventArgs(reqId, marketDataType));
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            OnUpdateMarketDepth(new UpdateMarketDepthEventArgs(tickerId, position, operation, side, price, size));
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size)
        {
            OnUpdateMarketDepthLevel2(new UpdateMarketDepthLevel2EventArgs(tickerId, position, marketMaker, operation, side, price, size));
        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            OnUpdateNewsBulletin(new UpdateNewsBulletinEventArgs(msgId, msgType, message, origExchange));
        }

        public void position(string account, Contract contract, int pos, double avgCost)
        {
            OnPosition(new PositionEventArgs(account, contract, pos, avgCost));
        }

        public void positionEnd()
        {
            OnPositionEnd();
        }

        public void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            OnRealtimeBar(new RealtimeBarEventArgs(reqId, time, open, high, low, close, volume, WAP, count));
        }

        public void scannerParameters(string xml)
        {
            throw new NotImplementedException();
        }

        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            throw new NotImplementedException();
        }

        public void scannerDataEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            throw new NotImplementedException();
        }

        public void verifyMessageAPI(string apiData)
        {
            throw new NotImplementedException();
        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }

        public void displayGroupList(int reqId, string groups)
        {
            throw new NotImplementedException();
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            throw new NotImplementedException();
        }

        #endregion
        
        #region Event Invocators

        protected virtual void OnError(ErrorEventArgs e)
        {
            var handler = Error;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnCurrentTimeUtc(CurrentTimeEventArgs e)
        {
            var handler = CurrentTimeUtc;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnTickPrice(TickPriceEventArgs e)
        {
            var handler = TickPrice;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnTickSize(TickSizeEventArgs e)
        {
            var handler = TickSize;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnTickString(TickStringEventArgs e)
        {
            var handler = TickString;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnTickGeneric(TickGenericEventArgs e)
        {
            var handler = TickGeneric;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnTickEfp(TickEfpEventArgs e)
        {
            var handler = TickEfp;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnDeltaNeutralValidation(DeltaNeutralValidationEventArgs e)
        {
            var handler = DeltaNeutralValidation;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnTickOptionComputation(TickOptionComputationEventArgs e)
        {
            var handler = TickOptionComputation;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnTickSnapshotEnd(TickSnapshotEndEventArgs e)
        {
            var handler = TickSnapshotEnd;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnNextValidId(NextValidIdEventArgs e)
        {
            var handler = NextValidId;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnManagedAccounts(ManagedAccountsEventArgs e)
        {
            var handler = ManagedAccounts;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnConnectionClosed()
        {
            var handler = ConnectionClosed;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnAccountSummary(AccountSummaryEventArgs e)
        {
            var handler = AccountSummary;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnAccountSummaryEnd(RequestEndEventArgs e)
        {
            var handler = AccountSummaryEnd;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnBondContractDetails(BondContractDetailsEventArgs e)
        {
            var handler = BondContractDetails;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnUpdateAccountValue(UpdateAccountValueEventArgs e)
        {
            var handler = UpdateAccountValue;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnUpdatePortfolio(UpdatePortfolioEventArgs e)
        {
            var handler = UpdatePortfolio;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnUpdateAccountTime(UpdateAccountTimeEventArgs e)
        {
            var handler = UpdateAccountTime;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnAccountDownloadEnd(AccountDownloadEndEventArgs e)
        {
            var handler = AccountDownloadEnd;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnOrderStatus(OrderStatusEventArgs e)
        {
            var handler = OrderStatus;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnOpenOrder(OpenOrderEventArgs e)
        {
            var handler = OpenOrder;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnOpenOrderEnd()
        {
            var handler = OpenOrderEnd;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnContractDetails(ContractDetailsEventArgs e)
        {
            var handler = ContractDetails;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnContractDetailsEnd(RequestEndEventArgs e)
        {
            var handler = ContractDetailsEnd;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnExecutionDetails(ExecutionDetailsEventArgs e)
        {
            var handler = ExecutionDetails;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnExecutionDetailsEnd(RequestEndEventArgs e)
        {
            var handler = ExecutionDetailsEnd;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnCommissionReport(CommissionReportEventArgs e)
        {
            var handler = CommissionReport;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnFundamentalData(FundamentalDataEventArgs e)
        {
            var handler = FundamentalData;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnHistoricalData(HistoricalDataEventArgs e)
        {
            var handler = HistoricalData;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnHistoricalDataEnd(HistoricalDataEndEventArgs e)
        {
            var handler = HistoricalDataEnd;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnMarketDataType(MarketDataTypeEventArgs e)
        {
            var handler = MarketDataType;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnUpdateMarketDepth(UpdateMarketDepthEventArgs e)
        {
            var handler = UpdateMarketDepth;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnUpdateMarketDepthLevel2(UpdateMarketDepthLevel2EventArgs e)
        {
            var handler = UpdateMarketDepthLevel2;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnUpdateNewsBulletin(UpdateNewsBulletinEventArgs e)
        {
            var handler = UpdateNewsBulletin;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnPosition(PositionEventArgs e)
        {
            var handler = Position;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnPositionEnd()
        {
            var handler = PositionEnd;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnRealtimeBar(RealtimeBarEventArgs e)
        {
            var handler = RealtimeBar;
            if (handler != null) handler(this, e);
        }

        #endregion
    }
}
