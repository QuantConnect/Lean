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
    /// Margin model for Bybit Inverse Futures using the Unified Trading Account (UTA).
    /// In UTA, <see cref="BybitBrokerage.GetCashBalance"/> reports <c>TotalAvailableBalance</c> as USD,
    /// so we use the quote currency (USD) as collateral instead of the base crypto (e.g. ADA).
    /// </summary>
    public class BybitInverseFuturesMarginModel : CryptoFutureMarginModel
    {
        public BybitInverseFuturesMarginModel(decimal leverage) : base(leverage) { }

        private protected override Cash GetCollateralCash(Security security)
        {
            return (security as CryptoFuture).QuoteCurrency;
        }
    }
}
