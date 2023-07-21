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
using System.Diagnostics.CodeAnalysis;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides a base class for all buying power models
    /// </summary>
    public class BuyingPowerModel : IBuyingPowerModel
    {
        /// <summary>
        /// Gets an implementation of <see cref="IBuyingPowerModel"/> that
        /// does not check for sufficient buying power
        /// </summary>
        public static readonly IBuyingPowerModel Null = new NullBuyingPowerModel();

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
                throw new ArgumentException(Messages.BuyingPowerModel.InvalidInitialMarginRequirement);
            }

            if (maintenanceMarginRequirement < 0 || maintenanceMarginRequirement > 1)
            {
                throw new ArgumentException(Messages.BuyingPowerModel.InvalidMaintenanceMarginRequirement);
            }

            if (requiredFreeBuyingPowerPercent < 0 || requiredFreeBuyingPowerPercent > 1)
            {
                throw new ArgumentException(Messages.BuyingPowerModel.InvalidFreeBuyingPowerPercentRequirement);
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
                throw new ArgumentException(Messages.BuyingPowerModel.InvalidLeverage);
            }

            if (requiredFreeBuyingPowerPercent < 0 || requiredFreeBuyingPowerPercent > 1)
            {
                throw new ArgumentException(Messages.BuyingPowerModel.InvalidFreeBuyingPowerPercentRequirement);
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
                throw new ArgumentException(Messages.BuyingPowerModel.InvalidLeverage);
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
                return parameters.Insufficient(Messages.BuyingPowerModel.InsufficientBuyingPowerDueToNullOrderTicket(parameters.Order));
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
                return parameters.Insufficient(Messages.BuyingPowerModel.InsufficientBuyingPowerDueToUnsufficientMargin(parameters.Order,
                    initialMarginRequiredForRemainderOfOrder, freeMargin));
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
                    parameters.MinimumOrderMarginPortfolioPercentage,
                    parameters.SilenceNonErrorReasons));
        }

        /// <summary>
        /// Get the maximum market order quantity to obtain a position with a given buying power percentage.
        /// Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the target signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>This implementation ensures that our resulting holdings is less than the target, but it does not necessarily
        /// maximize the holdings to meet the target. To do that we need a minimizing algorithm that reduces the difference between
        /// the target final margin value and the target holdings margin.</remarks>
        public virtual GetMaximumOrderQuantityResult GetMaximumOrderQuantityForTargetBuyingPower(GetMaximumOrderQuantityForTargetBuyingPowerParameters parameters)
        {
            // this is expensive so lets fetch it once
            var totalPortfolioValue = parameters.Portfolio.TotalPortfolioValue;

            // adjust target buying power to comply with required Free Buying Power Percent
            var signedTargetFinalMarginValue =
                parameters.TargetBuyingPower * (totalPortfolioValue - totalPortfolioValue * RequiredFreeBuyingPowerPercent);

            // if targeting zero, simply return the negative of the quantity
            if (signedTargetFinalMarginValue == 0)
            {
                return new GetMaximumOrderQuantityResult(-parameters.Security.Holdings.Quantity, string.Empty, false);
            }

            // we use initial margin requirement here to avoid the duplicate PortfolioTarget.Percent situation:
            // PortfolioTarget.Percent(1) -> fills -> PortfolioTarget.Percent(1) _could_ detect free buying power if we use Maintenance requirement here
            var signedCurrentUsedMargin = this.GetInitialMarginRequirement(parameters.Security, parameters.Security.Holdings.Quantity);

            // determine the unit price in terms of the account currency
            var utcTime = parameters.Security.LocalTime.ConvertToUtc(parameters.Security.Exchange.TimeZone);

            // determine the margin required for 1 unit
            var absUnitMargin = this.GetInitialMarginRequirement(parameters.Security, 1);
            if (absUnitMargin == 0)
            {
                return new GetMaximumOrderQuantityResult(0, parameters.Security.Symbol.GetZeroPriceMessage());
            }

            // Check that the change of margin is above our models minimum percentage change
            var absDifferenceOfMargin = Math.Abs(signedTargetFinalMarginValue - signedCurrentUsedMargin);
            if (!BuyingPowerModelExtensions.AboveMinimumOrderMarginPortfolioPercentage(parameters.Portfolio,
                parameters.MinimumOrderMarginPortfolioPercentage, absDifferenceOfMargin))
            {
                string reason = null;
                if (!parameters.SilenceNonErrorReasons)
                {
                    var minimumValue = totalPortfolioValue * parameters.MinimumOrderMarginPortfolioPercentage;
                    reason = Messages.BuyingPowerModel.TargetOrderMarginNotAboveMinimum(absDifferenceOfMargin, minimumValue);
                }

                if (!PortfolioTarget.MinimumOrderMarginPercentageWarningSent.HasValue)
                {
                    // will trigger the warning if it has not already been sent
                    PortfolioTarget.MinimumOrderMarginPercentageWarningSent = false;
                }
                return new GetMaximumOrderQuantityResult(0, reason, false);
            }

            // Use the following loop to converge on a value that places us under our target allocation when adjusted for fees
            var lastOrderQuantity = 0m;     // For safety check
            decimal orderFees = 0m;
            decimal signedTargetHoldingsMargin;
            decimal orderQuantity;

            do
            {
                // Calculate our order quantity
                orderQuantity = GetAmountToOrder(parameters.Security, signedTargetFinalMarginValue, absUnitMargin, out signedTargetHoldingsMargin);
                if (orderQuantity == 0)
                {
                    string reason = null;
                    if (!parameters.SilenceNonErrorReasons)
                    {
                        reason = Messages.BuyingPowerModel.OrderQuantityLessThanLotSize(parameters.Security,
                            signedTargetFinalMarginValue - signedCurrentUsedMargin);
                    }

                    return new GetMaximumOrderQuantityResult(0, reason, false);
                }

                // generate the order
                var order = new MarketOrder(parameters.Security.Symbol, orderQuantity, utcTime);
                var fees = parameters.Security.FeeModel.GetOrderFee(
                    new OrderFeeParameters(parameters.Security,
                        order)).Value;
                orderFees = parameters.Portfolio.CashBook.ConvertToAccountCurrency(fees).Amount;

                // Update our target portfolio margin allocated when considering fees, then calculate the new FinalOrderMargin
                signedTargetFinalMarginValue = (totalPortfolioValue - orderFees - totalPortfolioValue * RequiredFreeBuyingPowerPercent) * parameters.TargetBuyingPower;

                // Start safe check after first loop, stops endless recursion
                if (lastOrderQuantity == orderQuantity)
                {
                    var message = Messages.BuyingPowerModel.FailedToConvergeOnTheTargetMargin(parameters, signedTargetFinalMarginValue, orderFees);

                    // Need to add underlying value to message to reproduce with options
                    if (parameters.Security is Option.Option option && option.Underlying != null)
                    {
                        var underlying = option.Underlying;
                        message += " " + Messages.BuyingPowerModel.FailedToConvergeOnTheTargetMarginUnderlyingSecurityInfo(underlying);
                    }

                    throw new ArgumentException(message);
                }
                lastOrderQuantity = orderQuantity;

            }
            // Ensure that our target holdings margin will be less than or equal to our target allocated margin
            while (Math.Abs(signedTargetHoldingsMargin) > Math.Abs(signedTargetFinalMarginValue));

            // add directionality back in
            return new GetMaximumOrderQuantityResult(orderQuantity);
        }

        /// <summary>
        /// Helper function that determines the amount to order to get to a given target safely.
        /// Meaning it will either be at or just below target always.
        /// </summary>
        /// <param name="security">Security we are to determine order size for</param>
        /// <param name="targetMargin">Target margin allocated</param>
        /// <param name="marginForOneUnit">Margin requirement for one unit; used in our initial order guess</param>
        /// <param name="finalMargin">Output the final margin allocated to this security</param>
        /// <returns>The size of the order to get safely to our target</returns>
        public decimal GetAmountToOrder([NotNull]Security security, decimal targetMargin, decimal marginForOneUnit, out decimal finalMargin)
        {
            var lotSize = security.SymbolProperties.LotSize;

            // Start with order size that puts us back to 0, in theory this means current margin is 0
            // so we can calculate holdings to get to the new target margin directly. This is very helpful for
            // odd cases where margin requirements aren't linear.
            var orderSize = -security.Holdings.Quantity;

            // Use the margin for one unit to make our initial guess.
            orderSize += targetMargin / marginForOneUnit;

            // Determine the rounding mode for this order size
            var roundingMode = targetMargin < 0
                // Ending in short position; orders need to be rounded towards positive so we end up under our target
                ? MidpointRounding.ToPositiveInfinity
                // Ending in long position; orders need to be rounded towards negative so we end up under our target
                : MidpointRounding.ToNegativeInfinity;

            // Round this order size appropriately
            orderSize = orderSize.DiscretelyRoundBy(lotSize, roundingMode);

            // Use our model to calculate this final margin as a final check
            finalMargin = this.GetInitialMarginRequirement(security,
                    orderSize + security.Holdings.Quantity);

            // Until our absolute final margin is equal to or below target we need to adjust; ensures we don't overshoot target
            // This isn't usually the case, but for non-linear margin per unit cases this may be necessary.
            // For example https://www.quantconnect.com/forum/discussion/12470, (covered in OptionMarginBuyingPowerModelTests)
            var marginDifference = finalMargin - targetMargin;
            while ((targetMargin < 0 && marginDifference < 0) || (targetMargin > 0 && marginDifference > 0))
            {
                // TODO: Can this be smarter about its adjustment, instead of just stepping by lotsize?
                // We adjust according to the target margin being a short or long
                orderSize += targetMargin < 0 ? lotSize : -lotSize;

                // Recalculate final margin with this adjusted orderSize
                finalMargin = this.GetInitialMarginRequirement(security,
                    orderSize + security.Holdings.Quantity);

                // Safety check, does not occur in any of our testing, but to be sure we don't enter a endless loop
                // have this guy check that the difference between the two is not growing.
                var newDifference = finalMargin - targetMargin;
                if (Math.Abs(newDifference) > Math.Abs(marginDifference) && Math.Sign(newDifference) == Math.Sign(marginDifference))
                {
                    // We have a problem and are correcting in the wrong direction
                    var errorMessage = "BuyingPowerModel().GetAmountToOrder(): " +
                        Messages.BuyingPowerModel.MarginBeingAdjustedInTheWrongDirection(targetMargin, marginForOneUnit, security);

                    // Need to add underlying value to message to reproduce with options
                    if (security is Option.Option option && option.Underlying != null)
                    {
                        errorMessage += " " + Messages.BuyingPowerModel.MarginBeingAdjustedInTheWrongDirectionUnderlyingSecurityInfo(option.Underlying);
                    }

                    throw new ArgumentException(errorMessage);
                }

                marginDifference = newDifference;
            }

            return orderSize;
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
