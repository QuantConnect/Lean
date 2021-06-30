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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a base class for all buying power models
    /// </summary>
    public class BuyingPowerModel : IBuyingPowerModel
    {
        private decimal _initialMarginRequirement;
        private decimal _maintenanceMarginRequirement;

        /// <summary>
        /// The percentage used to determine the required unused buying power for the account.
        /// </summary>
        protected decimal RequiredFreeBuyingPowerPercent;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuyingPowerModel"/> with no leverage (1x)
        /// </summary>
        public BuyingPowerModel()
            : this(1m)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuyingPowerModel"/>
        /// </summary>
        /// <param name="initialMarginRequirement">The percentage of an order's absolute cost
        /// that must be held in free cash in order to place the order</param>
        /// <param name="maintenanceMarginRequirement">The percentage of the holding's absolute
        /// cost that must be held in free cash in order to avoid a margin call</param>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage used to determine the required
        /// unused buying power for the account.</param>
        public BuyingPowerModel(
            decimal initialMarginRequirement,
            decimal maintenanceMarginRequirement,
            decimal requiredFreeBuyingPowerPercent
            )
        {
            if (initialMarginRequirement < 0 || initialMarginRequirement > 1)
            {
                throw new ArgumentException("Initial margin requirement must be between 0 and 1");
            }

            if (maintenanceMarginRequirement < 0 || maintenanceMarginRequirement > 1)
            {
                throw new ArgumentException("Maintenance margin requirement must be between 0 and 1");
            }

            if (requiredFreeBuyingPowerPercent < 0 || requiredFreeBuyingPowerPercent > 1)
            {
                throw new ArgumentException("Free Buying Power Percent requirement must be between 0 and 1");
            }

            _initialMarginRequirement = initialMarginRequirement;
            _maintenanceMarginRequirement = maintenanceMarginRequirement;
            RequiredFreeBuyingPowerPercent = requiredFreeBuyingPowerPercent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuyingPowerModel"/>
        /// </summary>
        /// <param name="leverage">The leverage</param>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage used to determine the required
        /// unused buying power for the account.</param>
        public BuyingPowerModel(decimal leverage, decimal requiredFreeBuyingPowerPercent = 0)
        {
            if (leverage < 1)
            {
                throw new ArgumentException("Leverage must be greater than or equal to 1.");
            }

            if (requiredFreeBuyingPowerPercent < 0 || requiredFreeBuyingPowerPercent > 1)
            {
                throw new ArgumentException("Free Buying Power Percent requirement must be between 0 and 1");
            }

            _initialMarginRequirement = 1 / leverage;
            _maintenanceMarginRequirement = 1 / leverage;
            RequiredFreeBuyingPowerPercent = requiredFreeBuyingPowerPercent;
        }

        /// <summary>
        /// Gets the current leverage of the security
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        public virtual decimal GetLeverage(Security security)
        {
            return 1 / _initialMarginRequirement;
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, equities
        /// </summary>
        /// <remarks>
        /// This is added to maintain backwards compatibility with the old margin/leverage system
        /// </remarks>
        /// <param name="security"></param>
        /// <param name="leverage">The new leverage</param>
        public virtual void SetLeverage(Security security, decimal leverage)
        {
            if (leverage < 1)
            {
                throw new ArgumentException("Leverage must be greater than or equal to 1.");
            }

            var margin = 1 / leverage;
            _initialMarginRequirement = margin;
            _maintenanceMarginRequirement = margin;
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public virtual InitialMargin GetInitialMarginRequiredForOrder(
            InitialMarginRequiredForOrderParameters parameters
            )
        {
            //Get the order value from the non-abstract order classes (MarketOrder, LimitOrder, StopMarketOrder)
            //Market order is approximated from the current security price and set in the MarketOrder Method in QCAlgorithm.

            var fees = parameters.Security.FeeModel.GetOrderFee(
                new OrderFeeParameters(parameters.Security,
                    parameters.Order)).Value;
            var feesInAccountCurrency = parameters.CurrencyConverter.
                ConvertToAccountCurrency(fees).Amount;

            var orderMargin = this.GetInitialMarginRequirement(parameters.Security, parameters.Order.Quantity);

            return orderMargin + Math.Sign(orderMargin) * feesInAccountCurrency;
        }

        /// <summary>
        /// Gets the margin currently allocated to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security and holdings quantity/cost/value</param>
        /// <returns>The maintenance margin required for the provided holdings quantity/cost/value</returns>
        public virtual MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
        {
            return parameters.AbsoluteHoldingsValue * _maintenanceMarginRequirement;
        }

        /// <summary>
        /// Gets the margin cash available for a trade
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="direction">The direction of the trade</param>
        /// <returns>The margin available for the trade</returns>
        protected virtual decimal GetMarginRemaining(
            SecurityPortfolioManager portfolio,
            Security security,
            OrderDirection direction
            )
        {
            var totalPortfolioValue = portfolio.TotalPortfolioValue;
            var result = portfolio.GetMarginRemaining(totalPortfolioValue);

            if (direction != OrderDirection.Hold)
            {
                var holdings = security.Holdings;
                //If the order is in the same direction as holdings, our remaining cash is our cash
                //In the opposite direction, our remaining cash is 2 x current value of assets + our cash
                if (holdings.IsLong)
                {
                    switch (direction)
                    {
                        case OrderDirection.Sell:
                            result +=
                                // portion of margin to close the existing position
                                this.GetMaintenanceMargin(security) +
                                // portion of margin to open the new position
                                this.GetInitialMarginRequirement(security, security.Holdings.AbsoluteQuantity);
                            break;
                    }
                }
                else if (holdings.IsShort)
                {
                    switch (direction)
                    {
                        case OrderDirection.Buy:
                            result +=
                                // portion of margin to close the existing position
                                this.GetMaintenanceMargin(security) +
                                // portion of margin to open the new position
                                this.GetInitialMarginRequirement(security, security.Holdings.AbsoluteQuantity);
                            break;
                    }
                }
            }

            result -= totalPortfolioValue * RequiredFreeBuyingPowerPercent;
            return result < 0 ? 0 : result;
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity of shares</param>
        /// <returns>The initial margin required for the provided security and quantity</returns>
        public virtual InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            var security = parameters.Security;
            var quantity = parameters.Quantity;
            return security.QuoteCurrency.ConversionRate
                * security.SymbolProperties.ContractMultiplier
                * security.Price
                * quantity
                * _initialMarginRequirement;
        }

        /// <summary>
        /// Check if there is sufficient buying power to execute this order.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>Returns buying power information for an order</returns>
        public virtual HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(HasSufficientBuyingPowerForOrderParameters parameters)
        {
            // short circuit the div 0 case
            if (parameters.Order.Quantity == 0)
            {
                return parameters.Sufficient();
            }

            var ticket = parameters.Portfolio.Transactions.GetOrderTicket(parameters.Order.Id);
            if (ticket == null)
            {
                return parameters.Insufficient(
                    $"Null order ticket for id: {parameters.Order.Id}"
                );
            }

            if (parameters.Order.Type == OrderType.OptionExercise)
            {
                // for option assignment and exercise orders we look into the requirements to process the underlying security transaction
                var option = (Option.Option) parameters.Security;
                var underlying = option.Underlying;

                if (option.IsAutoExercised(underlying.Close) && underlying.IsTradable)
                {
                    var quantity = option.GetExerciseQuantity(parameters.Order.Quantity);

                    var newOrder = new LimitOrder
                    {
                        Id = parameters.Order.Id,
                        Time = parameters.Order.Time,
                        LimitPrice = option.StrikePrice,
                        Symbol = underlying.Symbol,
                        Quantity = quantity
                    };

                    // we continue with this call for underlying
                    var parametersForUnderlying = parameters.ForUnderlying(newOrder);

                    var freeMargin = underlying.BuyingPowerModel.GetBuyingPower(parametersForUnderlying.Portfolio, parametersForUnderlying.Security, parametersForUnderlying.Order.Direction);
                    // we add the margin used by the option itself
                    freeMargin += GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(option, -parameters.Order.Quantity));

                    var initialMarginRequired = underlying.BuyingPowerModel.GetInitialMarginRequiredForOrder(
                        new InitialMarginRequiredForOrderParameters(parameters.Portfolio.CashBook, underlying, newOrder));

                    return HasSufficientBuyingPowerForOrder(parametersForUnderlying, ticket, freeMargin, initialMarginRequired);
                }

                return parameters.Sufficient();
            }

            return HasSufficientBuyingPowerForOrder(parameters, ticket);
        }

        private HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(HasSufficientBuyingPowerForOrderParameters parameters, OrderTicket ticket,
            decimal? freeMarginToUse = null, decimal? initialMarginRequired = null)
        {
            // When order only reduces or closes a security position, capital is always sufficient
            if (parameters.Security.Holdings.Quantity * parameters.Order.Quantity < 0 && Math.Abs(parameters.Security.Holdings.Quantity) >= Math.Abs(parameters.Order.Quantity))
            {
                return parameters.Sufficient();
            }

            var freeMargin = freeMarginToUse ?? GetMarginRemaining(parameters.Portfolio, parameters.Security, parameters.Order.Direction);
            var initialMarginRequiredForOrder = initialMarginRequired ?? GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(
                    parameters.Portfolio.CashBook, parameters.Security, parameters.Order
            ));

            // pro-rate the initial margin required for order based on how much has already been filled
            var percentUnfilled = (Math.Abs(parameters.Order.Quantity) - Math.Abs(ticket.QuantityFilled)) / Math.Abs(parameters.Order.Quantity);
            var initialMarginRequiredForRemainderOfOrder = percentUnfilled * initialMarginRequiredForOrder;

            if (Math.Abs(initialMarginRequiredForRemainderOfOrder) > freeMargin)
            {
                return parameters.Insufficient(Invariant($"Id: {parameters.Order.Id}, ") +
                    Invariant($"Initial Margin: {initialMarginRequiredForRemainderOfOrder.Normalize()}, ") +
                    Invariant($"Free Margin: {freeMargin.Normalize()}")
                );
            }

            return parameters.Sufficient();
        }

        /// <summary>
        /// Get the maximum market order quantity to obtain a delta in the buying power used by a security.
        /// The deltas sign defines the position side to apply it to, positive long, negative short.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the delta buying power</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
        public virtual GetMaximumOrderQuantityResult GetMaximumOrderQuantityForDeltaBuyingPower(
            GetMaximumOrderQuantityForDeltaBuyingPowerParameters parameters)
        {
            var usedBuyingPower = parameters.Security.BuyingPowerModel.GetReservedBuyingPowerForPosition(
                new ReservedBuyingPowerForPositionParameters(parameters.Security)).AbsoluteUsedBuyingPower;

            var signedUsedBuyingPower = usedBuyingPower * (parameters.Security.Holdings.IsLong ? 1 : -1);

            var targetBuyingPower = signedUsedBuyingPower + parameters.DeltaBuyingPower;

            var target = 0m;
            if (parameters.Portfolio.TotalPortfolioValue != 0)
            {
                target = targetBuyingPower / parameters.Portfolio.TotalPortfolioValue;
            }

            return GetMaximumOrderQuantityForTargetBuyingPower(
                new GetMaximumOrderQuantityForTargetBuyingPowerParameters(parameters.Portfolio,
                    parameters.Security,
                    target,
                    parameters.SilenceNonErrorReasons));
        }

        /// <summary>
        /// Get the maximum market order quantity to obtain a position with a given buying power percentage.
        /// Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the target signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        public virtual GetMaximumOrderQuantityResult GetMaximumOrderQuantityForTargetBuyingPower(GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters)
        {
            // Determine the margin required for 1 unit; if zero then we don't have a price yet.
            var unitMargin = this.GetInitialMarginRequirement(parameters.Security, 1);
            if (unitMargin == 0)
            {
                return new GetMaximumOrderQuantityResult(0, parameters.Security.Symbol.GetZeroPriceMessage());
            }

            // Determine how much of our portfolio is useable for this target; TPV * (1 - BufferPercent)
            var totalUseablePortfolioValue = parameters.Portfolio.TotalPortfolioValue * (1 - RequiredFreeBuyingPowerPercent);

            // Our max target margin allocated to this security; if targeting zero, simply return the negative of the quantity
            var targetMarginAllocated = parameters.TargetBuyingPower * totalUseablePortfolioValue;
            if (targetMarginAllocated == 0)
            {
                return new GetMaximumOrderQuantityResult(-parameters.Security.Holdings.Quantity, string.Empty,
                    false);
            }

            // First pass, calculate:
            // - Final holdings quantity
            // - Order quantity to get to our final holdings
            // - Total target holdings margin requirement
            var finalHoldingsQuantity = (targetMarginAllocated / unitMargin)
                .DiscretelyRoundBy(parameters.Security.SymbolProperties.LotSize);
            var orderQuantity = finalHoldingsQuantity - parameters.Security.Holdings.Quantity;
            var targetHoldingsMargin = finalHoldingsQuantity * unitMargin;

            // Check order quantity before moving on
            if (orderQuantity == 0)
            {
                string reason = null;
                if (!parameters.SilenceNonErrorReasons)
                {
                    if (finalHoldingsQuantity != 0 && finalHoldingsQuantity == parameters.Security.Holdings.Quantity)
                    {
                        reason =
                            $"Already at target holding {finalHoldingsQuantity} for {parameters.Security.Symbol.Value}" +
                            $" Current holdings: {parameters.Security.Holdings.Quantity}";
                    }
                    else
                    {
                        reason = $"The order quantity is less than the minimum lot size of {parameters.Security.SymbolProperties.LotSize}";
                    }
                }

                return new GetMaximumOrderQuantityResult(0, reason, false);
            }

            // This loop will factor in order fees and adjust our quantities accordingly
            var lastOrderQuantity = 0m; // For safety check
            var utcTime = parameters.Security.LocalTime.ConvertToUtc(parameters.Security.Exchange.TimeZone); // For orders -> fee
            do
            {
                // Our target holdings value is over our target allocation, adjust the order size and final quantity
                if(Math.Abs(targetHoldingsMargin) > Math.Abs(targetMarginAllocated))
                {
                    // Use absolutes for this, we will apply sign before adjusting our quantities
                    var sign = Math.Sign(targetMarginAllocated);

                    var amountToAdjustOrder = Math.Abs((targetHoldingsMargin - targetMarginAllocated) / unitMargin);
                    if (amountToAdjustOrder < parameters.Security.SymbolProperties.LotSize)
                    {
                        // We will always adjust by at least 1 LotSize
                        amountToAdjustOrder = parameters.Security.SymbolProperties.LotSize;
                    }

                    // Round our order adjustment and reapply our sign
                    amountToAdjustOrder = sign * amountToAdjustOrder.DiscretelyRoundBy(parameters.Security.SymbolProperties.LotSize);

                    // Update our order size and final holdings quantity
                    orderQuantity -= amountToAdjustOrder;
                    finalHoldingsQuantity -= amountToAdjustOrder;

                    if (orderQuantity == 0)
                    {
                        string reason = parameters.SilenceNonErrorReasons
                            ? null
                            : $"Order reduced to 0 to keep holdings margin below {targetHoldingsMargin}." +
                            $" Current holdings: {parameters.Security.Holdings.Quantity}; Per unit margin: {unitMargin};" +
                            $" Total current margin: {parameters.Security.Holdings.Quantity * unitMargin}";
                        return new GetMaximumOrderQuantityResult(0, parameters.SilenceNonErrorReasons ? null : reason, false);
                    }
                }

                // Generate our order to determine fees; ensure those fees are not negative
                var order = new MarketOrder(parameters.Security.Symbol, orderQuantity, utcTime);
                var fees = parameters.Security.FeeModel.GetOrderFee(
                    new OrderFeeParameters(parameters.Security, order)).Value;
                var orderFee = parameters.Portfolio.CashBook.ConvertToAccountCurrency(fees).Amount;

                // Update our target holdings margin & target margin allocated values
                targetHoldingsMargin = (finalHoldingsQuantity * unitMargin);
                targetMarginAllocated = parameters.TargetBuyingPower * (totalUseablePortfolioValue - orderFee);

                // Safety check, stops infinite loop that doesn't converge, should not occur but just in case.
                if (lastOrderQuantity == orderQuantity)
                {
                    var orderMargin = orderQuantity * unitMargin;
                    var message = "GetMaximumOrderQuantityForTargetBuyingPower(): failed to converge to target order margin " +
                        Invariant($"{targetHoldingsMargin}. Current order margin is {orderMargin}; Order quantity {orderQuantity}; ") +
                        Invariant($"Lot size {parameters.Security.SymbolProperties.LotSize}; Order fee {orderFee}; Security symbol ") +
                        $"{parameters.Security.Symbol}; Margin per unit {unitMargin}.";
                    throw new ArgumentException(message);
                }
                lastOrderQuantity = orderQuantity;
            }
            while (Math.Abs(targetHoldingsMargin) > Math.Abs(targetMarginAllocated));

            // Return our determined order quantity
            return new GetMaximumOrderQuantityResult(orderQuantity);
        }

        /// <summary>
        /// Gets the amount of buying power reserved to maintain the specified position
        /// </summary>
        /// <param name="parameters">A parameters object containing the security</param>
        /// <returns>The reserved buying power in account currency</returns>
        public virtual ReservedBuyingPowerForPosition GetReservedBuyingPowerForPosition(ReservedBuyingPowerForPositionParameters parameters)
        {
            var maintenanceMargin = this.GetMaintenanceMargin(parameters.Security);
            return parameters.ResultInAccountCurrency(maintenanceMargin);
        }

        /// <summary>
        /// Gets the buying power available for a trade
        /// </summary>
        /// <param name="parameters">A parameters object containing the algorithm's portfolio, security, and order direction</param>
        /// <returns>The buying power available for the trade</returns>
        public virtual BuyingPower GetBuyingPower(BuyingPowerParameters parameters)
        {
            var marginRemaining = GetMarginRemaining(parameters.Portfolio, parameters.Security, parameters.Direction);
            return parameters.ResultInAccountCurrency(marginRemaining);
        }
    }
}
