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

using QuantConnect.Benchmarks;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;

namespace QuantConnect.Brokerages;

/// <summary>
/// Provides Bybit Inverse Futures specific properties.
/// Inverse (COIN-Margined) contracts are settled and collateralized in their base cryptocurrency (e.g. BTC for BTCUSD).
/// </summary>
public class BybitInverseFuturesBrokerageModel : BybitBrokerageModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BybitInverseFuturesBrokerageModel"/> class
    /// </summary>
    /// <param name="accountType">The type of account to be modeled, defaults to <see cref="AccountType.Margin"/></param>
    public BybitInverseFuturesBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
    {
    }

    /// <summary>
    /// Get the benchmark for this model
    /// </summary>
    /// <param name="securities">SecurityService to create the security with if needed</param>
    /// <returns>The benchmark for this brokerage</returns>
    public override IBenchmark GetBenchmark(SecurityManager securities)
    {
        var symbol = Symbol.Create("BTCUSD", SecurityType.CryptoFuture, MarketName);
        return SecurityBenchmark.CreateInstance(securities, symbol);
    }

    /// <summary>
    /// Provides Bybit Inverse Futures fee model
    /// </summary>
    /// <param name="security">The security to get a fee model for</param>
    /// <returns>The new fee model for this brokerage</returns>
    public override IFeeModel GetFeeModel(Security security)
    {
        return new BybitFuturesFeeModel();
    }

    /// <summary>
    /// Gets a new buying power model for the security
    /// </summary>
    /// <param name="security">The security to get a buying power model for</param>
    /// <returns>The buying power model for this brokerage/security</returns>
    public override IBuyingPowerModel GetBuyingPowerModel(Security security)
    {
        if (security.Type == SecurityType.CryptoFuture)
        {
            return new BybitInverseFuturesMarginModel(GetLeverage(security));
        }
        return base.GetBuyingPowerModel(security);
    }
}
