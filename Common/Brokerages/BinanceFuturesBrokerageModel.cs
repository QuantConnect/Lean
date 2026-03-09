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
using QuantConnect.Benchmarks;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Binance Futures specific properties
    /// </summary>
    public class BinanceFuturesBrokerageModel : BinanceBrokerageModel
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public BinanceFuturesBrokerageModel(AccountType accountType) : base(accountType)
        {
            if (accountType == AccountType.Cash)
            {
                throw new InvalidOperationException($"{SecurityType.CryptoFuture} can only be traded using a {AccountType.Margin} account type");
            }
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, MarketName);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <summary>
        /// Provides Binance Futures fee model
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new BinanceFuturesFeeModel();
        }

        /// <summary>
        /// Gets a new margin interest rate model for the security
        /// </summary>
        /// <param name="security">The security to get a margin interest rate model for</param>
        /// <returns>The margin interest rate model for this brokerage</returns>
        public override IMarginInterestRateModel GetMarginInterestRateModel(Security security)
        {
            // only applies for perpetual futures
            if (security.Symbol.SecurityType == SecurityType.CryptoFuture && security.Symbol.ID.Date == SecurityIdentifier.DefaultDate)
            {
                return new BinanceFutureMarginInterestRateModel();
            }
            return base.GetMarginInterestRateModel(security);
        }
    }
}
