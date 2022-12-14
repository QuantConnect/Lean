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
        /// <param name="leverage">The leverage to use, used on initial margin requirements</param>
        /// <param name="maintenanceMarginRate">The maintenance margin rate</param>
        /// <param name="maintenanceAmount">The maintenance amount which will reduce maintenance margin requirements</param>
        public CryptoFutureMarginModel(decimal leverage, decimal maintenanceMarginRate = 0.1m, decimal maintenanceAmount = 0)
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

            return new MaintenanceMargin(security.Holdings.GetQuantityValue(quantity, security.Price).InAccountCurrency * _maintenanceMarginRate - _maintenanceAmount);
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

            return new InitialMargin(security.Holdings.GetQuantityValue(quantity, security.Price).InAccountCurrency / GetLeverage(security));
        }
    }
}
