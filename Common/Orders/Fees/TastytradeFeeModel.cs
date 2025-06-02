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
 *
*/

using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees;

/// <summary>
/// Represents a fee model specific to Tastytrade.
/// </summary>
/// <see href="https://tastytrade.com/pricing/"/>
public class TastytradeFeeModel : FeeModel
{
    /// <summary>
    /// Represents the fee associated with equity options transactions (per contract).
    /// </summary>
    private const decimal _optionFee = 1m;

    /// <summary>
    /// The fee associated with futures transactions (per contract).
    /// </summary>
    private const decimal _futureFee = 1.25m;

    /// <summary>
    /// The fee associated with futures options transactions (per contract).
    /// </summary>
    private const decimal _futureOptionFee = 2.5m;

    /// <summary>
    /// Gets the order fee for a given security and order.
    /// </summary>
    /// <param name="parameters">The parameters including the security and order details.</param>
    /// <returns>
    /// A <see cref="OrderFee"/> instance representing the total fee for the order,
    /// or <see cref="OrderFee.Zero"/> if no fee is applicable.
    /// </returns>
    public override OrderFee GetOrderFee(OrderFeeParameters parameters)
    {
        if (parameters.Security.Type.IsOption())
        {
            var feeRate = parameters.Security.Type switch
            {
                SecurityType.IndexOption or SecurityType.Option => _optionFee,
                SecurityType.Future => _futureFee,
                SecurityType.FutureOption => _futureOptionFee,
                _ => 0m
            };
            return new OrderFee(new CashAmount(parameters.Order.AbsoluteQuantity * feeRate, Currencies.USD));
        }
        return OrderFee.Zero;
    }
}
