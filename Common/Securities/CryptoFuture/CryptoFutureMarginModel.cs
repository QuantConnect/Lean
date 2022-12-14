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
    /// 
    /// </summary>
    public class CryptoFutureMarginModel : SecurityMarginModel
    {
        private readonly decimal _maintenanceMarginRate;
        private readonly decimal _maintenanceAmount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leverage"></param>
        /// <param name="maintenanceMarginRate"></param>
        /// <param name="maintenanceAmount"></param>
        public CryptoFutureMarginModel(decimal leverage, decimal maintenanceMarginRate, decimal maintenanceAmount) :
            base(leverage, 0)
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

            return new MaintenanceMargin(GetNotionalPositionValue(security, quantity) * _maintenanceMarginRate - _maintenanceAmount);
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

            return new InitialMargin(GetNotionalPositionValue(security, quantity) / GetLeverage(security));
        }

        private static decimal GetNotionalPositionValue(Security security, decimal quantity)
        {
            var cryptoFuture = (CryptoFuture)security;

            // We could check base currency or the contract multiplier being 1
            if(cryptoFuture.QuoteCurrency.Symbol == "USDT" || cryptoFuture.QuoteCurrency.Symbol == "BUSD")
            {
                // https://www.binance.com/en/support/faq/how-to-calculate-cost-required-to-open-a-position-in-perpetual-futures-contracts-87fa7ee33b574f7084d42bd2ce2e463b
                // example BTCUSDT: (9,253.30 * 1 BTC) = 9,253.3 USDT
                var notionalPositionValue = quantity * security.SymbolProperties.ContractMultiplier * security.Price;

                // USDT is the QUOTE currency we need to convert it into account currency
                return notionalPositionValue * security.QuoteCurrency.ConversionRate;
            }
            else
            {
                // https://www.binance.com/en/support/faq/leverage-and-margin-in-coin-margined-futures-contracts-be2c7d9d95b04a7e8044ed02dd7dfe5c
                // example BTCUSD: [ (10*100 USD) / 9,800 USD ] = 0.10204 BTC
                var notionalPositionValue = quantity * security.SymbolProperties.ContractMultiplier / security.Price;

                // BTC is the BASE currency we need to convert it into account currency
                return notionalPositionValue * cryptoFuture.BaseCurrency.ConversionRate;
            }
        }
    }
}
