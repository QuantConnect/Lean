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
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Portfolio manager class groups popular properties and makes them accessible through one interface.
    /// It also provide indexing by the vehicle symbol to get the Security.Holding objects.
    /// </summary>
    public class SecurityPortfolioManager : ExtendedDictionary<Symbol, SecurityHolding>, IDictionary<Symbol, SecurityHolding>, ISecurityProvider
    {
        private Cash _baseCurrencyCash;
        private bool _setCashWasCalled;
        private decimal _totalPortfolioValue;
        private bool _isTotalPortfolioValueValid;
        private object _totalPortfolioValueLock = new();
        private bool _setAccountCurrencyWasCalled;
        private decimal _freePortfolioValue;
        private SecurityPositionGroupModel _positions;
        private IAlgorithmSettings _algorithmSettings;

        /// <summary>
        /// Local access to the securities collection for the portfolio summation.
        /// </summary>
        public SecurityManager Securities { get; init; }

        /// <summary>
        /// Local access to the transactions collection for the portfolio summation and updates.
        /// </summary>
        public SecurityTransactionManager Transactions { get; init; }

        /// <summary>
        /// Local access to the position manager
        /// </summary>
        public SecurityPositionGroupModel Positions
        {
            get
            {
                return _positions;
            }
            set
            {
                value?.Initialize(Securities);
                _positions = value;
            }
        }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only settled cash)
        /// </summary>
        public CashBook CashBook { get; }

        /// <summary>
        /// Gets the cash book that keeps track of all currency holdings (only unsettled cash)
        /// </summary>
        public CashBook UnsettledCashBook { get; }

        /// <summary>
        /// Initialise security portfolio manager.
        /// </summary>
        public SecurityPortfolioManager(SecurityManager securityManager, SecurityTransactionManager transactions, IAlgorithmSettings algorithmSettings, IOrderProperties defaultOrderProperties = null)
        {
            Securities = securityManager;
            Transactions = transactions;
            _algorithmSettings = algorithmSettings;
            Positions = new SecurityPositionGroupModel();
            MarginCallModel = new DefaultMarginCallModel(this, defaultOrderProperties);

            CashBook = new CashBook();
            UnsettledCashBook = new CashBook();

            _baseCurrencyCash = CashBook[CashBook.AccountCurrency];

            // default to $100,000.00
            _baseCurrencyCash.SetAmount(100000);

            CashBook.Updated += (sender, args) =>
            {
                if (args.UpdateType == CashBookUpdateType.Added)
                {
                    // add the same currency entry to the unsettled cashbook as well
                    var cash = args.Cash;
                    var unsettledCash = new Cash(cash.Symbol, 0m, cash.ConversionRate);
                    unsettledCash.CurrencyConversion = cash.CurrencyConversion;

                    cash.CurrencyConversionUpdated += (sender, args) =>
                    {
                        // Share the currency conversion instance between the settled and unsettled cash instances to synchronize the conversion rates
                        UnsettledCashBook[((Cash)sender).Symbol].CurrencyConversion = cash.CurrencyConversion;
                    };

                    UnsettledCashBook.Add(cash.Symbol, unsettledCash);
                }

                InvalidateTotalPortfolioValue();
            };
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
        public void Add(Symbol symbol, SecurityHolding holding) { throw new NotImplementedException(Messages.SecurityPortfolioManager.DictionaryAddNotImplemented); }

        /// <summary>
        /// Add a new securities key value pair to the portfolio.
        /// </summary>
        /// <param name="pair">Key value pair of dictionary</param>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public void Add(KeyValuePair<Symbol, SecurityHolding> pair) { throw new NotImplementedException(Messages.SecurityPortfolioManager.DictionaryAddNotImplemented); }

        /// <summary>
        /// Clear the portfolio of securities objects.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public override void Clear() { throw new NotImplementedException(Messages.SecurityPortfolioManager.DictionaryClearNotImplemented); }

        /// <summary>
        /// Remove this keyvalue pair from the portfolio.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <param name="pair">Key value pair of dictionary</param>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public bool Remove(KeyValuePair<Symbol, SecurityHolding> pair) { throw new NotImplementedException(Messages.SecurityPortfolioManager.DictionaryRemoveNotImplemented); }

        /// <summary>
        /// Remove this symbol from the portfolio.
        /// </summary>
        /// <exception cref="NotImplementedException">Portfolio object is an adaptor for Security Manager. This method is not applicable for PortfolioManager class.</exception>
        /// <param name="symbol">Symbol of dictionary</param>
        /// <remarks>This method is not implemented and using it will throw an exception</remarks>
        public override bool Remove(Symbol symbol) { throw new NotImplementedException(Messages.SecurityPortfolioManager.DictionaryRemoveNotImplemented); }

        /// <summary>
        /// Check if the portfolio contains this symbol string.
        /// </summary>
        /// <param name="symbol">String search symbol for the security</param>
        /// <returns>Boolean true if portfolio contains this symbol</returns>
        public override bool ContainsKey(Symbol symbol)
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
        public override int Count
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
            foreach (var asset in Securities.Values)
            {
                if (i >= index)
                {
                    array[i] = new KeyValuePair<Symbol, SecurityHolding>(asset.Symbol, asset.Holdings);
                }
                i++;
            }
        }

        /// <summary>
        /// Gets all the items in the dictionary
        /// </summary>
        /// <returns>All the items in the dictionary</returns>
        public override IEnumerable<KeyValuePair<Symbol, SecurityHolding>> GetItems() =>
            Securities.GetItems().Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.Holdings));

        /// <summary>
        /// Gets an <see cref="System.Collections.Generic.ICollection{T}"/> containing the Symbol objects of the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="System.Collections.Generic.ICollection{T}"/> containing the Symbol objects of the object that implements <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.
        /// </returns>
        protected override IEnumerable<Symbol> GetKeys => Keys;

        /// <summary>
        /// Gets an <see cref="System.Collections.Generic.ICollection{T}"/> containing the values in the <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="System.Collections.Generic.ICollection{T}"/> containing the values in the object that implements <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>.
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
                return GetValues.ToList();
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
                return Securities.Values.Sum(security => security.Holdings.UnleveredAbsoluteHoldingsCost);
            }
        }

        /// <summary>
        /// Gets the total absolute holdings cost of the portfolio. This sums up the individual
        /// absolute cost of each holding
        /// </summary>
        public decimal TotalAbsoluteHoldingsCost
        {
            get
            {
                return Securities.Values.Sum(security => security.Holdings.AbsoluteHoldingsCost);
            }
        }

        /// <summary>
        /// Absolute sum the individual items in portfolio.
        /// </summary>
        public decimal TotalHoldingsValue
        {
            get
            {
                //Sum sum of holdings
                return Securities.Values.Sum(security => security.Holdings.AbsoluteHoldingsValue);
            }
        }

        /// <summary>
        /// Boolean flag indicating we have any holdings in the portfolio.
        /// </summary>
        /// <remarks>Assumes no asset can have $0 price and uses the sum of total holdings value</remarks>
        /// <seealso cref="Invested"/>
        public bool HoldStock
        {
            get
            {
                foreach (var security in Securities.Values)
                {
                    if (security.HoldStock)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Alias for HoldStock. Check if we have any holdings.
        /// </summary>
        /// <seealso cref="HoldStock"/>
        public bool Invested => HoldStock;

        /// <summary>
        /// Get the total unrealised profit in our portfolio from the individual security unrealized profits.
        /// </summary>
        public decimal TotalUnrealisedProfit
        {
            get
            {
                return Securities.Values.Sum(security => security.Holdings.UnrealizedProfit);
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
                lock (_totalPortfolioValueLock)
                {
                    if (!_isTotalPortfolioValueValid)
                    {
                        decimal totalHoldingsValueWithoutForexCryptoFutureCfd = 0;
                        decimal totalFuturesAndCfdHoldingsValue = 0;
                        foreach (var security in Securities.Values.Where((x) => x.Holdings.Invested))
                        {
                            var position = security;
                            var securityType = position.Type;
                            // We can't include forex in this calculation since we would be double accounting with respect to the cash book
                            // We also exclude futures and CFD as they are calculated separately because they do not impact the account's cash.
                            // We include futures options as part of this calculation because IB chooses to change our account's cash balance
                            // when we buy or sell a futures options contract.
                            if (securityType != SecurityType.Forex && securityType != SecurityType.Crypto
                                && securityType != SecurityType.Future && securityType != SecurityType.Cfd
                                && securityType != SecurityType.CryptoFuture)
                            {
                                totalHoldingsValueWithoutForexCryptoFutureCfd += position.Holdings.HoldingsValue;
                            }

                            // CFDs don't impact account cash, so they must be calculated
                            // by applying the unrealized P&L to the cash balance.
                            if (securityType == SecurityType.Cfd || securityType == SecurityType.CryptoFuture)
                            {
                                totalFuturesAndCfdHoldingsValue += position.Holdings.UnrealizedProfit;
                            }
                            // Futures P&L is settled daily into cash, here we take into account the current days unsettled profit
                            if (securityType == SecurityType.Future)
                            {
                                var futureHoldings = (FutureHolding)position.Holdings;
                                totalFuturesAndCfdHoldingsValue += futureHoldings.UnsettledProfit;
                            }
                        }

                        _totalPortfolioValue = CashBook.TotalValueInAccountCurrency +
                           UnsettledCashBook.TotalValueInAccountCurrency +
                           totalHoldingsValueWithoutForexCryptoFutureCfd +
                           totalFuturesAndCfdHoldingsValue;

                        _isTotalPortfolioValueValid = true;
                    }
                }

                return _totalPortfolioValue;
            }
        }

        /// <summary>
        /// Returns the adjusted total portfolio value removing the free amount
        /// If the <see cref="IAlgorithmSettings.FreePortfolioValue"/> has not been set, the free amount will have a trailing behavior and be updated when requested
        /// </summary>
        public decimal TotalPortfolioValueLessFreeBuffer
        {
            get
            {
                if (_algorithmSettings.FreePortfolioValue.HasValue)
                {
                    // the user set it, we will respect the value set
                    _freePortfolioValue = _algorithmSettings.FreePortfolioValue.Value;
                }
                else
                {
                    // keep the free portfolio value up to date every time we use it
                    _freePortfolioValue = TotalPortfolioValue * _algorithmSettings.FreePortfolioValuePercentage;
                }

                return TotalPortfolioValue - _freePortfolioValue;

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
                return Securities.Total.Sum(security => security.Holdings.TotalFees);
            }
        }

        /// <summary>
        /// Sum of all gross profit across all securities in portfolio and dividend payments.
        /// </summary>
        public decimal TotalProfit
        {
            get
            {
                return Securities.Total.Sum(security => security.Holdings.Profit);
            }
        }

        /// <summary>
        /// Sum of all net profit across all securities in portfolio and dividend payments.
        /// </summary>
        public decimal TotalNetProfit
        {
            get
            {
                return Securities.Total.Sum(security => security.Holdings.NetProfit);
            }
        }

        /// <summary>
        /// Total sale volume since the start of algorithm operations.
        /// </summary>
        public decimal TotalSaleVolume
        {
            get
            {
                return Securities.Total.Sum(security => security.Holdings.TotalSaleVolume);
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
                foreach (var group in Positions.Groups)
                {
                    sum += group.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(this, group);
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
            get
            {
                return Securities[symbol].Holdings;
            }
            set
            {
                Securities[symbol].Holdings = value;
            }
        }

        /// <summary>
        /// Sets the account currency cash symbol this algorithm is to manage, as well
        /// as the starting cash in this currency if given
        /// </summary>
        /// <remarks>Has to be called before calling <see cref="SetCash(decimal)"/>
        /// or adding any <see cref="Security"/></remarks>
        /// <param name="accountCurrency">The account currency cash symbol to set</param>
        /// <param name="startingCash">The account currency starting cash to set</param>
        public void SetAccountCurrency(string accountCurrency, decimal? startingCash = null)
        {
            accountCurrency = accountCurrency.LazyToUpper();

            // only allow setting account currency once
            // we could try to set it twice when backtesting and the job packet specifies the initial CashAmount to use
            if (_setAccountCurrencyWasCalled)
            {
                if (accountCurrency != CashBook.AccountCurrency)
                {
                    Log.Trace("SecurityPortfolioManager.SetAccountCurrency(): " +
                        Messages.SecurityPortfolioManager.AccountCurrencyAlreadySet(CashBook, accountCurrency));
                }
                return;
            }
            _setAccountCurrencyWasCalled = true;

            if (Securities.Count > 0)
            {
                throw new InvalidOperationException("SecurityPortfolioManager.SetAccountCurrency(): " +
                    Messages.SecurityPortfolioManager.CannotChangeAccountCurrencyAfterAddingSecurity);
            }

            if (_setCashWasCalled)
            {
                throw new InvalidOperationException("SecurityPortfolioManager.SetAccountCurrency(): " +
                    Messages.SecurityPortfolioManager.CannotChangeAccountCurrencyAfterSettingCash);
            }

            Log.Trace("SecurityPortfolioManager.SetAccountCurrency(): " +
                Messages.SecurityPortfolioManager.SettingAccountCurrency(accountCurrency));

            UnsettledCashBook.AccountCurrency = accountCurrency;
            CashBook.AccountCurrency = accountCurrency;

            _baseCurrencyCash = CashBook[accountCurrency];

            if (startingCash != null)
            {
                SetCash((decimal)startingCash);
            }
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
            symbol = symbol.LazyToUpper();
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

        // TODO: Review and fix these comments: it doesn't return what it says it does.
        /// <summary>
        /// Gets the margin available for trading a specific symbol in a specific direction.
        /// </summary>
        /// <param name="symbol">The symbol to compute margin remaining for</param>
        /// <param name="direction">The order/trading direction</param>
        /// <returns>The maximum order size that is currently executable in the specified direction</returns>
        public decimal GetMarginRemaining(Symbol symbol, OrderDirection direction = OrderDirection.Buy)
        {
            var security = Securities[symbol];

            var positionGroup = Positions.GetOrCreateDefaultGroup(security);
            // Order direction in GetPositionGroupBuyingPower is regarding buying or selling the position group sent as parameter.
            // Since we are passing the same position group as the one in the holdings, we need to invert the direction.
            // Buying the means increasing the position group (in the same direction it is currently held) and selling means decreasing it.
            var positionGroupOrderDirection = direction;
            if (security.Holdings.IsShort)
            {
                positionGroupOrderDirection = direction == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            }

            var parameters = new PositionGroupBuyingPowerParameters(this, positionGroup, positionGroupOrderDirection);
            return positionGroup.BuyingPowerModel.GetPositionGroupBuyingPower(parameters);
        }

        /// <summary>
        /// Gets the margin available for trading a specific symbol in a specific direction.
        /// Alias for <see cref="GetMarginRemaining(Symbol, OrderDirection)"/>
        /// </summary>
        /// <param name="symbol">The symbol to compute margin remaining for</param>
        /// <param name="direction">The order/trading direction</param>
        /// <returns>The maximum order size that is currently executable in the specified direction</returns>
        public decimal GetBuyingPower(Symbol symbol, OrderDirection direction = OrderDirection.Buy)
        {
            return GetMarginRemaining(symbol, direction);
        }

        /// <summary>
        /// Calculate the new average price after processing a list of partial/complete order fill events.
        /// </summary>
        /// <remarks>
        ///     For purchasing stocks from zero holdings, the new average price is the sale price.
        ///     When simply partially reducing holdings the average price remains the same.
        ///     When crossing zero holdings the average price becomes the trade price in the new side of zero.
        /// </remarks>
        public virtual void ProcessFills(List<OrderEvent> fills)
        {
            lock (_totalPortfolioValueLock)
            {
                for (var i = 0; i < fills.Count; i++)
                {
                    var fill = fills[i];
                    var security = Securities[fill.Symbol];
                    security.PortfolioModel.ProcessFill(this, security, fill);
                }

                InvalidateTotalPortfolioValue();
            }

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

            // only apply dividends when we're in raw mode or split adjusted mode
            if (mode == DataNormalizationMode.Raw || mode == DataNormalizationMode.SplitAdjusted)
            {
                var security = Securities[dividend.Symbol];

                // longs get benefits, shorts get clubbed on dividends
                var total = security.Holdings.Quantity * dividend.Distribution * security.QuoteCurrency.ConversionRate;

                // assuming USD, we still need to add Currency to the security object
                _baseCurrencyCash.AddAmount(total);
                security.Holdings.AddNewDividend(total);
            }
        }

        /// <summary>
        /// Applies a split to the portfolio
        /// </summary>
        /// <param name="split">The split to be applied</param>
        /// <param name="security">The security the split will be applied to</param>
        /// <param name="liveMode">True if live mode, false for backtest</param>
        /// <param name="mode">The <see cref="DataNormalizationMode"/> for this security</param>
        public void ApplySplit(Split split, Security security, bool liveMode, DataNormalizationMode mode)
        {
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

            security.Holdings.SetHoldings(avgPrice, (int)quantity);

            // build a 'next' value to update the market prices in light of the split factor
            var next = security.GetLastData();
            if (next == null)
            {
                // sometimes we can get splits before we receive data which
                // will cause this to return null, in this case we can't possibly
                // have any holdings or price to set since we haven't received
                // data yet, so just do nothing
                _baseCurrencyCash.AddAmount(leftOver * split.ReferencePrice * split.SplitFactor);
                return;
            }

            security.ApplySplit(split);
            // The data price should have been adjusted already
            _baseCurrencyCash.AddAmount(leftOver * next.Price);

            // security price updated
            InvalidateTotalPortfolioValue();
        }

        /// <summary>
        /// Record the transaction value and time in a list to later be processed for statistics creation.
        /// </summary>
        /// <param name="time">Time of order processed </param>
        /// <param name="transactionProfitLoss">Profit Loss.</param>
        /// <param name="isWin">
        /// Whether the transaction is a win.
        /// For options exercise, this might not depend only on the profit/loss value
        /// </param>
        public void AddTransactionRecord(DateTime time, decimal transactionProfitLoss, bool isWin)
        {
            Transactions.AddTransactionRecord(time, transactionProfitLoss, isWin);
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
        /// Logs margin information for debugging
        /// </summary>
        public void LogMarginInformation(OrderRequest orderRequest = null)
        {
            Log.Trace(Messages.SecurityPortfolioManager.TotalMarginInformation(TotalMarginUsed, MarginRemaining));

            var orderSubmitRequest = orderRequest as SubmitOrderRequest;
            if (orderSubmitRequest != null)
            {
                var direction = orderSubmitRequest.Quantity > 0 ? OrderDirection.Buy : OrderDirection.Sell;
                var security = Securities[orderSubmitRequest.Symbol];

                var positionGroup = Positions.GetOrCreateDefaultGroup(security);
                var marginUsed = positionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(
                    this, positionGroup
                );

                var marginRemaining = positionGroup.BuyingPowerModel.GetPositionGroupBuyingPower(
                    this, positionGroup, direction
                );

                Log.Trace(Messages.SecurityPortfolioManager.OrderRequestMarginInformation(marginUsed, marginRemaining.Value));
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

        /// <summary>
        /// Will determine if the algorithms portfolio has enough buying power to fill the given orders
        /// </summary>
        /// <param name="orders">The orders to check</param>
        /// <returns>True if the algorithm has enough buying power available</returns>
        public HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(List<Order> orders)
        {
            if (Positions.TryCreatePositionGroup(orders, out var group))
            {
                return group.BuyingPowerModel.HasSufficientBuyingPowerForOrder(new HasSufficientPositionGroupBuyingPowerForOrderParameters(this, group, orders));
            }

            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var security = Securities[order.Symbol];
                var result = security.BuyingPowerModel.HasSufficientBuyingPowerForOrder(this, security, order);
                if (!result.IsSufficient)
                {
                    // if any fails, we fail all
                    return result;
                }
            }
            return new HasSufficientBuyingPowerForOrderResult(true);
        }

        /// <summary>
        /// Will set the security position group model to use
        /// </summary>
        /// <param name="positionGroupModel">The position group model instance</param>
        public void SetPositions(SecurityPositionGroupModel positionGroupModel)
        {
            Positions = positionGroupModel;
        }
    }
}
