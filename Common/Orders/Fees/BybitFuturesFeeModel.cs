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

namespace QuantConnect.Orders.Fees;

/// <summary>
/// Bybit futures fee model implementation
/// </summary>
public class BybitFuturesFeeModel : BybitFeeModel
{
    /// <summary>
    /// Tier 1 maker fees
    /// https://learn.bybit.com/bybit-guide/bybit-trading-fees/
    /// </summary>
    public new const decimal MakerNonVIPFee = 0.0002m;

    /// <summary>
    /// Tier 1 taker fees
    /// https://learn.bybit.com/bybit-guide/bybit-trading-fees/
    /// </summary>
    public new const decimal TakerNonVIPFee = 0.00055m;

    /// <summary>
    /// Initializes a new instance of the <see cref="BybitFuturesFeeModel"/> class
    /// </summary>
    /// <param name="makerFee">The accounts maker fee</param>
    /// <param name="takerFee">The accounts taker fee</param>
    public BybitFuturesFeeModel(decimal makerFee = MakerNonVIPFee, decimal takerFee = TakerNonVIPFee)
        : base(makerFee, takerFee)
    {
    }
}
