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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Python;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Portfolio manager class groups popular properties and makes them accessible through one interface.
    /// It also provide indexing by the vehicle symbol to get the Security.Holding objects.
    /// </summary>
    public class SecurityPortfolioManager : ExtendedDictionary<SecurityHolding>, IDictionary<Symbol, SecurityHolding>, ISecurityProvider
    {
        // flips to true when the user called SetCash(), if true, SetAccountCurrency will throw
        private bool _setAccountCurrencyWasCalled;
        private bool _setCashWasCalled;
        private bool _isTotalPortfolioValueValid;
        private decimal _totalPortfolioValue;

        /// <summary>
        /// Local access to the securities collection for the portfolio summation.
        /// </summary>
        public SecurityManager Securities;

        /// <summary>
        /// Local access to the transactions collection for the portfolio summation and updates.
        /// </summary>
        public SecurityTransactionManager Transactions;

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only settled cash)
        /// </summary>
        public CashBook CashBook { get; }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only unsettled cash)
        /// </summary>
        public CashBook UnsettledCashBook { get; }

        /// <summary>
        /// The list of pending funds waiting for settlement time
        /// </summary>
        private readonly List<UnsettledCashAmount> _unsettledCashAmounts;

        // The _unsettledCashAmounts list has to be synchronized because order fills are happening on a separate thread
        private readonly object _unsettledCashAmountsLocker = new object();

        // Record keeping variables
        private Cash _baseCurrencyCash;
        private Cash _baseCurrencyUnsettledCash;

        /// <summary>
        /// Initialise security portfolio manager.
        /// </summary>
        public SecurityPortfolioManager(SecurityManager securityManager, SecurityTransactionManager transactions, IOrderProperties defaultOrderProperties = null)
        {
            Securities = securityManager;
            Transactions = transactions;
            MarginCallModel = new DefaultMarginCallModel(this, defaultOrderProperties);

            CashBook = new CashBook();
            UnsettledCashBook = new CashBook();
            _unsettledCashAmounts = new List<UnsettledCashAmount>();

            _baseCurrencyCash = CashBook[CashBook.AccountCurrency];
            _baseCurrencyUnsettledCash = UnsettledCashBook[CashBook.AccountCurrency];

            // default to $100,000.00
            _baseCurrencyCash.SetAmount(100000);

            CashBook.Updated += (sender, args) => InvalidateTotalPortfolioValue();
            UnsettledCashBook.Updated += (sender, args) => InvalidateTotalPortfolioValue();
        }

        #region IDictionary Implementation

        /// <summary>
        /// Add a new securities string-security to the portfolio.
        /// </summary>
        /// <param name="symbol">Symbol of dictionary</param>
        /// <param name="holding">SecurityHoldings object</param>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public void Add(Symbol symbol, SecurityHolding holding) { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager. To add a new asset add the required data during initialization."); }

        /// <summary>
        /// Add a new securities key value pair to the portfolio.
        /// </summary>
        /// <param name="pair">Key value pair of dictionary</param>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public void Add(KeyValuePair<Symbol, SecurityHolding> pair) { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager. To add a new asset add the required data during initialization."); }

        /// <summary>
        /// Clear the portfolio of securities objects.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public override void Clear() { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager and cannot be cleared."); }

        /// <summary>
        /// Remove this keyvalue pair from the portfolio.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <param name="pair">Key value pair of dictionary</param>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public bool Remove(KeyValuePair<Symbol, SecurityHolding> pair) { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager and objects cannot be removed."); }

        /// <summary>
        /// Remove this symbol from the portfolio.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <param name="symbol">Symbol of dictionary</param>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public override bool Remove(Symbol symbol) { throw new NotImplementedException("Portfolio object is an adaptor for Security Manager and objects cannot be removed."); }

        /// <summary>
        /// Check if the portfolio contains this symbol string.
        /// </summary>
        /// <param name="symbol">String search symbol for the security</param>
        /// <returns>Boolean true if portfolio contains this symbol</returns>
        public bool ContainsKey(Symbol symbol)
        {
            return Securities.ContainsKey(symbol);
        }

        /// <summary>
        /// Check if the key-value pair is in the portfolio.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying Securities collection</remarks>
        /// <param name="pair">Pair we're searching for</param>
        /// <returns>True if we have this object</returns>
        public bool Contains(KeyValuePair<Symbol, SecurityHolding> pair)
        {
            return Securities.ContainsKey(pair.Key);
        }

        /// <summary>
        /// Count the securities objects in the portfolio.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying Securities collection</remarks>
        public int Count
        {
            get
            {
                return Securities.Count;
            }
        }

        /// <summary>
        /// Check if the underlying securities array is read only.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying Securities collection</remarks>
        public override bool IsReadOnly
        {
            get
            {
                return Securities.IsReadOnly;
            }
        }

        /// <summary>
        /// Copy contents of the portfolio collection to a new destination.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying Securities collection</remarks>
        /// <param name="array">Destination array</param>
        /// <param name="index">Position in array to start copying</param>
        public void CopyTo(KeyValuePair<Symbol, SecurityHolding>[] array, int index)
        {
            array = new KeyValuePair<Symbol, SecurityHolding>[Securities.Count];
            var i = 0;
            foreach (var asset in Securities)
            {
                if (i >= index)
                {
                    array[i] = new KeyValuePair<Symbol, SecurityHolding>(asset.Key, asset.Value.Holdings);
                }
                i++;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the Symbol objects of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the Symbol objects of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected override IEnumerable<Symbol> GetKeys => Securities.Select(pair => pair.Key);

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected override IEnumerable<SecurityHolding> GetValues => Securities.Select(pair => pair.Value.Holdings);

        /// <summary>
        /// Symbol keys collection of the underlying assets in the portfolio.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying securities key symbols</remarks>
        public ICollection<Symbol> Keys
        {
            get
            {
                return Securities.Keys;
            }
        }

        /// <summary>
        /// Collection of securities objects in the portfolio.
        /// </summary>
        /// <remarks>IDictionary implementation calling the underlying securities values collection</remarks>
        public ICollection<SecurityHolding> Values
        {
            get
            {
                return (from kvp in Securities
                        select kvp.Value.Holdings).ToList();
            }
        }

        /// <summary>
        /// Attempt to get the value of the securities holding class if this symbol exists.
        /// </summary>
        /// <param name="symbol">String search symbol</param>
        /// <param name="holding">Holdings object of this security</param>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Boolean true if successful locating and setting the holdings object</returns>
        public override bool TryGetValue(Symbol symbol, out SecurityHolding holding)
        {
            Security security;
            var success = Securities.TryGetValue(symbol, out security);
            holding = success ? security.Holdings : null;
            return success;
        }

        /// <summary>
        /// Get the enumerator for the underlying securities collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerable key value pair</returns>
        IEnumerator<KeyValuePair<Symbol, SecurityHolding>> IEnumerable<KeyValuePair<Symbol, SecurityHolding>>.GetEnumerator()
        {
            return Securities.Select(x => new KeyValuePair<Symbol, SecurityHolding>(x.Key, x.Value.Holdings)).GetEnumerator();
        }

        /// <summary>
        /// Get the enumerator for the underlying securities collection.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Securities.Select(x => new KeyValuePair<Symbol, SecurityHolding>(x.Key, x.Value.Holdings)).GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Sum of all currencies in account in US dollars (only settled cash)
        /// </summary>
        /// <remarks>
        /// This should not be mistaken for margin available because Forex uses margin
        /// even though the total cash value is not impact
        /// </remarks>
        public decimal Cash
        {
            get { return CashBook.TotalValueInAccountCurrency; }
        }

        /// <summary>
        /// Sum of all currencies in account in US dollars (only unsettled cash)
        /// </summary>
        /// <remarks>
        /// This should not be mistaken for margin available because Forex uses margin
        /// even though the total cash value is not impact
        /// </remarks>
        public decimal UnsettledCash
        {
            get { return UnsettledCashBook.TotalValueInAccountCurrency; }
        }

        /// <summary>
        /// Absolute value of cash discounted from our total cash by the holdings we own.
        /// </summary>
        /// <remarks>When account has leverage the actual cash removed is a fraction of the purchase price according to the leverage</remarks>
        public decimal TotalUnleveredAbsoluteHoldingsCost
        {
            get
            {
                //Sum of unlevered cost of holdings
                return (from kvp in Securities
                        select kvp.Value.Holdings.UnleveredAbsoluteHoldingsCost).Sum();
            }
        }

        /// <summary>
        /// Gets the total absolute holdings cost of the portfolio. This sums up the individual
        /// absolute cost of each holding
        /// </summary>
        public decimal TotalAbsoluteHoldingsCost
        {
            get { return Securities.Aggregate(0m, (d, pair) => d + pair.Value.Holdings.AbsoluteHoldingsCost); }
        }

        /// <summary>
        /// Absolute sum the individual items in portfolio.
        /// </summary>
        public decimal TotalHoldingsValue
        {
            get
            {
                //Sum sum of holdings
                return (from kvp in Securities
                        select kvp.Value.Holdings.AbsoluteHoldingsValue).Sum();
            }
        }

        /// <summary>
        /// Boolean flag indicating we have any holdings in the portfolio.
        /// </summary>
        /// <remarks>Assumes no asset can have $0 price and uses the sum of total holdings value</remarks>
        /// <seealso cref="Invested"/>
        public bool HoldStock
        {
            get { return TotalHoldingsValue > 0; }
        }

        /// <summary>
        /// Alias for HoldStock. Check if we have and holdings.
        /// </summary>
        /// <seealso cref="HoldStock"/>
        public bool Invested
        {
            get { return HoldStock; }
        }

        /// <summary>
        /// Get the total unrealised profit in our portfolio from the individual security unrealized profits.
        /// </summary>
        public decimal TotalUnrealisedProfit
        {
            get
            {
                return (from kvp in Securities
                        select kvp.Value.Holdings.UnrealizedProfit).Sum();
            }
        }

        /// <summary>
        /// Get the total unrealised profit in our portfolio from the individual security unrealized profits.
        /// </summary>
        /// <remarks>Added alias for American spelling</remarks>
        public decimal TotalUnrealizedProfit
        {
            get { return TotalUnrealisedProfit; }
        }

        /// <summary>
        /// Total portfolio value if we sold all holdings at current market rates.
        /// </summary>
        /// <remarks>Cash + TotalUnrealisedProfit + TotalUnleveredAbsoluteHoldingsCost</remarks>
        /// <seealso cref="Cash"/>
        /// <seealso cref="TotalUnrealizedProfit"/>
        /// <seealso cref="TotalUnleveredAbsoluteHoldingsCost"/>
        public decimal TotalPortfolioValue
        {
            get
            {
                if (!_isTotalPortfolioValueValid)
                {
                    decimal totalHoldingsValueWithoutForexCryptoFutureCfd = 0;
                    decimal totalFuturesAndCfdHoldingsValue = 0;
                    foreach (var kvp in Securities.Where((pair, i) => pair.Value.Holdings.Quantity != 0))
                    {
                        var position = kvp.Value;
                        var securityType = position.Type;
                        // we can't include forex in this calculation since we would be double accounting with respect to the cash book
                        // we also exclude futures and CFD as they are calculated separately
                        if (securityType != SecurityType.Forex && securityType != SecurityType.Crypto &&
                            securityType != SecurityType.Future && securityType != SecurityType.Cfd)
                        {
                            totalHoldingsValueWithoutForexCryptoFutureCfd += position.Holdings.HoldingsValue;
                        }

                        if (securityType == SecurityType.Future || securityType == SecurityType.Cfd)
                        {
                            totalFuturesAndCfdHoldingsValue += position.Holdings.UnrealizedProfit;
                        }
                    }

                    _totalPortfolioValue = CashBook.TotalValueInAccountCurrency +
                       UnsettledCashBook.TotalValueInAccountCurrency +
                       totalHoldingsValueWithoutForexCryptoFutureCfd +
                       totalFuturesAndCfdHoldingsValue;

                    _isTotalPortfolioValueValid = true;
                }

                return _totalPortfolioValue;
            }
        }

        /// <summary>
        /// Will flag the current <see cref="TotalPortfolioValue"/> as invalid
        /// so it is recalculated when gotten
        /// </summary>
        public void InvalidateTotalPortfolioValue()
        {
            _isTotalPortfolioValueValid = false;
        }

        /// <summary>
        /// Total fees paid during the algorithm operation across all securities in portfolio.
        /// </summary>
        public decimal TotalFees
        {
            get
            {
                return (from kvp in Securities
                        select kvp.Value.Holdings.TotalFees).Sum();
            }
        }

        /// <summary>
        /// Sum of all gross profit across all securities in portfolio.
        /// </summary>
        public decimal TotalProfit
        {
            get
            {
                return (from kvp in Securities
                        select kvp.Value.Holdings.Profit).Sum();
            }
        }

        /// <summary>
        /// Total sale volume since the start of algorithm operations.
        /// </summary>
        public decimal TotalSaleVolume
        {
            get
            {
                return (from kvp in Securities
                        select kvp.Value.Holdings.TotalSaleVolume).Sum();
            }
        }

        /// <summary>
        /// Gets the total margin used across all securities in the account's currency
        /// </summary>
        public decimal TotalMarginUsed
        {
            get
            {
                decimal sum = 0;
                foreach (var kvp in Securities.Where((pair, i) => pair.Value.Holdings.Quantity != 0))
                {
                    var security = kvp.Value;
                    var context = new ReservedBuyingPowerForPositionParameters(security);
                    var reservedBuyingPower = security.BuyingPowerModel.GetReservedBuyingPowerForPosition(context);
                    sum += reservedBuyingPower.AbsoluteUsedBuyingPower;
                }
                return sum;
            }
        }

        /// <summary>
        /// Gets the remaining margin on the account in the account's currency
        /// </summary>
        /// <see cref="GetMarginRemaining(decimal)"/>
        public decimal MarginRemaining => GetMarginRemaining(TotalPortfolioValue);

        /// <summary>
        /// Gets the remaining margin on the account in the account's currency
        /// for the given total portfolio value
        /// </summary>
        /// <remarks>This method is for performance, for when the user already knows
        /// the total portfolio value, we can avoid re calculating it. Else use
        /// <see cref="MarginRemaining"/></remarks>
        /// <param name="totalPortfolioValue">The total portfolio value <see cref="TotalPortfolioValue"/></param>
        public decimal GetMarginRemaining(decimal totalPortfolioValue)
        {
            return totalPortfolioValue - UnsettledCashBook.TotalValueInAccountCurrency - TotalMarginUsed;
        }

        /// <summary>
        /// Gets or sets the <see cref="MarginCallModel"/> for the portfolio. This
        /// is used to executed margin call orders.
        /// </summary>
        public IMarginCallModel MarginCallModel { get; set; }

        /// <summary>
        /// Indexer for the PortfolioManager class to access the underlying security holdings objects.
        /// </summary>
        /// <param name="symbol">Symbol object indexer</param>
        /// <returns>SecurityHolding class from the algorithm securities</returns>
        public override SecurityHolding this[Symbol symbol]
        {
            get { return Securities[symbol].Holdings; }
            set { Securities[symbol].Holdings = value; }
        }

        /// <summary>
        /// Sets the account currency cash symbol this algorithm is to manage.
        /// </summary>
        /// <remarks>Has to be called before calling <see cref="SetCash(decimal)"/>
        /// or adding any <see cref="Security"/></remarks>
        /// <param name="accountCurrency">The account currency cash symbol to set</param>
        public void SetAccountCurrency(string accountCurrency)
        {
            accountCurrency = accountCurrency.LazyToUpper();

            // only allow setting account currency once
            // we could try to set it twice when backtesting and the job packet specifies the initial CashAmount to use
            if (_setAccountCurrencyWasCalled)
            {
                if (accountCurrency != CashBook.AccountCurrency)
                {
                    Log.Trace("SecurityPortfolioManager.SetAccountCurrency():" +
                              $" account currency has already been set to {CashBook.AccountCurrency}." +
                              $" Will ignore new value {accountCurrency}");
                }
                return;
            }
            _setAccountCurrencyWasCalled = true;

            if (Securities.Count > 0)
            {
                throw new InvalidOperationException("SecurityPortfolioManager.SetAccountCurrency(): " +
                    "Cannot change AccountCurrency after adding a Security. " +
                    "Please move SetAccountCurrency() before AddSecurity().");
            }

            if (_setCashWasCalled)
            {
                throw new InvalidOperationException("SecurityPortfolioManager.SetAccountCurrency(): " +
                    "Cannot change AccountCurrency after setting cash. " +
                    "Please move SetAccountCurrency() before SetCash().");
            }

            Log.Trace("SecurityPortfolioManager.SetAccountCurrency():" +
                $" setting account currency to {accountCurrency}");

            UnsettledCashBook.AccountCurrency = accountCurrency;
            CashBook.AccountCurrency = accountCurrency;

            _baseCurrencyCash = CashBook[accountCurrency];
            _baseCurrencyUnsettledCash = UnsettledCashBook[accountCurrency];
        }

        /// <summary>
        /// Set the account currency cash this algorithm is to manage.
        /// </summary>
        /// <param name="cash">Decimal cash value of portfolio</param>
        public void SetCash(decimal cash)
        {
            _setCashWasCalled = true;
            _baseCurrencyCash.SetAmount(cash);
        }

        /// <summary>
        /// Set the cash for the specified symbol
        /// </summary>
        /// <param name="symbol">The cash symbol to set</param>
        /// <param name="cash">Decimal cash value of portfolio</param>
        /// <param name="conversionRate">The current conversion rate for the</param>
        public void SetCash(string symbol, decimal cash, decimal conversionRate)
        {
            _setCashWasCalled = true;
            Cash item;
            if (CashBook.TryGetValue(symbol, out item))
            {
                item.SetAmount(cash);
                item.ConversionRate = conversionRate;
            }
            else
            {
                CashBook.Add(symbol, cash, conversionRate);
            }
        }

        /// <summary>
        /// Gets the margin available for trading a specific symbol in a specific direction.
        /// </summary>
        /// <param name="symbol">The symbol to compute margin remaining for</param>
        /// <param name="direction">The order/trading direction</param>
        /// <returns>The maximum order size that is currently executable in the specified direction</returns>
        public decimal GetMarginRemaining(Symbol symbol, OrderDirection direction = OrderDirection.Buy)
        {
            var security = Securities[symbol];
            var context = new BuyingPowerParameters(this, security, direction);
            return security.BuyingPowerModel.GetBuyingPower(context).Value;
        }

        /// <summary>
        /// Gets the margin available for trading a specific symbol in a specific direction.
        /// Alias for <see cref="GetMarginRemaining"/>
        /// </summary>
        /// <param name="symbol">The symbol to compute margin remaining for</param>
        /// <param name="direction">The order/trading direction</param>
        /// <returns>The maximum order size that is currently executable in the specified direction</returns>
        public decimal GetBuyingPower(Symbol symbol, OrderDirection direction = OrderDirection.Buy)
        {
            return GetMarginRemaining(symbol, direction);
        }

        /// <summary>
        /// Calculate the new average price after processing a partial/complete order fill event.
        /// </summary>
        /// <remarks>
        ///     For purchasing stocks from zero holdings, the new average price is the sale price.
        ///     When simply partially reducing holdings the average price remains the same.
        ///     When crossing zero holdings the average price becomes the trade price in the new side of zero.
        /// </remarks>
        public virtual void ProcessFill(OrderEvent fill)
        {
            var security = Securities[fill.Symbol];
            security.PortfolioModel.ProcessFill(this, security, fill);
            InvalidateTotalPortfolioValue();
        }

        /// <summary>
        /// Applies a dividend to the portfolio
        /// </summary>
        /// <param name="dividend">The dividend to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="mode">The <see cref="DataNormalizationMode"/> for this security</param>
        public void ApplyDividend(Dividend dividend, bool liveMode, DataNormalizationMode mode)
        {
            // we currently don't properly model dividend payable dates, so in
            // live mode it's more accurate to rely on the brokerage cash sync
            if (liveMode)
            {
                return;
            }

            var security = Securities[dividend.Symbol];

            // only apply dividends when we're in raw mode or split adjusted mode
            if (mode == DataNormalizationMode.Raw || mode == DataNormalizationMode.SplitAdjusted)
            {
                // longs get benefits, shorts get clubbed on dividends
                var total = security.Holdings.Quantity*dividend.Distribution;

                // assuming USD, we still need to add Currency to the security object
                _baseCurrencyCash.AddAmount(total);
            }
        }

        /// <summary>
        /// Applies a split to the portfolio
        /// </summary>
        /// <param name="split">The split to be applied</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="mode">The <see cref="DataNormalizationMode"/> for this security</param>
        public void ApplySplit(Split split, bool liveMode, DataNormalizationMode mode)
        {
            var security = Securities[split.Symbol];

            // only apply splits to equities
            if (security.Type != SecurityType.Equity)
            {
                return;
            }

            // only apply splits in live or raw data mode
            if (!liveMode && mode != DataNormalizationMode.Raw)
            {
                return;
            }

            // we need to modify our holdings in lght of the split factor
            var quantity = security.Holdings.Quantity / split.SplitFactor;
            var avgPrice = security.Holdings.AveragePrice * split.SplitFactor;

            // we'll model this as a cash adjustment
            var leftOver = quantity - (int)quantity;
            var extraCash = leftOver * split.ReferencePrice;
            _baseCurrencyCash.AddAmount(extraCash);

            security.Holdings.SetHoldings(avgPrice, (int)quantity);

            // build a 'next' value to update the market prices in light of the split factor
            var next = security.GetLastData();
            if (next == null)
            {
                // sometimes we can get splits before we receive data which
                // will cause this to return null, in this case we can't possibly
                // have any holdings or price to set since we haven't received
                // data yet, so just do nothing
                return;
            }
            next.Value *= split.SplitFactor;

            // make sure to modify open/high/low as well for tradebar data types
            var tradeBar = next as TradeBar;
            if (tradeBar != null)
            {
                tradeBar.Open *= split.SplitFactor;
                tradeBar.High *= split.SplitFactor;
                tradeBar.Low *= split.SplitFactor;
            }

            // make sure to modify bid/ask as well for tradebar data types
            var tick = next as Tick;
            if (tick != null)
            {
                tick.AskPrice *= split.SplitFactor;
                tick.BidPrice *= split.SplitFactor;
            }

            security.SetMarketPrice(next);
            // security price updated
            InvalidateTotalPortfolioValue();
        }

        /// <summary>
        /// Record the transaction value and time in a list to later be processed for statistics creation.
        /// </summary>
        /// <param name="time">Time of order processed </param>
        /// <param name="transactionProfitLoss">Profit Loss.</param>
        public void AddTransactionRecord(DateTime time, decimal transactionProfitLoss)
        {
            Transactions.AddTransactionRecord(time, transactionProfitLoss);
        }

        /// <summary>
        /// Retrieves a summary of the holdings for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to get holdings for</param>
        /// <returns>The holdings for the symbol or null if the symbol is invalid and/or not in the portfolio</returns>
        Security ISecurityProvider.GetSecurity(Symbol symbol)
        {
            Security security;

            if (Securities.TryGetValue(symbol, out security))
            {
                return security;
            }

            return null;
        }

        /// <summary>
        /// Adds an item to the list of unsettled cash amounts
        /// </summary>
        /// <param name="item">The item to add</param>
        public void AddUnsettledCashAmount(UnsettledCashAmount item)
        {
            lock (_unsettledCashAmountsLocker)
            {
                _unsettledCashAmounts.Add(item);
            }
        }

        /// <summary>
        /// Scan the portfolio to check if unsettled funds should be settled
        /// </summary>
        public void ScanForCashSettlement(DateTime timeUtc)
        {
            lock (_unsettledCashAmountsLocker)
            {
                foreach (var item in _unsettledCashAmounts.ToList())
                {
                    // check if settlement time has passed
                    if (timeUtc >= item.SettlementTimeUtc)
                    {
                        // remove item from unsettled funds list
                        _unsettledCashAmounts.Remove(item);

                        // update unsettled cashbook
                        UnsettledCashBook[item.Currency].AddAmount(-item.Amount);

                        // update settled cashbook
                        CashBook[item.Currency].AddAmount(item.Amount);
                    }
                }
            }
        }

        /// <summary>
        /// Logs margin information for debugging
        /// </summary>
        public void LogMarginInformation(OrderRequest orderRequest = null)
        {
            Log.Trace("Total margin information: " +
                  Invariant($"TotalMarginUsed: {TotalMarginUsed:F2}, ") +
                  Invariant($"MarginRemaining: {MarginRemaining:F2}")
              );

            var orderSubmitRequest = orderRequest as SubmitOrderRequest;
            if (orderSubmitRequest != null)
            {
                var direction = orderSubmitRequest.Quantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
                var security = Securities[orderSubmitRequest.Symbol];

                var marginUsed = security.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                    new ReservedBuyingPowerForPositionParameters(security)
                );

                var marginRemaining = security.BuyingPowerModel.GetBuyingPower(
                    new BuyingPowerParameters(this, security, direction)
                );

                Log.Trace("Order request margin information: " +
                    Invariant($"MarginUsed: {marginUsed.AbsoluteUsedBuyingPower:F2}, ") +
                    Invariant($"MarginRemaining: {marginRemaining.Value:F2}")
                );
            }
        }

        /// <summary>
        /// Sets the margin call model
        /// </summary>
        /// <param name="marginCallModel">Model that represents a portfolio's model to executed margin call orders.</param>
        public void SetMarginCallModel(IMarginCallModel marginCallModel)
        {
            MarginCallModel = marginCallModel;
        }

        /// <summary>
        /// Sets the margin call model
        /// </summary>
        /// <param name="pyObject">Model that represents a portfolio's model to executed margin call orders.</param>
        public void SetMarginCallModel(PyObject pyObject)
        {
            SetMarginCallModel(new MarginCallModelPythonWrapper(pyObject));
        }
    }
}
