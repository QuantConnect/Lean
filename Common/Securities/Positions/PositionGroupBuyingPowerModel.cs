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

using System.Collections.Generic;
using System.Linq;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Provides a base class for implementations of <see cref="IPositionGroupBuyingPowerModel"/>
    /// </summary>
    public abstract class PositionGroupBuyingPowerModel : IPositionGroupBuyingPowerModel
    {
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
            var impactedPositions = parameters.ContemplatedChanges.ToDictionary(p => p.Symbol);
            foreach (var impactedGroup in positionManager.GetImpactedGroups(parameters.ContemplatedChanges))
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
            var contemplatedGroups = positionManager.ResolvePositionGroups(
                new PositionCollection(impactedPositions.Values)
            );

            // 5. Compute contemplated reserved buying power
            var contemplated = 0m;
            foreach (var contemplatedGroup in contemplatedGroups)
            {
                contemplated += contemplatedGroup.BuyingPowerModel.GetReservedBuyingPowerForPositionGroup(
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
            var availableBuyingPower = this.GetPositionGroupBuyingPower(
                parameters.Portfolio, parameters.PositionGroup, parameters.Order.Direction
            );

            // 2. Confirm we pass position group specific checks
            var result = PassesPositionGroupSpecificBuyingPowerForOrderChecks(parameters, availableBuyingPower);
            if (result?.IsSufficient == false)
            {
                return result;
            }

            // 3. Confirm that the new groupings arising from the change doesn't make maintenance margin exceed TPV
            var deltaBuyingPower = this.GetChangeInReservedBuyingPower(parameters.Portfolio, parameters.PositionGroup);
            if (deltaBuyingPower <= availableBuyingPower)
            {
                return parameters.Sufficient();
            }

            return parameters.Insufficient(Invariant(
                $"Id: {parameters.Order.Id}, Maintenance Margin Delta: {deltaBuyingPower.Normalize()}, Free Margin: {availableBuyingPower.Value.Normalize()}"
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
            return this.GetMaintenanceMargin(parameters.Portfolio, parameters.PositionGroup, true);
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

        public virtual decimal ToAccountCurrency(SecurityPortfolioManager portfolio, CashAmount cash)
        {
            return portfolio.CashBook.ConvertToAccountCurrency(cash).Amount;
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

            return Equals((SecurityPositionGroupBuyingPowerModel) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }
    }
}
