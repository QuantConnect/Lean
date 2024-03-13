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
using QuantConnect.Orders.Fees;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Represents a simple option margin model.
    /// </summary>
    /// <remarks>
    /// Options are not traded on margin. Margin requirements exist though for those portfolios with short positions.
    /// Current implementation covers only single long/naked short option positions.
    /// </remarks>
    public class OptionMarginModel : SecurityMarginModel
    {
        // initial margin
        private const decimal OptionMarginRequirement = 1;
        private const decimal NakedPositionMarginRequirement = 0.1m;
        private const decimal EquityOptionNakedPositionMarginRequirementOtm = 0.2m;
        private const decimal IndexOptionNakedPositionMarginRequirementOtm = 0.15m;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionMarginModel"/>
        /// </summary>
        /// <param name="requiredFreeBuyingPowerPercent">The percentage used to determine the required unused buying power for the account.</param>
        public OptionMarginModel(decimal requiredFreeBuyingPowerPercent = 0)
        {
            RequiredFreeBuyingPowerPercent = requiredFreeBuyingPowerPercent;
        }

        /// <summary>
        /// Gets the current leverage of the security
        /// </summary>
        /// <param name="security">The security to get leverage for</param>
        /// <returns>The current leverage in the security</returns>
        public override decimal GetLeverage(Security security)
        {
            // Options are not traded on margin
            return 1;
        }

        /// <summary>
        /// Sets the leverage for the applicable securities, i.e, options.
        /// </summary>
        /// <param name="security"></param>
        /// <param name="leverage">The new leverage</param>
        public override void SetLeverage(Security security, decimal leverage)
        {
            // Options are leveraged products and different leverage cannot be set by user.
            throw new InvalidOperationException("Options are leveraged products and different leverage cannot be set by user");
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public override InitialMargin GetInitialMarginRequiredForOrder(
            InitialMarginRequiredForOrderParameters parameters
            )
        {
            //Get the order value from the non-abstract order classes (MarketOrder, LimitOrder, StopMarketOrder)
            //Market order is approximated from the current security price and set in the MarketOrder Method in QCAlgorithm.

            var fees = parameters.Security.FeeModel.GetOrderFee(
                new OrderFeeParameters(parameters.Security, parameters.Order)
            );

            var feesInAccountCurrency = parameters.CurrencyConverter.ConvertToAccountCurrency(fees.Value);

            var value = parameters.Order.GetValue(parameters.Security);
            var orderMargin = value * GetMarginRequirement(parameters.Security, parameters.Order.Quantity, value);

            return orderMargin + Math.Sign(orderMargin) * feesInAccountCurrency.Amount;
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the provided holdings quantity/cost/value</returns>
        public override MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
        {
            // Long options have zero maintenance margin requirement
            return parameters.Quantity >= 0 ? 0 : parameters.AbsoluteHoldingsCost * GetMaintenanceMarginRequirement(parameters);
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <returns>The initial margin required for the provided security and quantity</returns>
        public override InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            var security = parameters.Security;
            var quantity = parameters.Quantity;
            var value = security.QuoteCurrency.ConversionRate
                        * security.SymbolProperties.ContractMultiplier
                        * security.Price
                        * quantity;

            // Initial margin requirement for long options is only the premium that is paid upfront
            return new OptionInitialMargin(parameters.Quantity >= 0 ? 0 : value * GetMarginRequirement(security, quantity, value), value);
        }

        /// <summary>
        /// The percentage of the holding's absolute cost that must be held in free cash in order to avoid a margin call
        /// </summary>
        private decimal GetMaintenanceMarginRequirement(MaintenanceMarginParameters parameters)
        {
            return GetMarginRequirement(parameters.Security, parameters.Quantity, parameters.HoldingsCost);
        }

        /// <summary>
        /// Private method takes option security and its holding and returns required margin. Method considers all short positions naked.
        /// </summary>
        /// <param name="security">Option security</param>
        /// <param name="quantity">Holding quantity</param>
        /// <param name="value">Holding value</param>
        /// <returns></returns>
        private decimal GetMarginRequirement(Security security, decimal quantity, decimal value)
        {
            var option = (Option)security;

            if (value == 0m ||
                option.Close == 0m ||
                option.StrikePrice == 0m ||
                option.Underlying == null ||
                option.Underlying.Close == 0m)
            {
                return 0m;
            }

            if (value > 0m)
            {
                return OptionMarginRequirement;
            }

            var absValue = -value;
            var optionProperties = (OptionSymbolProperties)option.SymbolProperties;
            var underlying = option.Underlying;

            // inferring ratios of the option and its underlying to get underlying security value
            var multiplierRatio = underlying.SymbolProperties.ContractMultiplier / optionProperties.ContractMultiplier;
            var quantityRatio = optionProperties.ContractUnitOfTrade;

            // Some options are based on a fraction of their underlying security value, such as NQX for example. Thus,
            // for them we need to scale the underlying value so that the later comparisons made with the option's strike
            // value are correct
            var priceRatio = (underlying.Close / option.SymbolProperties.StrikeMultiplier) / (absValue / quantityRatio);
            var underlyingValueRatio = multiplierRatio * quantityRatio * priceRatio;

            // calculating underlying security value less out-of-the-money amount
            var amountOTM = option.OutOfTheMoneyAmount(underlying.Close);
            var priceRatioOTM = amountOTM / (absValue / quantityRatio);
            var underlyingValueRatioOTM = multiplierRatio * quantityRatio * priceRatioOTM;

            var strikePriceRatio = option.StrikePrice / (absValue / quantityRatio);
            strikePriceRatio = multiplierRatio * quantityRatio * strikePriceRatio;

            var nakedMarginRequirement = option.Right == OptionRight.Call
                ? NakedPositionMarginRequirement * underlyingValueRatio
                : NakedPositionMarginRequirement * strikePriceRatio;
            var nakedMarginRequirementOtm = security.Type == SecurityType.Option
                ? EquityOptionNakedPositionMarginRequirementOtm
                : IndexOptionNakedPositionMarginRequirementOtm;

            return OptionMarginRequirement +
                   Math.Abs(quantity) * Math.Max(nakedMarginRequirement,
                       nakedMarginRequirementOtm * underlyingValueRatio - underlyingValueRatioOTM);
        }
    }
}
