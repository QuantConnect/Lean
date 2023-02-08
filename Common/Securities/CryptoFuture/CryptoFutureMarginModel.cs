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
using QuantConnect.Orders;

namespace QuantConnect.Securities.CryptoFuture
{
    /// <summary>
    /// The crypto future margin model which supports both Coin and USDT futures
    /// </summary>
    public class CryptoFutureMarginModel : SecurityMarginModel
    {
        private readonly decimal _maintenanceMarginRate;
        private readonly decimal _maintenanceAmount;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="leverage">The leverage to use, used on initial margin requirements, default 25x</param>
        /// <param name="maintenanceMarginRate">The maintenance margin rate, default 5%</param>
        /// <param name="maintenanceAmount">The maintenance amount which will reduce maintenance margin requirements, default 0</param>
        public CryptoFutureMarginModel(decimal leverage = 25, decimal maintenanceMarginRate = 0.05m, decimal maintenanceAmount = 0)
             : base(leverage, 0)
        {
            _maintenanceAmount = maintenanceAmount;
            _maintenanceMarginRate = maintenanceMarginRate;
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding.
        /// </summary>
        /// <param name="parameters">An object containing the security</param>
        /// <returns>The maintenance margin required for the option</returns>
        public override MaintenanceMargin GetMaintenanceMargin(MaintenanceMarginParameters parameters)
        {
            var security = parameters.Security;
            var quantity = parameters.Quantity;
            if (security?.GetLastData() == null || quantity == 0m)
            {
                return MaintenanceMargin.Zero;
            }

            var positionValue = security.Holdings.GetQuantityValue(quantity, security.Price);
            var marginRequirementInCollateral = Math.Abs(positionValue.Amount) * _maintenanceMarginRate - _maintenanceAmount;

            return new MaintenanceMargin(marginRequirementInCollateral * positionValue.Cash.ConversionRate);
        }

        /// <summary>
        /// The margin that must be held in order to increase the position by the provided quantity
        /// </summary>
        /// <param name="parameters">An object containing the security and quantity of shares</param>
        /// <returns>The initial margin required for the option (i.e. the equity required to enter a position for this option)</returns>
        public override InitialMargin GetInitialMarginRequirement(InitialMarginParameters parameters)
        {
            var security = parameters.Security;
            var quantity = parameters.Quantity;
            if (security?.GetLastData() == null || quantity == 0m)
            {
                return InitialMargin.Zero;
            }

            var positionValue = security.Holdings.GetQuantityValue(quantity, security.Price);
            var marginRequirementInCollateral = Math.Abs(positionValue.Amount) / GetLeverage(security);

            return new InitialMargin(marginRequirementInCollateral * positionValue.Cash.ConversionRate);
        }

        /// <summary>
        /// Gets the margin cash available for a trade
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The security to be traded</param>
        /// <param name="direction">The direction of the trade</param>
        /// <returns>The margin available for the trade</returns>
        /// <remarks>What we do specially here is that instead of using the total portfolio value as potential margin remaining we only consider the collateral currency</remarks>
        protected override decimal GetMarginRemaining(SecurityPortfolioManager portfolio, Security security, OrderDirection direction)
        {
            var collateralCurrency = GetCollateralCash(security);
            var totalCollateralCurrency = collateralCurrency.Amount;
            var result = totalCollateralCurrency;

            foreach (var kvp in portfolio.Where(holdings => holdings.Value.Invested && holdings.Value.Type == SecurityType.CryptoFuture && holdings.Value.Symbol != security.Symbol))
            {
                var otherCryptoFuture = portfolio.Securities[kvp.Key];
                // check if we share the collateral
                if (collateralCurrency == GetCollateralCash(otherCryptoFuture))
                {
                    // we reduce the available collateral based on total usage of all other positions too
                    result -= otherCryptoFuture.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForCurrentHoldings(otherCryptoFuture));
                }
            }

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

            result -= totalCollateralCurrency * RequiredFreeBuyingPowerPercent;
            // convert into account currency
            result *= collateralCurrency.ConversionRate;
            return result < 0 ? 0 : result;
        }

        /// <summary>
        /// Helper method to determine what's the collateral currency for the given crypto future
        /// </summary>
        private static Cash GetCollateralCash(Security security)
        {
            var cryptoFuture = (CryptoFuture)security;

            var collateralCurrency = cryptoFuture.BaseCurrency;
            if (security.QuoteCurrency.Symbol == "USDT" || security.QuoteCurrency.Symbol == "BUSD")
            {
                collateralCurrency = cryptoFuture.QuoteCurrency;
            }

            return collateralCurrency;
        }
    }
}
