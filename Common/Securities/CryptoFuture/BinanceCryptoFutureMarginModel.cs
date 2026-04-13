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
    /// EU/EEA users under MiCA Credits Trading Mode have BNFCR in their account.
    /// When BNFCR is present, this model aggregates all supplementary collateral assets
    /// using their walletBalance values converted to the primary collateral currency.
    /// Non-EU accounts don't have BNFCR — the check is a no-op for them.
    /// See: https://www.binance.com/en/support/faq/detail/0e857c392a2d47cebde0af762d9255ae
    /// </remarks>
    public class BinanceCryptoFutureMarginModel : CryptoFutureMarginModel
    {
        /// <summary>
        /// Binance Futures Credits currency symbol, present in EU/EEA accounts under MiCA Credits Trading Mode.
        /// </summary>
        private const string BNFCRCurrency = "BNFCR";

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
        /// collateral assets for EU/EEA accounts in MiCA Credits Trading Mode.
        /// For coin futures (e.g. BTCUSD), only the primary collateral (base currency) is used.
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The crypto future security</param>
        /// <param name="primaryCollateral">The primary collateral cash (e.g. USDT)</param>
        /// <returns>Total collateral amount in terms of the primary collateral currency</returns>
        protected override decimal GetTotalCollateralAmount(
            SecurityPortfolioManager portfolio, Security security, Cash primaryCollateral)
        {
            // Coin futures (e.g. BTCUSD) use only base currency as collateral
            var cryptoFuture = (CryptoFuture)security;
            if (cryptoFuture.IsCryptoCoinFuture())
            {
                return primaryCollateral.Amount;
            }

            // BNFCR presence means EU/EEA account in MiCA Credits Trading Mode.
            // Non-EU accounts don't have BNFCR in CashBook — skip entirely.
            var cashBook = portfolio.CashBook;
            if (!cashBook.ContainsKey(BNFCRCurrency))
            {
                return primaryCollateral.Amount;
            }

            // Aggregate all collateral assets using walletBalance values.
            // Binance controls which assets are in the account — we sum everything
            // with a non-zero balance, converting to the primary collateral currency.
            // Negative amounts (e.g. BNFCR fees) correctly reduce the total.
            var total = 0m;
            foreach (var kvp in cashBook)
            {
                var cash = kvp.Value;
                if (cash.Amount == 0)
                {
                    continue;
                }

                total += cashBook.Convert(cash.Amount, cash.Symbol, primaryCollateral.Symbol);
            }

            return total;
        }

        /// <summary>
        /// When BNFCR is present (EU/MiCA mode), all USDⓈ-M futures share the same collateral
        /// pool regardless of quote currency (USDT, USDC, etc.).
        /// </summary>
        protected override bool SharesCollateral(SecurityPortfolioManager portfolio, Cash collateralCurrency, Security otherCryptoFuture)
        {
            return portfolio.CashBook.ContainsKey(BNFCRCurrency) || base.SharesCollateral(portfolio, collateralCurrency, otherCryptoFuture);
        }
    }
}
