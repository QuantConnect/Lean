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
    /// Binance-specific crypto future margin model that includes supplementary stable coin
    /// currencies as alternative collateral for non-coin (USDⓈ-M) futures.
    /// </summary>
    /// <remarks>
    /// EU/EEA users under MiCA use Credits Trading Mode where BNFCR (pegged 1:1 to USD) replaces
    /// USDT/USDC for margin, PNL and fees. This model aggregates USD-pegged stable coins (BNFCR,
    /// USDC, FDUSD, etc.) as supplementary collateral. Volatile assets (BTC/ETH/BNB) are excluded
    /// because they require exchange-specific haircut rates.
    /// See: https://www.binance.com/en/support/faq/detail/0e857c392a2d47cebde0af762d9255ae
    /// </remarks>
    public class BinanceCryptoFutureMarginModel : CryptoFutureMarginModel
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="leverage">The leverage to use, default 25x</param>
        public BinanceCryptoFutureMarginModel(decimal leverage = 25)
            : base(leverage)
        {
        }

        /// <summary>
        /// Gets the total collateral amount for a Binance crypto future, including supplementary
        /// stable coin currencies that can serve as alternative collateral
        /// (e.g. BNFCR for USDT-quoted futures on Binance).
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The crypto future security</param>
        /// <param name="primaryCollateral">The primary collateral cash (e.g. USDT)</param>
        /// <returns>Total collateral amount in terms of the primary collateral currency</returns>
        protected override decimal GetTotalCollateralAmount(
            SecurityPortfolioManager portfolio, Security security, Cash primaryCollateral)
        {
            var total = primaryCollateral.Amount;
            var market = security.Symbol.ID.Market;
            var accountCurrency = portfolio.CashBook.AccountCurrency;
            foreach (var kvp in portfolio.CashBook)
            {
                var cash = kvp.Value;
                if (cash == primaryCollateral || cash.Amount <= 0)
                {
                    continue;
                }

                if (Currencies.IsStableCoinWithoutPair(accountCurrency, cash.Symbol, market))
                {
                    // convert supplementary collateral to primary collateral terms
                    total += portfolio.CashBook.Convert(cash.Amount, cash.Symbol, primaryCollateral.Symbol);
                }
            }

            return total;
        }
    }
}
