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
    /// Crypto Future holdings implementation of the base securities class
    /// </summary>
    /// <seealso cref="SecurityHolding"/>
    public class CryptoFutureHolding : SecurityHolding
    {
        /// <summary>
        /// Crypto Future Holding Class constructor
        /// </summary>
        /// <param name="security">The crypto future security being held</param>
        /// <param name="currencyConverter">A currency converter instance</param>
        public CryptoFutureHolding(Security security, ICurrencyConverter currencyConverter)
            : base(security, currencyConverter) { }

        /// <summary>
        /// Gets the total value of the specified <paramref name="quantity"/> of shares of this security
        /// in the account currency
        /// </summary>
        /// <param name="quantity">The quantity of shares</param>
        /// <param name="price">The current price</param>
        /// <returns>The value of the quantity of shares in the account currency</returns>
        public override ConvertibleCashAmount GetQuantityValue(decimal quantity, decimal price)
        {
            var cryptoFuture = (CryptoFuture)Security;

            Cash cash;
            decimal notionalPositionValue;
            // We could check quote currency or the contract multiplier being 1
            if (!cryptoFuture.IsCryptoCoinFuture())
            {
                // https://www.binance.com/en/support/faq/how-to-calculate-cost-required-to-open-a-position-in-perpetual-futures-contracts-87fa7ee33b574f7084d42bd2ce2e463b
                // example BTCUSDT: (9,253.30 * 1 BTC) = 9,253.3 USDT
                notionalPositionValue =
                    price * quantity * cryptoFuture.SymbolProperties.ContractMultiplier;

                // USDT is the QUOTE currency we will need to convert it into account currency
                cash = cryptoFuture.QuoteCurrency;
            }
            else
            {
                // https://www.binance.com/en/support/faq/leverage-and-margin-in-coin-margined-futures-contracts-be2c7d9d95b04a7e8044ed02dd7dfe5c
                // example BTCUSD: [ (10*100 USD) / 9,800 USD ] = 0.10204 BTC
                notionalPositionValue =
                    quantity * cryptoFuture.SymbolProperties.ContractMultiplier / price;

                // BTC is the BASE currency we will need to convert it into account currency
                cash = cryptoFuture.BaseCurrency;
            }

            return new ConvertibleCashAmount(notionalPositionValue, cash);
        }
    }
}
