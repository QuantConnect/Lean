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
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Securities
{
    /// <summary>
    /// SecurityHolding is a base class for purchasing and holding a market item which manages the asset portfolio
    /// </summary>
    public class SecurityHolding
    {
        /// <summary>
        /// Event raised each time the holdings quantity is changed.
        /// </summary>
        public event EventHandler<SecurityHoldingQuantityChangedEventArgs> QuantityChanged;

        //Working Variables
        private bool _invested;
        private decimal _averagePrice;
        private decimal _quantity;
        private decimal _price;
        private decimal _totalSaleVolume;
        private decimal _profit;
        private decimal _lastTradeProfit;
        private decimal _totalFees;
        private decimal _totalDividends;
        private readonly Security _security;
        private readonly ICurrencyConverter _currencyConverter;

        /// <summary>
        /// Create a new holding class instance setting the initial properties to $0.
        /// </summary>
        /// <param name="security">The security being held</param>
        /// <param name="currencyConverter">A currency converter instance</param>
        public SecurityHolding(Security security, ICurrencyConverter currencyConverter)
        {
            _security = security;
            //Total Sales Volume for the day
            _totalSaleVolume = 0;
            _lastTradeProfit = 0;
            _currencyConverter = currencyConverter;
        }

        /// <summary>
        /// Create a new holding class instance copying the initial properties
        /// </summary>
        /// <param name="holding">The security being held</param>
        protected SecurityHolding(SecurityHolding holding)
        {
            _security = holding._security;
            _averagePrice = holding._averagePrice;
            Quantity = holding._quantity;
            _price = holding._price;
            _totalSaleVolume = holding._totalSaleVolume;
            _profit = holding._profit;
            _lastTradeProfit = holding._lastTradeProfit;
            _totalFees = holding._totalFees;
            _currencyConverter = holding._currencyConverter;
        }

        /// <summary>
        /// The security being held
        /// </summary>
        protected Security Security
        {
            get
            {
                return _security;
            }
        }

        /// <summary>
        /// Gets the current target holdings for this security
        /// </summary>
        public IPortfolioTarget Target
        {
            get; set;
        }

        /// <summary>
        /// Average price of the security holdings.
        /// </summary>
        public decimal AveragePrice
        {
            get
            {
                return _averagePrice;
            }
            protected set
            {
                _averagePrice = value;
            }
        }

        /// <summary>
        /// Quantity of the security held.
        /// </summary>
        /// <remarks>Positive indicates long holdings, negative quantity indicates a short holding</remarks>
        /// <seealso cref="AbsoluteQuantity"/>
        public decimal Quantity
        {
            get
            {
                return _quantity;
            }
            protected set
            {
                _invested = value != 0;
                _quantity = value;
            }
        }

        /// <summary>
        /// Symbol identifier of the underlying security.
        /// </summary>
        public Symbol Symbol
        {
            get
            {
                return _security.Symbol;
            }
        }

        /// <summary>
        /// The security type of the symbol
        /// </summary>
        public SecurityType Type
        {
            get
            {
                return _security.Type;
            }
        }

        /// <summary>
        /// Leverage of the underlying security.
        /// </summary>
        public virtual decimal Leverage
        {
            get
            {
                return _security.BuyingPowerModel.GetLeverage(_security);
            }
        }

        /// <summary>
        /// Acquisition cost of the security total holdings in units of the account's currency.
        /// </summary>
        public virtual decimal HoldingsCost
        {
            get
            {
                if (Quantity == 0)
                {
                    return 0;
                }
                return GetQuantityValue(Quantity, AveragePrice).InAccountCurrency;
            }
        }

        /// <summary>
        /// Unlevered Acquisition cost of the security total holdings in units of the account's currency.
        /// </summary>
        public virtual decimal UnleveredHoldingsCost
        {
            get { return HoldingsCost/Leverage; }
        }

        /// <summary>
        /// Current market price of the security.
        /// </summary>
        public virtual decimal Price
        {
            get
            {
                return _price;
            }
            protected set
            {
                _price = value;
            }
        }

        /// <summary>
        /// Absolute holdings cost for current holdings in units of the account's currency.
        /// </summary>
        /// <seealso cref="HoldingsCost"/>
        public virtual decimal AbsoluteHoldingsCost
        {
            get
            {
                return Math.Abs(HoldingsCost);
            }
        }

        /// <summary>
        /// Unlevered absolute acquisition cost of the security total holdings in units of the account's currency.
        /// </summary>
        public virtual decimal UnleveredAbsoluteHoldingsCost
        {
            get
            {
                return Math.Abs(UnleveredHoldingsCost);
            }
        }

        /// <summary>
        /// Market value of our holdings in units of the account's currency.
        /// </summary>
        public virtual decimal HoldingsValue
        {
            get
            {
                if (Quantity == 0)
                {
                    return 0;
                }

                return GetQuantityValue(Quantity).InAccountCurrency;
            }
        }

        /// <summary>
        /// Absolute of the market value of our holdings in units of the account's currency.
        /// </summary>
        /// <seealso cref="HoldingsValue"/>
        public virtual decimal AbsoluteHoldingsValue
        {
            get { return Math.Abs(HoldingsValue); }
        }

        /// <summary>
        /// Boolean flat indicating if we hold any of the security
        /// </summary>
        public virtual bool HoldStock => _invested;

        /// <summary>
        /// Boolean flat indicating if we hold any of the security
        /// </summary>
        /// <remarks>Alias of HoldStock</remarks>
        /// <seealso cref="HoldStock"/>
        public virtual bool Invested => _invested;

        /// <summary>
        /// The total transaction volume for this security since the algorithm started in units of the account's currency.
        /// </summary>
        public virtual decimal TotalSaleVolume
        {
            get { return _totalSaleVolume; }
        }

        /// <summary>
        /// Total fees for this company since the algorithm started in units of the account's currency.
        /// </summary>
        public virtual decimal TotalFees
        {
            get { return _totalFees; }
        }

        /// <summary>
        /// Total dividends for this company since the algorithm started in units of the account's currency.
        /// </summary>
        public virtual decimal TotalDividends
        {
            get { return _totalDividends; }
        }

        /// <summary>
        /// Boolean flag indicating we have a net positive holding of the security.
        /// </summary>
        /// <seealso cref="IsShort"/>
        public virtual bool IsLong
        {
            get
            {
                return Quantity > 0;
            }
        }

        /// <summary>
        /// BBoolean flag indicating we have a net negative holding of the security.
        /// </summary>
        /// <seealso cref="IsLong"/>
        public virtual bool IsShort
        {
            get
            {
                return Quantity < 0;
            }
        }

        /// <summary>
        /// Absolute quantity of holdings of this security
        /// </summary>
        /// <seealso cref="Quantity"/>
        public virtual decimal AbsoluteQuantity
        {
            get
            {
                return Math.Abs(Quantity);
            }
        }

        /// <summary>
        /// Record of the closing profit from the last trade conducted in units of the account's currency.
        /// </summary>
        public virtual decimal LastTradeProfit
        {
            get
            {
                return _lastTradeProfit;
            }
        }

        /// <summary>
        /// Calculate the total profit for this security in units of the account's currency.
        /// </summary>
        /// <seealso cref="NetProfit"/>
        public virtual decimal Profit
        {
            get { return _profit + _totalDividends; }
        }

        /// <summary>
        /// Return the net for this company measured by the profit less fees in units of the account's currency.
        /// </summary>
        /// <seealso cref="Profit"/>
        /// <seealso cref="TotalFees"/>
        public virtual decimal NetProfit
        {
            get
            {
                return Profit - TotalFees;
            }
        }

        /// <summary>
        /// Gets the unrealized profit as a percentage of holdings cost
        /// </summary>
        public virtual decimal UnrealizedProfitPercent
        {
            get
            {
                if (AbsoluteHoldingsCost == 0) return 0m;
                return UnrealizedProfit/AbsoluteHoldingsCost;
            }
        }

        /// <summary>
        /// Unrealized profit of this security when absolute quantity held is more than zero in units of the account's currency.
        /// </summary>
        public virtual decimal UnrealizedProfit
        {
            get { return TotalCloseProfit(); }
        }

        /// <summary>
        /// Adds a fee to the running total of total fees in units of the account's currency.
        /// </summary>
        /// <param name="newFee"></param>
        public void AddNewFee(decimal newFee)
        {
            _totalFees += newFee;
        }

        /// <summary>
        /// Adds a profit record to the running total of profit in units of the account's currency.
        /// </summary>
        /// <param name="profitLoss">The cash change in portfolio from closing a position</param>
        public void AddNewProfit(decimal profitLoss)
        {
            _profit += profitLoss;
        }

        /// <summary>
        /// Adds a new sale value to the running total trading volume in units of the account's currency.
        /// </summary>
        /// <param name="saleValue"></param>
        public void AddNewSale(decimal saleValue)
        {
            _totalSaleVolume += saleValue;
        }

        /// <summary>
        /// Adds a new dividend payment to the running total dividend in units of the account's currency.
        /// </summary>
        /// <param name="dividend"></param>
        public void AddNewDividend(decimal dividend)
        {
            _totalDividends += dividend;
        }

        /// <summary>
        /// Set the last trade profit for this security from a Portfolio.ProcessFill call in units of the account's currency.
        /// </summary>
        /// <param name="lastTradeProfit">Value of the last trade profit</param>
        public void SetLastTradeProfit(decimal lastTradeProfit)
        {
            _lastTradeProfit = lastTradeProfit;
        }

        /// <summary>
        /// Set the quantity of holdings and their average price after processing a portfolio fill.
        /// </summary>
        public virtual void SetHoldings(decimal averagePrice, int quantity)
        {
            SetHoldings(averagePrice, (decimal) quantity);
        }

        /// <summary>
        /// Set the quantity of holdings and their average price after processing a portfolio fill.
        /// </summary>
        public virtual void SetHoldings(decimal averagePrice, decimal quantity)
        {
            var previousQuantity = _quantity;
            var previousAveragePrice = _averagePrice;

            Quantity = quantity;
            _averagePrice = averagePrice;

            OnQuantityChanged(previousAveragePrice, previousQuantity);
        }

        /// <summary>
        /// Update local copy of closing price value.
        /// </summary>
        /// <param name="closingPrice">Price of the underlying asset to be used for calculating market price / portfolio value</param>
        public virtual void UpdateMarketPrice(decimal closingPrice)
        {
            _price = closingPrice;
        }

        /// <summary>
        /// Gets the total value of the specified <paramref name="quantity"/> of shares of this security
        /// in the account currency
        /// </summary>
        /// <param name="quantity">The quantity of shares</param>
        /// <returns>The value of the quantity of shares in the account currency</returns>
        public virtual ConvertibleCashAmount GetQuantityValue(decimal quantity)
        {
            return GetQuantityValue(quantity, _price);
        }

        /// <summary>
        /// Gets the total value of the specified <paramref name="quantity"/> of shares of this security
        /// in the account currency
        /// </summary>
        /// <param name="quantity">The quantity of shares</param>
        /// <param name="price">The current price</param>
        /// <returns>The value of the quantity of shares in the account currency</returns>
        public virtual ConvertibleCashAmount GetQuantityValue(decimal quantity, decimal price)
        {
            var amount = price * quantity * _security.SymbolProperties.ContractMultiplier;
            return new ConvertibleCashAmount(amount, _security.QuoteCurrency);
        }

        /// <summary>
        /// Profit if we closed the holdings right now including the approximate fees in units of the account's currency.
        /// </summary>
        /// <remarks>Does not use the transaction model for market fills but should.</remarks>
        public virtual decimal TotalCloseProfit(bool includeFees = true, decimal? exitPrice = null, decimal? entryPrice = null, decimal? quantity = null)
        {
            var quantityToUse = quantity ?? Quantity;
            if (quantityToUse == 0)
            {
                return 0;
            }

            // this is in the account currency
            var marketOrder = new MarketOrder(_security.Symbol, -quantityToUse, _security.LocalTime.ConvertToUtc(_security.Exchange.TimeZone));

            var feesInAccountCurrency = 0m;
            if (includeFees)
            {
                var orderFee = _security.FeeModel.GetOrderFee(
                    new OrderFeeParameters(_security, marketOrder)).Value;
                feesInAccountCurrency = _currencyConverter.ConvertToAccountCurrency(orderFee).Amount;
            }

            var price = marketOrder.Direction == OrderDirection.Sell ? _security.BidPrice : _security.AskPrice;
            if (price == 0)
            {
                // Bid/Ask prices can both be equal to 0. This usually happens when we request our holdings from
                // the brokerage, but only the last trade price was provided.
                price = _security.Price;
            }

            var entryValue = GetQuantityValue(quantityToUse, entryPrice ?? AveragePrice).InAccountCurrency;
            var potentialExitValue = GetQuantityValue(quantityToUse, exitPrice ?? price).InAccountCurrency;
            return potentialExitValue - entryValue - feesInAccountCurrency;
        }

        /// <summary>
        /// Writes out the properties of this instance to string
        /// </summary>
        public override string ToString()
        {
            return Messages.SecurityHolding.ToString(this);
        }

        /// <summary>
        /// Event invocator for the <see cref="QuantityChanged"/> event
        /// </summary>
        protected virtual void OnQuantityChanged(decimal previousAveragePrice, decimal previousQuantity)
        {
            QuantityChanged?.Invoke(this, new SecurityHoldingQuantityChangedEventArgs(
                _security, previousAveragePrice, previousQuantity
            ));
        }
    }
}
