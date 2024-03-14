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
using QuantConnect.Securities.Future;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Defines a margin model for future options (an option with a future as its underlying).
    /// We re-use the <see cref="FutureMarginModel"/> implementation and multiply its results
    /// by 1.5x to simulate the increased margins seen for future options.
    /// </summary>
    public class FuturesOptionsMarginModel : FutureMarginModel
    {
        private readonly Option _futureOption;

        /// <summary>
        /// Initial Overnight margin requirement for the contract effective from the date of change
        /// </summary>
        public override decimal InitialOvernightMarginRequirement => GetMarginRequirement(_futureOption, base.InitialOvernightMarginRequirement);

        /// <summary>
        /// Maintenance Overnight margin requirement for the contract effective from the date of change
        /// </summary>
        public override decimal MaintenanceOvernightMarginRequirement => GetMarginRequirement(_futureOption, base.MaintenanceOvernightMarginRequirement);

        /// <summary>
        /// Initial Intraday margin for the contract effective from the date of change
        /// </summary>
        public override decimal InitialIntradayMarginRequirement => GetMarginRequirement(_futureOption, base.InitialIntradayMarginRequirement);

        /// <summary>
        /// Maintenance Intraday margin requirement for the contract effective from the date of change
        /// </summary>
        public override decimal MaintenanceIntradayMarginRequirement => GetMarginRequirement(_futureOption, base.MaintenanceIntradayMarginRequirement);

        /// <summary>
        /// Creates an instance of FutureOptionMarginModel
        /// </summary>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage used to determine the required unused buying power for the account.</param>
        /// <param name="futureOption">Option Security containing a Future security as the underlying</param>
        public FuturesOptionsMarginModel(decimal requiredFreeBuyingPowerPercent = 0, Option futureOption = null) : base(requiredFreeBuyingPowerPercent, futureOption?.Underlying)
        {
            _futureOption = futureOption;
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding.
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the option</returns>
        /// <remarks>
        /// We fix the option to 1.5x the maintenance because of its close coupling with the underlying.
        /// The option's contract multiplier is 1x, but might be more sensitive to volatility shocks in the long
        /// run when it comes to calculating the different market scenarios attempting to simulate VaR, resulting
        /// in a margin greater than the underlying's margin.
        /// </remarks>
        public override MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
        {
            var underlyingRequirement = base.GetMaintenanceMargin(parameters.ForUnderlying(parameters.Quantity));
            var positionSide = parameters.Quantity > 0 ? PositionSide.Long : PositionSide.Short;
            return GetMarginRequirement(_futureOption, underlyingRequirement, positionSide);
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity of shares</param>
        /// <returns>The initial margin required for the option (i.e. the equity required to enter a position for this option)</returns>
        /// <remarks>
        /// We fix the option to 1.5x the initial because of its close coupling with the underlying.
        /// The option's contract multiplier is 1x, but might be more sensitive to volatility shocks in the long
        /// run when it comes to calculating the different market scenarios attempting to simulate VaR, resulting
        /// in a margin greater than the underlying's margin.
        /// </remarks>
        public override InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            var underlyingRequirement = base.GetInitialMarginRequirement(parameters.ForUnderlying()).Value;
            var positionSide = parameters.Quantity > 0 ? PositionSide.Long : PositionSide.Short;

            return new InitialMargin(GetMarginRequirement(_futureOption, underlyingRequirement, positionSide));
        }

        /// <summary>
        /// Get's the margin requirement for a future option based on the underlying future margin requirement and the position side to trade.
        /// FOPs margin requirement is an 'S' curve based on the underlying requirement around it's current price, see https://en.wikipedia.org/wiki/Logistic_function
        /// </summary>
        /// <param name="option">The future option contract to trade</param>
        /// <param name="underlyingRequirement">The underlying future associated margin requirement</param>
        /// <param name="positionSide">The position side to trade, long by default. This is because short positions require higher margin requirements</param>
        public static int GetMarginRequirement(Option option, decimal underlyingRequirement, PositionSide positionSide = PositionSide.Long)
        {
            var maximumValue = underlyingRequirement;
            var curveGrowthRate = -7.8m;
            var underlyingPrice = option.Underlying.Price;

            // If the underlying price is 0, we can't calculate a margin requirement, so return the underlying requirement.
            // This could be removed after GH issue #6523 is resolved.
            if (option.Underlying == null || option.Underlying.Price == 0m)
            {
                return 0;
            }

            if (positionSide == PositionSide.Short)
            {
                if (option.Right == OptionRight.Call)
                {
                    // going short the curve growth rate is slower
                    curveGrowthRate = -4m;
                    // curve shifted to the right -> causes a margin requirement increase
                    underlyingPrice *= 1.5m;
                }
                else
                {
                    // higher max requirements
                    maximumValue *= 1.25m;
                    // puts are inverter from calls
                    curveGrowthRate = 2.4m;
                    // curve shifted to the left -> causes a margin requirement increase
                    underlyingPrice *= 0.30m;
                }
            }
            else
            {
                if (option.Right == OptionRight.Put)
                {
                    // fastest change rate
                    curveGrowthRate = 9m;
                }
                else
                {
                    maximumValue *= 1.20m;
                }
            }

            // we normalize the curve growth rate by dividing by the underlyings price
            // this way, contracts with different order of magnitude price and strike (like CL & ES) share this logic
            var denominator = Math.Pow(Math.E, (double) (-curveGrowthRate * (option.ScaledStrikePrice - underlyingPrice) / underlyingPrice));

            if (double.IsInfinity(denominator))
            {
                return 0;
            }
            if (denominator.IsNaNOrZero())
            {
                return (int) maximumValue;
            }

            return (int) (maximumValue / (1 + denominator).SafeDecimalCast());
        }
    }
}
