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
    /// EU/EEA users under MiCA Credits Trading Mode use BNFCR (pegged 1:1 to USD) as the
    /// cross-margin accounting currency. Binance stores the total available collateral pool
    /// (all stablecoins combined, minus used margin) in BNFCR's availableBalance field.
    /// This model reads that value as supplementary collateral for USDⓈ-M futures.
    /// Non-EU users won't have BNFCR in their account — the check is a no-op for them.
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
        /// For coin futures (e.g. BTCUSD), only the primary collateral (base currency) is used.
        /// </summary>
        /// <param name="portfolio">The algorithm's portfolio</param>
        /// <param name="security">The crypto future security</param>
        /// <param name="primaryCollateral">The primary collateral cash (e.g. USDT)</param>
        /// <returns>Total collateral amount in terms of the primary collateral currency</returns>
        protected override decimal GetTotalCollateralAmount(
            SecurityPortfolioManager portfolio, Security security, Cash primaryCollateral)
        {
            var total = primaryCollateral.Amount;

            // Coin futures (e.g. BTCUSD) use only base currency as collateral - stable coins don't apply
            var cryptoFuture = (CryptoFuture)security;
            if (cryptoFuture.IsCryptoCoinFuture())
            {
                return total;
            }

            // BNFCR is available only for EU/EEA accounts (MiCA Credits Trading Mode).
            // Its CashBook amount reflects availableBalance from the Binance API — the total
            // cross-margin available balance across all stablecoins, already aggregated by Binance.
            // Non-EU users won't have BNFCR in their CashBook, so TryGetValue returns false.
            var cashBook = portfolio.CashBook;
            if (cashBook.TryGetValue("BNFCR", out var bnfcrCash) && bnfcrCash.Amount > 0)
            {
                total += cashBook.Convert(bnfcrCash.Amount, "BNFCR", primaryCollateral.Symbol);
            }

            return total;
        }
    }
}
