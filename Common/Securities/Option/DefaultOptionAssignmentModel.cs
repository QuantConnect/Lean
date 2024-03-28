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

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// The option assignment model emulates exercising of short option positions in the portfolio.
    /// Simulator implements basic no-arb argument: when time value of the option contract is close to zero
    /// it assigns short legs getting profit close to expiration dates in deep ITM positions. User algorithm then receives
    /// assignment event from LEAN. Simulator randomly scans for arbitrage opportunities every two hours or so.
    /// </summary>
    public class DefaultOptionAssignmentModel : IOptionAssignmentModel
    {
        // when we start simulating assignments prior to expiration
        private readonly TimeSpan _priorExpiration;

        // we focus only on deep ITM calls and puts
        private readonly decimal _requiredInTheMoneyPercent;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="requiredInTheMoneyPercent">The percent in the money the option has to be to trigger the option assignment</param>
        /// <param name="priorExpiration">For <see cref="OptionStyle.American"/>, the time span prior to expiration were we will try to evaluate option assignment</param>
        public DefaultOptionAssignmentModel(decimal requiredInTheMoneyPercent = 0.05m, TimeSpan? priorExpiration = null)
        {
            _priorExpiration = priorExpiration ?? new TimeSpan(4, 0, 0, 0);
            _requiredInTheMoneyPercent = requiredInTheMoneyPercent;
        }

        /// <summary>
        /// Get's the option assignments to generate if any
        /// </summary>
        /// <param name="parameters">The option assignment parameters data transfer class</param>
        /// <returns>The option assignment result</returns>
        public virtual OptionAssignmentResult GetAssignment(OptionAssignmentParameters parameters)
        {
            var option = parameters.Option;
            var underlying = parameters.Option.Underlying;

            // we take only options that expire soon
            if ((option.Symbol.ID.OptionStyle == OptionStyle.American && option.Symbol.ID.Date - option.LocalTime <= _priorExpiration ||
                option.Symbol.ID.OptionStyle == OptionStyle.European && option.Symbol.ID.Date.Date == option.LocalTime.Date)
                // we take only deep ITM strikes
                && IsDeepInTheMoney(option))
            {
                // we estimate P/L
                var potentialPnL = EstimateArbitragePnL(option, (OptionHolding)option.Holdings, underlying);
                if (potentialPnL > 0)
                {
                    return new OptionAssignmentResult(option.Holdings.AbsoluteQuantity, "Simulated option assignment before expiration");
                }
            }

            return OptionAssignmentResult.Null;
        }

        private bool IsDeepInTheMoney(Option option)
        {
            var symbol = option.Symbol;
            var underlyingPrice = option.Underlying.Close;

            // For some options, the price is based on a fraction of the underlying, such as for NQX.
            // Therefore, for those options we need to scale the price when comparing it with the
            // underlying. For that reason we use option.ScaledStrikePrice instead of
            // option.StrikePrice
            var result =
                symbol.ID.OptionRight == OptionRight.Call
                    ? (underlyingPrice - option.ScaledStrikePrice) / underlyingPrice > _requiredInTheMoneyPercent
                    : (option.ScaledStrikePrice - underlyingPrice) / underlyingPrice > _requiredInTheMoneyPercent;

            return result;
        }

        private static decimal EstimateArbitragePnL(Option option, OptionHolding holding, Security underlying)
        {
            // no-arb argument:
            // if our long deep ITM position has a large B/A spread and almost no time value, it may be interesting for us
            // to exercise the option and close the resulting position in underlying instrument, if we want to exit now.

            // User's short option position is our long one.
            // In order to sell ITM position we take option bid price as an input
            var optionPrice = option.BidPrice;

            // we are interested in underlying bid price if we exercise calls and want to sell the underlying immediately.
            // we are interested in underlying ask price if we exercise puts
            var underlyingPrice = option.Symbol.ID.OptionRight == OptionRight.Call
                ? underlying.BidPrice
                : underlying.AskPrice;

            // quantity is normally negative algo's holdings, but since we're modeling the contract holder (counter-party)
            // it's negative THEIR holdings. holding.Quantity is negative, so if counter-party exercises, they would reduce holdings
            var underlyingQuantity = option.GetExerciseQuantity(holding.Quantity);

            // Scenario 1 (base): we just close option position
            var marketOrder1 = new MarketOrder(option.Symbol, -holding.Quantity, option.LocalTime.ConvertToUtc(option.Exchange.TimeZone));
            var orderFee1 = option.FeeModel.GetOrderFee(
                new OrderFeeParameters(option, marketOrder1)).Value.Amount * option.QuoteCurrency.ConversionRate;

            var basePnL = (optionPrice - holding.AveragePrice) * -holding.Quantity
                * option.QuoteCurrency.ConversionRate
                * option.SymbolProperties.ContractMultiplier
                - orderFee1;

            // Scenario 2 (alternative): we exercise option and then close underlying position
            var optionExerciseOrder2 = new OptionExerciseOrder(option.Symbol, (int)holding.AbsoluteQuantity, option.LocalTime.ConvertToUtc(option.Exchange.TimeZone));
            var optionOrderFee2 = option.FeeModel.GetOrderFee(
                new OrderFeeParameters(option, optionExerciseOrder2)).Value.Amount * option.QuoteCurrency.ConversionRate;

            var underlyingOrderFee2Amount = 0m;

            // Cash settlements do not open a position for the underlying.
            // For Physical Delivery, we calculate the order fee since we have to close the position
            if (option.ExerciseSettlement == SettlementType.PhysicalDelivery)
            {
                var underlyingMarketOrder2 = new MarketOrder(underlying.Symbol, -underlyingQuantity,
                    underlying.LocalTime.ConvertToUtc(underlying.Exchange.TimeZone));
                var underlyingOrderFee2 = underlying.FeeModel.GetOrderFee(
                    new OrderFeeParameters(underlying, underlyingMarketOrder2)).Value.Amount * underlying.QuoteCurrency.ConversionRate;
                underlyingOrderFee2Amount = underlyingOrderFee2;
            }

            // calculating P/L of the two transactions (exercise option and then close underlying position)
            var altPnL = (underlyingPrice - option.ScaledStrikePrice) * underlyingQuantity * underlying.QuoteCurrency.ConversionRate * option.ContractUnitOfTrade
                        - underlyingOrderFee2Amount
                        - holding.AveragePrice * holding.AbsoluteQuantity * option.SymbolProperties.ContractMultiplier * option.QuoteCurrency.ConversionRate
                        - optionOrderFee2;

            return altPnL - basePnL;
        }
    }
}
