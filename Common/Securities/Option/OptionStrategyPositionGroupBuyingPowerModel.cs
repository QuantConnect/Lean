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
using System.Linq;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities.Positions;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Option strategy buying power model
    /// </summary>
    /// <remarks>
    /// Reference used https://www.interactivebrokers.com/en/index.php?f=26660
    /// </remarks>
    public class OptionStrategyPositionGroupBuyingPowerModel : PositionGroupBuyingPowerModel
    {
        private readonly OptionStrategy _optionStrategy;

        /// <summary>
        /// Creates a new instance for a target option strategy
        /// </summary>
        /// <param name="optionStrategy">The option strategy to model</param>
        public OptionStrategyPositionGroupBuyingPowerModel(OptionStrategy optionStrategy)
        {
            _optionStrategy = optionStrategy;
        }

        /// <summary>
        /// Gets the margin currently allocated to the specified holding
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the </returns>
        public override MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters)
        {
            if (_optionStrategy == null)
            {
                // we could be liquidating a position
                return new MaintenanceMargin(0);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ProtectivePut.Name || _optionStrategy.Name == OptionStrategyDefinitions.ProtectiveCall.Name)
            {
                // Minimum (((10% * Call/Put Strike Price) + Call/Put Out of the Money Amount), Short Stock/Long Maintenance Requirement)
                var optionPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => position.Symbol.SecurityType.IsOption());
                var underlyingPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => !position.Symbol.SecurityType.IsOption());
                var optionSecurity = (Option)parameters.Portfolio.Securities[optionPosition.Symbol];
                var underlyingSecurity = parameters.Portfolio.Securities[underlyingPosition.Symbol];

                var absOptionQuantity = Math.Abs(optionPosition.Quantity);
                var outOfTheMoneyAmount = optionSecurity.OutOfTheMoneyAmount(underlyingSecurity.Price) * optionSecurity.ContractUnitOfTrade * absOptionQuantity;

                var underlyingMarginRequired = Math.Abs(underlyingSecurity.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(
                    underlyingSecurity, underlyingPosition.Quantity)));

                var result = Math.Min(0.1m * optionSecurity.StrikePrice * optionSecurity.ContractUnitOfTrade * absOptionQuantity + outOfTheMoneyAmount, underlyingMarginRequired);
                var inAccountCurrency = parameters.Portfolio.CashBook.ConvertToAccountCurrency(result, optionSecurity.QuoteCurrency.Symbol);

                return new MaintenanceMargin(inAccountCurrency);
            }
            else if(_optionStrategy.Name == OptionStrategyDefinitions.CoveredCall.Name)
            {
                // MAX[In-the-money amount + Margin(long stock evaluated at min(mark price, strike(short call))), min(stock value, max(call value, long stock margin))]
                var optionPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => position.Symbol.SecurityType.IsOption());
                var underlyingPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => !position.Symbol.SecurityType.IsOption());
                var optionSecurity = (Option)parameters.Portfolio.Securities[optionPosition.Symbol];
                var underlyingSecurity = parameters.Portfolio.Securities[underlyingPosition.Symbol];

                var intrinsicValue = optionSecurity.GetIntrinsicValue(underlyingSecurity.Price);
                var inTheMoneyAmount = intrinsicValue * optionSecurity.ContractUnitOfTrade * Math.Abs(optionPosition.Quantity);

                var underlyingValue = underlyingSecurity.Holdings.GetQuantityValue(underlyingPosition.Quantity).InAccountCurrency;
                var optionValue = optionSecurity.Holdings.GetQuantityValue(optionPosition.Quantity).InAccountCurrency;

                // mark price, strike price
                var underlyingPriceToEvaluate = Math.Min(underlyingSecurity.Price, optionSecurity.StrikePrice);
                var underlyingHypotheticalValue = underlyingSecurity.Holdings.GetQuantityValue(underlyingPosition.Quantity, underlyingPriceToEvaluate).InAccountCurrency;

                var hypotheticalMarginRequired = underlyingSecurity.BuyingPowerModel.GetMaintenanceMargin(
                        new MaintenanceMarginParameters(underlyingSecurity, underlyingPosition.Quantity, 0, underlyingHypotheticalValue));
                var marginRequired = underlyingSecurity.BuyingPowerModel.GetMaintenanceMargin(
                    new MaintenanceMarginParameters(underlyingSecurity, underlyingPosition.Quantity, 0, underlyingValue));

                var secondOperand = Math.Min(underlyingValue, Math.Max(optionValue, marginRequired));
                var result = Math.Max(inTheMoneyAmount + hypotheticalMarginRequired, secondOperand);
                var inAccountCurrency = parameters.Portfolio.CashBook.ConvertToAccountCurrency(result, optionSecurity.QuoteCurrency.Symbol);

                return new MaintenanceMargin(inAccountCurrency);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.CoveredPut.Name)
            {
                // Initial Stock Margin Requirement + In the Money Amount
                var optionPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => position.Symbol.SecurityType.IsOption());
                var underlyingPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => !position.Symbol.SecurityType.IsOption());
                var optionSecurity = (Option)parameters.Portfolio.Securities[optionPosition.Symbol];
                var underlyingSecurity = parameters.Portfolio.Securities[underlyingPosition.Symbol];

                var intrinsicValue = optionSecurity.GetIntrinsicValue(underlyingSecurity.Price);
                var inTheMoneyAmount = intrinsicValue * optionSecurity.ContractUnitOfTrade * Math.Abs(optionPosition.Quantity);

                var initialMarginRequirement = underlyingSecurity.BuyingPowerModel.GetInitialMarginRequirement(underlyingSecurity, underlyingPosition.Quantity);

                var result = Math.Abs(initialMarginRequirement) + inTheMoneyAmount;
                var inAccountCurrency = parameters.Portfolio.CashBook.ConvertToAccountCurrency(result, optionSecurity.QuoteCurrency.Symbol);

                return new MaintenanceMargin(inAccountCurrency);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.NakedCall.Name)
            {
                var callOption = parameters.PositionGroup.Positions.Single();
                var callSecurity = (Option)parameters.Portfolio.Securities[callOption.Symbol];
                var margin = callSecurity.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(
                    callSecurity, callOption.Quantity));

                return new MaintenanceMargin(margin);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.NakedPut.Name)
            {
                var putOption = parameters.PositionGroup.Positions.Single();
                var putSecurity = (Option)parameters.Portfolio.Securities[putOption.Symbol];
                var margin = putSecurity.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(
                    putSecurity, putOption.Quantity));

                return new MaintenanceMargin(margin);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.BearCallSpread.Name
                || _optionStrategy.Name == OptionStrategyDefinitions.BullCallSpread.Name)
            {
                var result = GetLongCallShortCallStrikeDifferenceMargin(parameters.PositionGroup, parameters.Portfolio);
                return new MaintenanceMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.CallCalendarSpread.Name
                || _optionStrategy.Name == OptionStrategyDefinitions.PutCalendarSpread.Name)
            {
                return new MaintenanceMargin(0);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ShortCallCalendarSpread.Name
                || _optionStrategy.Name == OptionStrategyDefinitions.ShortPutCalendarSpread.Name)
            {
                var shortCall = parameters.PositionGroup.Positions.Single(position => position.Quantity < 0);
                var shortCallSecurity = (Option)parameters.Portfolio.Securities[shortCall.Symbol];
                var result = shortCallSecurity.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(
                    shortCallSecurity, shortCall.Quantity));

                return new MaintenanceMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.BearPutSpread.Name
                || _optionStrategy.Name == OptionStrategyDefinitions.BullPutSpread.Name)
            {
                var result = GetShortPutLongPutStrikeDifferenceMargin(parameters.PositionGroup, parameters.Portfolio);
                return new MaintenanceMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.Straddle.Name || _optionStrategy.Name == OptionStrategyDefinitions.Strangle.Name)
            {
                // Margined as two long options: since there is not margin requirements for long options, we return 0
                return new MaintenanceMargin(0);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ShortStraddle.Name || _optionStrategy.Name == OptionStrategyDefinitions.ShortStrangle.Name)
            {
                var result = GetShortStraddleStrangleMargin(parameters.PositionGroup, parameters.Portfolio,
                    (option, quantity) => Math.Abs(option.BuyingPowerModel.GetMaintenanceMargin(
                        MaintenanceMarginParameters.ForQuantityAtCurrentPrice(option, quantity))));
                return new MaintenanceMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ButterflyCall.Name || _optionStrategy.Name == OptionStrategyDefinitions.ButterflyPut.Name)
            {
                return new MaintenanceMargin(0);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ShortButterflyPut.Name || _optionStrategy.Name == OptionStrategyDefinitions.ShortButterflyCall.Name)
            {
                var result = GetMiddleAndLowStrikeDifference(parameters.PositionGroup, parameters.Portfolio);
                return new MaintenanceMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.IronCondor.Name)
            {
                var result = GetShortPutLongPutStrikeDifferenceMargin(parameters.PositionGroup, parameters.Portfolio);
                return new MaintenanceMargin(result);
            }

            throw new NotImplementedException($"Option strategy {_optionStrategy.Name} margin modeling has yet to be implemented");
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity</param>
        public override InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters)
        {
            if (_optionStrategy.Name == OptionStrategyDefinitions.ProtectivePut.Name || _optionStrategy.Name == OptionStrategyDefinitions.ProtectiveCall.Name)
            {
                // 	Initial Standard Stock Margin Requirement
                var underlyingPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => !position.Symbol.SecurityType.IsOption());
                var underlyingSecurity = parameters.Portfolio.Securities[underlyingPosition.Symbol];

                var result = Math.Abs(underlyingSecurity.BuyingPowerModel.GetInitialMarginRequirement(underlyingSecurity, underlyingPosition.Quantity));

                var inAccountCurrency = parameters.Portfolio.CashBook.ConvertToAccountCurrency(result, underlyingSecurity.QuoteCurrency.Symbol);
                return new InitialMargin(inAccountCurrency);
            }
            else if(_optionStrategy.Name == OptionStrategyDefinitions.CoveredCall.Name)
            {
                // Max(Call Value, Long Stock Initial Margin)
                var optionPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => position.Symbol.SecurityType.IsOption());
                var underlyingPosition = parameters.PositionGroup.Positions.FirstOrDefault(position => !position.Symbol.SecurityType.IsOption());
                var optionSecurity = (Option)parameters.Portfolio.Securities[optionPosition.Symbol];
                var underlyingSecurity = parameters.Portfolio.Securities[underlyingPosition.Symbol];

                var optionValue = Math.Abs(optionSecurity.Holdings.GetQuantityValue(optionPosition.Quantity).InAccountCurrency);

                var marginRequired = underlyingSecurity.BuyingPowerModel.GetInitialMarginRequirement(underlyingSecurity, underlyingPosition.Quantity);

                // IB charges more than expected, this formula was infered based on actual requirements see 'CoveredCallInitialMarginRequirementsTestCases'
                var result = optionValue * 0.8m + marginRequired;
                var inAccountCurrency = parameters.Portfolio.CashBook.ConvertToAccountCurrency(result, optionSecurity.QuoteCurrency.Symbol);

                return new InitialMargin(inAccountCurrency);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.CoveredPut.Name)
            {
                // Initial Stock Margin Requirement + In the Money Amount
                var margin = GetMaintenanceMargin(new PositionGroupMaintenanceMarginParameters(parameters.Portfolio, parameters.PositionGroup));

                return new InitialMargin(margin.Value);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.NakedCall.Name)
            {
                var callOption = parameters.PositionGroup.Positions.Single();
                var callSecurity = (Option)parameters.Portfolio.Securities[callOption.Symbol];
                var margin = callSecurity.BuyingPowerModel.GetInitialMarginRequirement(callSecurity, callOption.Quantity);

                return new InitialMargin(margin);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.NakedPut.Name)
            {
                var putOption = parameters.PositionGroup.Positions.Single();
                var putSecurity = (Option)parameters.Portfolio.Securities[putOption.Symbol];
                var margin = putSecurity.BuyingPowerModel.GetInitialMarginRequirement(putSecurity, putOption.Quantity);

                return new InitialMargin(margin);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.BearCallSpread.Name
                || _optionStrategy.Name == OptionStrategyDefinitions.BullCallSpread.Name)
            {
                var result = GetLongCallShortCallStrikeDifferenceMargin(parameters.PositionGroup, parameters.Portfolio);
                return new InitialMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.CallCalendarSpread.Name
                || _optionStrategy.Name == OptionStrategyDefinitions.PutCalendarSpread.Name)
            {
                return new InitialMargin(0);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ShortCallCalendarSpread.Name
                || _optionStrategy.Name == OptionStrategyDefinitions.ShortPutCalendarSpread.Name)
            {
                var shortOptionPosition = parameters.PositionGroup.Positions.Single(position => position.Quantity < 0);
                var shortOption = (Option)parameters.Portfolio.Securities[shortOptionPosition.Symbol];
                var result = shortOption.BuyingPowerModel.GetInitialMarginRequirement(shortOption, shortOptionPosition.Quantity);

                return new InitialMargin(Math.Abs(result));
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.BearPutSpread.Name
                || _optionStrategy.Name == OptionStrategyDefinitions.BullPutSpread.Name)
            {
                var result = GetShortPutLongPutStrikeDifferenceMargin(parameters.PositionGroup, parameters.Portfolio);
                return new InitialMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.Straddle.Name || _optionStrategy.Name == OptionStrategyDefinitions.Strangle.Name)
            {
                // Margined as two long options: since there is not margin requirements for long options, we return 0
                return new InitialMargin(0);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ShortStraddle.Name || _optionStrategy.Name == OptionStrategyDefinitions.ShortStrangle.Name)
            {
                var result = GetShortStraddleStrangleMargin(parameters.PositionGroup, parameters.Portfolio,
                    (option, quantity) => Math.Abs(option.BuyingPowerModel.GetInitialMarginRequirement(option, quantity)));
                return new InitialMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ButterflyCall.Name || _optionStrategy.Name == OptionStrategyDefinitions.ButterflyPut.Name)
            {
                return new InitialMargin(0);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.ShortButterflyPut.Name || _optionStrategy.Name == OptionStrategyDefinitions.ShortButterflyCall.Name)
            {
                var result = GetMiddleAndLowStrikeDifference(parameters.PositionGroup, parameters.Portfolio);
                return new InitialMargin(result);
            }
            else if (_optionStrategy.Name == OptionStrategyDefinitions.IronCondor.Name)
            {
                var result = GetShortPutLongPutStrikeDifferenceMargin(parameters.PositionGroup, parameters.Portfolio);
                return new InitialMargin(result);
            }

            throw new NotImplementedException($"Option strategy {_optionStrategy.Name} margin modeling has yet to be implemented");
        }

        /// <summary>
        /// Gets the total margin required to execute the specified order in units of the account currency including fees
        /// </summary>
        /// <param name="parameters">An object containing the portfolio, the security and the order</param>
        /// <returns>The total margin in terms of the currency quoted in the order</returns>
        public override InitialMargin GetInitialMarginRequiredForOrder(PositionGroupInitialMarginForOrderParameters parameters)
        {
            var security = parameters.Portfolio.Securities[parameters.Order.Symbol];
            var fees = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, parameters.Order));
            var feesInAccountCurrency = parameters.Portfolio.CashBook.ConvertToAccountCurrency(fees.Value);

            var initialMarginRequired = GetInitialMarginRequirement(new PositionGroupInitialMarginParameters(parameters.Portfolio, parameters.PositionGroup));

            var feesWithSign = Math.Sign(initialMarginRequired) * feesInAccountCurrency.Amount;

            return new InitialMargin(feesWithSign + initialMarginRequired);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return _optionStrategy.Name;
        }

        /// <summary>
        /// Returns the Maximum (Short Put Strike - Long Put Strike, 0)
        /// </summary>
        private static decimal GetShortPutLongPutStrikeDifferenceMargin(IPositionGroup positionGroup, SecurityPortfolioManager portfolio)
        {
            var longOption = positionGroup.Positions.Single(position => position.Symbol.ID.OptionRight == OptionRight.Put && position.Quantity > 0);
            var shortOption = positionGroup.Positions.Single(position => position.Symbol.ID.OptionRight == OptionRight.Put && position.Quantity < 0);
            var optionSecurity = (Option)portfolio.Securities[longOption.Symbol];

            // Maximum (Short Put Strike - Long Put Strike, 0)
            var strikeDifference = shortOption.Symbol.ID.StrikePrice - longOption.Symbol.ID.StrikePrice;

            var result = Math.Max(strikeDifference * optionSecurity.ContractUnitOfTrade * Math.Abs(positionGroup.Quantity), 0);

            // convert into account currency
            return portfolio.CashBook.ConvertToAccountCurrency(result, optionSecurity.QuoteCurrency.Symbol);
        }

        /// <summary>
        /// Returns the Maximum (Strike Long Call - Strike Short Call, 0)
        /// </summary>
        private static decimal GetLongCallShortCallStrikeDifferenceMargin(IPositionGroup positionGroup, SecurityPortfolioManager portfolio)
        {
            var longOption = positionGroup.Positions.Single(position => position.Symbol.ID.OptionRight == OptionRight.Call && position.Quantity > 0);
            var shortOption = positionGroup.Positions.Single(position => position.Symbol.ID.OptionRight == OptionRight.Call && position.Quantity < 0);
            var optionSecurity = (Option)portfolio.Securities[longOption.Symbol];

            var strikeDifference = longOption.Symbol.ID.StrikePrice - shortOption.Symbol.ID.StrikePrice;

            var result = Math.Max(strikeDifference * optionSecurity.ContractUnitOfTrade * Math.Abs(positionGroup.Quantity), 0);

            // convert into account currency
            return portfolio.CashBook.ConvertToAccountCurrency(result, optionSecurity.QuoteCurrency.Symbol);
        }

        /// <summary>
        /// Returns the Maximum (Middle Strike - Lowest Strike, 0)
        /// </summary>
        private static decimal GetMiddleAndLowStrikeDifference(IPositionGroup positionGroup, SecurityPortfolioManager portfolio)
        {
            var options = positionGroup.Positions.OrderBy(position => position.Symbol.ID.StrikePrice).ToList();
            var lowestCallStrike = options[0].Symbol.ID.StrikePrice;
            var middleCallStrike = options[1].Symbol.ID.StrikePrice;
            var optionSecurity = (Option)portfolio.Securities[options[0].Symbol];

            var strikeDifference = Math.Max((middleCallStrike - lowestCallStrike) * optionSecurity.ContractUnitOfTrade * Math.Abs(positionGroup.Quantity), 0);

            // convert into account currency
            return portfolio.CashBook.ConvertToAccountCurrency(strikeDifference, optionSecurity.QuoteCurrency.Symbol);
        }

        /// <summary>
        /// Returns the margin for a short straddle or strangle.
        /// This is the same for both the initial margin requirement and the maintenance margin.
        /// </summary>
        private static decimal GetShortStraddleStrangleMargin(IPositionGroup positionGroup, SecurityPortfolioManager portfolio,
            Func<Option, decimal, decimal> getOptionMargin)
        {
            var callOption = positionGroup.Positions.Single(position => position.Symbol.ID.OptionRight == OptionRight.Call);
            var callSecurity = (Option)portfolio.Securities[callOption.Symbol];
            var callMargin = getOptionMargin(callSecurity, callOption.Quantity);

            var putOption = positionGroup.Positions.Single(position => position.Symbol.ID.OptionRight == OptionRight.Put);
            var putSecurity = (Option)portfolio.Securities[putOption.Symbol];
            var putMargin = getOptionMargin(putSecurity, putOption.Quantity);

            var result = 0m;

            if (putMargin > callMargin)
            {
                result = putMargin + callSecurity.Price * callSecurity.ContractUnitOfTrade * Math.Abs(callOption.Quantity);
            }
            else
            {
                result = callMargin + putSecurity.Price * putSecurity.ContractUnitOfTrade * Math.Abs(putOption.Quantity);
            }

            return result;
        }
    }
}
