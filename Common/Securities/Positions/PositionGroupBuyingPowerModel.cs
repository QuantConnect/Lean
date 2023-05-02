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
using System.Linq;

using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Util;

using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides a base class for implementations of <see cref="IPositionGroupBuyingPowerModel"/>
    /// </summary>
    public abstract class PositionGroupBuyingPowerModel : IPositionGroupBuyingPowerModel
    {
        /// <summary>
        /// Gets the percentage of portfolio buying power to leave as a buffer
        /// </summary>
        protected decimal RequiredFreeBuyingPowerPercent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionGroupBuyingPowerModel"/> class
        /// </summary>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage of portfolio buying power to leave as a buffer</param>
        protected PositionGroupBuyingPowerModel(decimal requiredFreeBuyingPowerPercent = 0m)
        {
            RequiredFreeBuyingPowerPercent = requiredFreeBuyingPowerPercent;
        }

        /// <summary>
        /// Gets the margin currently allocated to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the </returns>
        public abstract MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters);

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity</param>
        public abstract InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters);

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public abstract InitialMargin GetInitialMarginRequiredForOrder(PositionGroupInitialMarginForOrderParameters parameters);

        /// <summary>
        /// Computes the impact on the portfolio's buying power from adding the position group to the portfolio. This is
        /// a 'what if' analysis to determine what the state of the portfolio would be if these changes were applied. The
        /// delta (before - after) is the margin requirement for adding the positions and if the margin used after the changes
        /// are applied is less than the total portfolio value, this indicates sufficient capital.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio and a position group containing the contemplated
        /// changes to the portfolio</param>
        /// <returns>Returns the portfolio's total portfolio value and margin used before and after the position changes are applied</returns>
        public virtual ReservedBuyingPowerImpact GetReservedBuyingPowerImpact(ReservedBuyingPowerImpactParameters parameters)
        {
            // This process aims to avoid having to compute buying power on the entire portfolio and instead determines
            // the set of groups that can be impacted by the changes being contemplated. The only real way to determine
            // the change in maintenance margin is to determine what groups we'll have after the changes and compute the
            // margin based on that.
            //   1. Determine impacted groups (depends on IPositionGroupResolver.GetImpactedGroups)
            //   2. Compute the currently reserved buying power of impacted groups
            //   3. Create position collection using impacted groups and apply contemplated changes
            //   4. Resolve new position groups using position collection with applied contemplated changes
            //   5. Compute the contemplated reserved buying power on these newly resolved groups

            // 1. Determine impacted groups
            var positionManager = parameters.Portfolio.Positions;

            // 2. Compute current reserved buying power
            var current = 0m;
            var impactedGroups = new List<IPositionGroup>();

            // 3. Determine set of impacted positions to be grouped
            var positions = parameters.Orders.Select(o => o.CreatePositions(parameters.Portfolio.Securities)).SelectMany(p => p).ToList();

            var impactedPositions = positions.ToDictionary(p => p.Symbol);

            foreach (var impactedGroup in positionManager.GetImpactedGroups(positions))
            {
                impactedGroups.Add(impactedGroup);
                current += impactedGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(
                    parameters.Portfolio, impactedGroup
                );

                foreach (var position in impactedGroup)
                {
                    IPosition existing;
                    if (impactedPositions.TryGetValue(position.Symbol, out existing))
                    {
                        // if it already exists then combine it with the existing
                        impactedPositions[position.Symbol] = existing.Combine(position);
                    }
                    else
                    {
                        impactedPositions[position.Symbol] = position;
                    }
                }
            }

            // 4. Resolve new position groups
            var contemplatedGroups = positionManager.ResolvePositionGroups(new PositionCollection(impactedPositions.Values));

            // 5. Compute contemplated reserved buying power
            var contemplated = 0m;
            foreach (var contemplatedGroup in contemplatedGroups)
            {
                contemplated += contemplatedGroup.BuyingPowerModel.GetMaintenanceMargin(
                    parameters.Portfolio, contemplatedGroup
                );
            }

            return new ReservedBuyingPowerImpact(
                current, contemplated, impactedGroups, parameters.ContemplatedChanges, contemplatedGroups
            );
        }

        /// <summary>
        /// Check if there is sufficient buying power for the position group to execute this order.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the order</param>
        /// <returns>Returns buying power information for an order against a position group</returns>
        public virtual HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
            HasSufficientPositionGroupBuyingPowerForOrderParameters parameters
            )
        {
            // The addition of position groups requires that we not only check initial margin requirements, but also
            // that we confirm that after the changes have been applied and the new groups resolved our maintenance
            // margin is still in a valid range (less than TPV). For this model, we use the security's sufficient buying
            // power impl to confirm initial margin requirements and lean heavily on GetReservedBuyingPowerImpact for
            // help with confirming that our expected maintenance margin is still less than TPV.
            //   1. Confirm we have sufficient buying power to execute the trade using security's BP model
            //   2. Confirm we pass position group specific checks
            //   3. Confirm we haven't exceeded maintenance margin limits via GetReservedBuyingPowerImpact's delta

            // 1. Confirm we meet initial margin requirements, accounting for buffer
            var direction = parameters.Orders.Select(o =>
            {
                if (o.GroupOrderManager != null)
                {
                    return o.GroupOrderManager.Direction;
                }
                return o.Direction;
            }).First();

            var deltaBuyingPowerArgs = new ReservedBuyingPowerImpactParameters(parameters.Portfolio, parameters.PositionGroup, parameters.Orders);
            var deltaBuyingPower = GetReservedBuyingPowerImpact(deltaBuyingPowerArgs).Delta;

            // When order only reduces or closes a security position, capital is always sufficient
            if (deltaBuyingPower < 0)
            {
                return parameters.Sufficient();
            }

            var availableBuyingPower = this.GetPositionGroupBuyingPower(parameters.Portfolio, parameters.PositionGroup, direction);

            // 2. Confirm we pass position group specific checks
            var result = PassesPositionGroupSpecificBuyingPowerForOrderChecks(parameters, availableBuyingPower);
            if (result?.IsSufficient == false)
            {
                return result;
            }

            // 3. Confirm that the new groupings arising from the change doesn't make maintenance margin exceed TPV
            if (deltaBuyingPower <= availableBuyingPower)
            {
                return parameters.Sufficient();
            }

            return parameters.Insufficient(Invariant(
                $"Id: {string.Join(",", parameters.Orders.Select(o => o.Id))}, Maintenance Margin Delta: {deltaBuyingPower.Normalize()}, Free Margin: {availableBuyingPower.Value.Normalize()}"
            ));
        }

        /// <summary>
        /// Provides a mechanism for derived types to add their own buying power for order checks without needing to
        /// recompute the available buying power. Implementations should return null if all checks pass and should
        /// return an instance of <see cref="HasSufficientBuyingPowerForOrderResult"/> with IsSufficient=false if it
        /// fails.
        /// </summary>
        protected virtual HasSufficientBuyingPowerForOrderResult PassesPositionGroupSpecificBuyingPowerForOrderChecks(
            HasSufficientPositionGroupBuyingPowerForOrderParameters parameters,
            decimal availableBuyingPower
            )
        {
            return null;
        }

        /// <summary>
        /// Computes the amount of buying power reserved by the provided position group
        /// </summary>
        public virtual ReservedBuyingPowerForPositionGroup GetReservedBuyingPowerForPositionGroup(
            ReservedBuyingPowerForPositionGroupParameters parameters
            )
        {
            return this.GetMaintenanceMargin(parameters.Portfolio, parameters.PositionGroup);
        }

        /// <summary>
        /// Get the maximum position group order quantity to obtain a position with a given buying power
        /// percentage. Will not take into account free buying power.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the target
        ///     signed buying power percentage</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        public virtual GetMaximumLotsResult GetMaximumLotsForTargetBuyingPower(
            GetMaximumLotsForTargetBuyingPowerParameters parameters
            )
        {
            // In order to determine maximum order quantity for a particular amount of buying power, we must resolve
            // the group's 'unit' as this will be the quantity step size. If we don't step according to these units
            // then we could be left with a different type of group with vastly different margin requirements, so we
            // must keep the ratios between all of the position quantities the same. First we'll determine the target
            // buying power, taking into account RequiredFreeBuyingPowerPercent to ensure a buffer. Then we'll evaluate
            // the initial margin requirement using the provided position group position quantities. From this value,
            // we can determine if we need to add more quantity or remove quantity by looking at the delta from the target
            // to the computed initial margin requirement. We can also compute, assuming linearity, the change in initial
            // margin requirements for each 'unit' of the position group added. The final value we need before starting to
            // iterate to solve for quantity is the minimum quantities. This is the 'unit' of the position group, and any
            // quantities less than the unit's quantity would yield an entirely different group w/ different margin calcs.
            // Now that we've resolved our target, our group unit and the unit's initial margin requirement, we can iterate
            // increasing/decreasing quantities in multiples of the unit's quantities until we're within a unit's amount of
            // initial margin to the target buying power.
            // NOTE: The first estimate MUST be greater than the target and iteration will successively decrease quantity estimates.
            //   1. Determine current holdings of position group
            //   2. If targeting zero, we can short circuit and return the negative of existing position quantities
            //   3. Determine target buying power, taking into account RequiredFreeBuyingPowerPercent
            //   4. Determine current used margin [we're using initial here to match BuyingPowerModel]
            //   5. Check that the change of margin is above our models minimum percentage change
            //   6. Resolve the group's 'unit' quantities, this is our step size
            //  6a. Compute the initial margin requirement for a single unit
            //   7. Begin iterating until the allocated holdings margin (after order fees are applied) less or equal to the expected target margin
            //  7a. Calculate the amount to order to get the target margin
            //  7b. Apply order fees to the allocated holdings margin and compare to the target margin to end loop.

            var portfolio = parameters.Portfolio;

            // 1. Determine current holdings of position group
            var currentPositionGroup = portfolio.Positions[parameters.PositionGroup.Key];

            // 2. If targeting zero, short circuit and return the negative of existing quantities
            if (parameters.TargetBuyingPower == 0m)
            {
                return parameters.Result(-currentPositionGroup.Quantity);
            }

            // 3. Determine target buying power, taking into account RequiredFreeBuyingPowerPercent
            var bufferFactor = 1 - RequiredFreeBuyingPowerPercent;
            var targetBufferFactor = bufferFactor * parameters.TargetBuyingPower;
            var totalPortfolioValue = portfolio.TotalPortfolioValue;
            var signedTargetFinalMargin = targetBufferFactor * totalPortfolioValue;

            //3a. If targeting zero, simply return the negative of the quantity
            if (signedTargetFinalMargin == 0)
            {
                return new GetMaximumLotsResult(-currentPositionGroup.Quantity, string.Empty, false);
            }

            // 4. Determine initial margin requirement for current holdings
            var currentSignedUsedMargin = 0m;
            if (currentPositionGroup.Quantity != 0)
            {
                currentSignedUsedMargin = this.GetInitialMarginRequirement(portfolio, currentPositionGroup);
            }

            // 5. Check that the change of margin is above our models minimum percentage change
            var absDifferenceOfMargin = Math.Abs(signedTargetFinalMargin - currentSignedUsedMargin);
            if (!BuyingPowerModelExtensions.AboveMinimumOrderMarginPortfolioPercentage(parameters.Portfolio,
                parameters.MinimumOrderMarginPortfolioPercentage, absDifferenceOfMargin))
            {
                string reason = null;
                if (!parameters.SilenceNonErrorReasons)
                {
                    var minimumValue = totalPortfolioValue * parameters.MinimumOrderMarginPortfolioPercentage;
                    reason = Messages.BuyingPowerModel.TargetOrderMarginNotAboveMinimum(absDifferenceOfMargin, minimumValue);
                }
                return new GetMaximumLotsResult(0, reason, false);
            }

            // 6. Resolve 'unit' group -- this is our step size
            var groupUnit = parameters.PositionGroup.Key.CreateUnitGroup();

            // 6a. Compute initial margin requirement for a single unit
            var absUnitMargin = this.GetInitialMarginRequirement(portfolio, groupUnit);
            if (absUnitMargin == 0m)
            {
                // likely due to missing price data
                var zeroPricedPosition = parameters.PositionGroup.FirstOrDefault(
                    p => portfolio.Securities.GetValueOrDefault(p.Symbol)?.Price == 0m
                );
                return parameters.Error(zeroPricedPosition?.Symbol.GetZeroPriceMessage()
                    ?? $"Computed zero initial margin requirement for {parameters.PositionGroup.GetUserFriendlyName()}."
                );
            }

            // 7. Begin iterating
            var lastPositionGroupOrderQuantity = 0m;    // For safety check
            decimal orderFees = 0m;
            decimal signedTargetHoldingsMargin;
            decimal positionGroupQuantity;
            do
            {
                // 7a.Calculate the amount to order to get the target margin
                positionGroupQuantity = GetPositionGroupOrderQuantity(portfolio, currentPositionGroup, currentSignedUsedMargin,
                    signedTargetFinalMargin, groupUnit, absUnitMargin, out signedTargetHoldingsMargin);
                if (positionGroupQuantity == 0)
                {
                    string reason = null;
                    if (!parameters.SilenceNonErrorReasons)
                    {
                        reason = $@"The position group order quantity has been rounded to zero. Target order margin {
                            signedTargetFinalMargin - currentSignedUsedMargin}. ";
                    }

                    return new GetMaximumLotsResult(0, reason, false);
                }

                // 7b.Apply order fees to the allocated holdings margin
                orderFees = GetOrderFeeInAccountCurrency(portfolio, currentPositionGroup.WithQuantity(positionGroupQuantity));

                // Update our target portfolio margin allocated when considering fees, then calculate the new FinalOrderMargin
                signedTargetFinalMargin = (totalPortfolioValue - orderFees) * targetBufferFactor;

                // Start safe check after first loop, stops endless recursion
                if (lastPositionGroupOrderQuantity == positionGroupQuantity)
                {
                    throw new ArgumentException(Invariant($@"Failed to converge on the target margin: {signedTargetFinalMargin
                        }; the following information can be used to reproduce the issue. Total Portfolio Cash: {parameters.Portfolio.Cash
                        }; Position group: {parameters.PositionGroup.GetUserFriendlyName()}; Position group order quantity: {positionGroupQuantity
                        } Order Fee: {orderFees}; Current Holdings: {parameters.PositionGroup.Quantity
                        }; Target Percentage: %{parameters.TargetBuyingPower * 100};"));
                }

                lastPositionGroupOrderQuantity = positionGroupQuantity;

            }
            // Ensure that our target holdings margin will be less than or equal to our target allocated margin
            while (Math.Abs(signedTargetHoldingsMargin) > Math.Abs(signedTargetFinalMargin));

            return parameters.Result(positionGroupQuantity);
        }

        /// <summary>
        /// Get the maximum market position group order quantity to obtain a delta in the buying power used by a position group.
        /// The deltas sign defines the position side to apply it to, positive long, negative short.
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the position group and the delta buying power</param>
        /// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
        /// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
        public virtual GetMaximumLotsResult GetMaximumLotsForDeltaBuyingPower(
            GetMaximumLotsForDeltaBuyingPowerParameters parameters
            )
        {
            // we convert this delta request into a target buying power request through projection
            // by determining the currently used (reserved) buying power and adding the delta to
            // arrive at a target buying power percentage

            var currentPositionGroup = parameters.Portfolio.Positions[parameters.PositionGroup.Key];
            var usedBuyingPower = parameters.PositionGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(
                parameters.Portfolio, currentPositionGroup
            );

            var signedUsedBuyingPower = Math.Sign(currentPositionGroup.Quantity) * usedBuyingPower;
            var targetBuyingPower = signedUsedBuyingPower + parameters.DeltaBuyingPower;

            var targetBuyingPowerPercent = parameters.Portfolio.TotalPortfolioValue != 0
                ? targetBuyingPower / parameters.Portfolio.TotalPortfolioValue
                : 0;

            return GetMaximumLotsForTargetBuyingPower(new GetMaximumLotsForTargetBuyingPowerParameters(
                parameters.Portfolio, parameters.PositionGroup, targetBuyingPowerPercent, parameters.MinimumOrderMarginPortfolioPercentage
            ));
        }

        /// <summary>
        /// Gets the buying power available for a position group trade
        /// </summary>
        /// <param name="parameters">A parameters object containing the algorithm's portfolio, security, and order direction</param>
        /// <returns>The buying power available for the trade</returns>
        public PositionGroupBuyingPower GetPositionGroupBuyingPower(PositionGroupBuyingPowerParameters parameters)
        {
            // SecurityPositionGroupBuyingPowerModel models buying power the same as non-grouped, so we can simply delegate
            // to the security's model. For posterity, however, I'll lay out the process for computing the available buying
            // power for a position group trade. There's two separate cases, one where we're increasing the position and one
            // where we're decreasing the position and potentially crossing over zero. When decreasing the position we have
            // to account for the reserved buying power that the position currently holds and add that to any free buying power
            // in the portfolio.
            //   1. Get portfolio's MarginRemaining (free buying power)
            //   2. Determine if closing position
            //   2a. Add reserved buying power freed up by closing the position
            //   2b. Rebate initial buying power required for current position [to match current behavior, might not be possible]

            // 1. Get MarginRemaining
            var buyingPower = parameters.Portfolio.MarginRemaining;

            // 2. Determine if closing position
            IPositionGroup existing;
            if (parameters.Portfolio.Positions.Groups.TryGetGroup(parameters.PositionGroup.Key, out existing) &&
                parameters.Direction.Closes(existing.GetPositionSide()))
            {
                // 2a. Add reserved buying power of current position
                buyingPower += GetReservedBuyingPowerForPositionGroup(parameters);

                // 2b. Rebate the initial margin equivalent of current position
                // this interface doesn't have a concept of initial margin as it's an impl detail of the BuyingPowerModel base class
                buyingPower += this.GetInitialMarginRequirement(parameters.Portfolio, existing);
            }

            return buyingPower;
        }

        /// <summary>
        /// Helper function to convert a <see cref="CashAmount"/> to the account currency
        /// </summary>
        protected virtual decimal ToAccountCurrency(SecurityPortfolioManager portfolio, CashAmount cash)
        {
            return portfolio.CashBook.ConvertToAccountCurrency(cash).Amount;
        }

        /// <summary>
        /// Helper function to compute the order fees associated with executing market orders for the specified <paramref name="positionGroup"/>
        /// </summary>
        protected virtual decimal GetOrderFeeInAccountCurrency(SecurityPortfolioManager portfolio, IPositionGroup positionGroup)
        {
            // TODO : Add Order parameter to support Combo order type, pulling the orders per position

            var orderFee = 0m;
            var utcTime = portfolio.Securities.UtcTime;

            foreach (var position in positionGroup)
            {
                var security = portfolio.Securities[position.Symbol];
                var order = new MarketOrder(position.Symbol, position.Quantity, utcTime);
                var positionOrderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, order)).Value;
                orderFee += ToAccountCurrency(portfolio, positionOrderFee);
            }

            return orderFee;
        }

        /// <summary>
        /// Checks if the margin difference is not growing in final margin calculation, just making sure we don't end up in an infinite loop.
        /// This function was split out to support derived types using the same error message as well as removing the added noise of the check
        /// and message creation.
        /// </summary>
        protected static bool UnableToConverge(decimal currentMarginDifference, decimal lastMarginDifference, IPositionGroup groupUnit,
            SecurityPortfolioManager portfolio, decimal positionGroupQuantity, decimal targetMargin, decimal currentMargin,
            decimal absUnitMargin, out ArgumentException error)
        {
            // determine if we're unable to converge by seeing if quantity estimate hasn't changed
            if (Math.Abs(currentMarginDifference) > Math.Abs(lastMarginDifference) &&
                Math.Sign(currentMarginDifference) == Math.Sign(lastMarginDifference))
            {
                string message;
                if (groupUnit.Count == 1)
                {
                    // single security group
                    var security = portfolio.Securities[groupUnit.Single().Symbol];
                    message = "GetMaximumPositionGroupOrderQuantityForTargetBuyingPower failed to converge to target margin " +
                        Invariant($"{targetMargin}. Current margin is {currentMargin}. Position group quantity {positionGroupQuantity}. ") +
                        Invariant($"Lot size is {security.SymbolProperties.LotSize}.Security symbol ") +
                        Invariant($"{security.Symbol}. Margin unit {absUnitMargin}.");
                }
                else
                {
                    message = "GetMaximumPositionGroupOrderQuantityForTargetBuyingPower failed to converge to target margin " +
                        Invariant($"{targetMargin}. Current margin is {currentMargin}. Position group quantity {positionGroupQuantity}. ") +
                        Invariant($"Position Group Unit is {groupUnit.Key}. Position Group Name ") +
                        Invariant($"{groupUnit.GetUserFriendlyName()}. Margin unit {absUnitMargin}.");
                }

                error = new ArgumentException(message);
                return true;
            }

            error = null;
            return false;
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public virtual bool Equals(IPositionGroupBuyingPowerModel other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return GetType() == other.GetType();
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((IPositionGroupBuyingPowerModel) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        /// <summary>
        /// Helper method that determines the amount to order to get to a given target safely.
        /// Meaning it will either be at or just below target always.
        /// </summary>
        /// <param name="portfolio">Current portfolio</param>
        /// <param name="currentPositionGroup">Current position group</param>
        /// <param name="currentUsedMargin">Current margin reserved for the position</param>
        /// <param name="targetFinalMargin">The target margin</param>
        /// <param name="groupUnit">Unit position group corresponding to the <paramref name="currentPositionGroup"/></param>
        /// <param name="unitMargin">Margin required for the <paramref name="groupUnit"/></param>
        /// <param name="finalMargin">Output the final margin allocated for the position group</param
        /// <returns>The size of the order to get safely to our target</returns>
        private decimal GetPositionGroupOrderQuantity(SecurityPortfolioManager portfolio, IPositionGroup currentPositionGroup,
            decimal currentUsedMargin, decimal targetFinalMargin, IPositionGroup groupUnit, decimal unitMargin,
            out decimal finalMargin)
        {
            // Compute initial position group quantity estimate -- group quantities are whole numbers [number of lots/unit quantities].
            // Start with order size that puts us back to 0, in theory this means current margin is 0
            // so we can calculate holdings to get to the new target margin directly. This is very helpful for
            // odd cases where margin requirements aren't linear.
            var positionGroupQuantity = -currentPositionGroup.Quantity;
            // Use the margin for one unit to make our initial guess.
            positionGroupQuantity += targetFinalMargin / unitMargin;
            // TODO: Do we need this rounding?
            // Determine the rounding mode for this order size
            var roundingMode = targetFinalMargin < 0
                // Ending in short position; orders need to be rounded towards positive so we end up under our target
                ? MidpointRounding.ToPositiveInfinity
                // Ending in long position; orders need to be rounded towards negative so we end up under our target
                : MidpointRounding.ToNegativeInfinity;
            // Round this order size appropriately
            positionGroupQuantity = positionGroupQuantity.DiscretelyRoundBy(1, roundingMode);

            // Calculate the initial value for the wanted final margin after the delta is applied.
            var finalPositionGroup = currentPositionGroup.WithQuantity(currentPositionGroup.Quantity + positionGroupQuantity);
            finalMargin = this.GetInitialMarginRequirement(portfolio, finalPositionGroup);

            // Begin iterating until  quantity is within target absFinalOrderMargin bounds (coming from above)
            var marginDifference = finalMargin - targetFinalMargin;
            while ((targetFinalMargin < 0 && marginDifference < 0) || (targetFinalMargin > 0 && marginDifference > 0))
            {
                positionGroupQuantity += targetFinalMargin < 0 ? 1 : -1;
                finalPositionGroup = currentPositionGroup.WithQuantity(currentPositionGroup.Quantity + positionGroupQuantity);
                finalMargin = this.GetInitialMarginRequirement(portfolio, finalPositionGroup);

                var newDifference = finalMargin - targetFinalMargin;

                if (UnableToConverge(newDifference, marginDifference, groupUnit, portfolio, positionGroupQuantity, targetFinalMargin,
                    currentUsedMargin, unitMargin, out var error))
                {
                    throw error;
                }

                marginDifference = newDifference;
            }

            return positionGroupQuantity;
        }
    }
}
