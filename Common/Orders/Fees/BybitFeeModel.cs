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

using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Orders.Fees;

/// <summary>
/// Bybit fee model implementation
/// </summary>
public class BybitFeeModel : FeeModel
{
    /// <summary>
    /// Tier 1 maker fees
    /// https://learn.bybit.com/bybit-guide/bybit-trading-fees/
    /// </summary>
    public const decimal MakerNonVIPFee = 0.001m;

    /// <summary>
    /// Tier 1 taker fees
    /// https://learn.bybit.com/bybit-guide/bybit-trading-fees/
    /// </summary>
    public const decimal TakerNonVIPFee = 0.001m;

    private readonly decimal _makerFee;
    private readonly decimal _takerFee;

    /// <summary>
    /// Creates Binance fee model setting fees values
    /// </summary>
    /// <param name="mFee">Maker fee value</param>
    /// <param name="tFee">Taker fee value</param>
    public BybitFeeModel(decimal mFee = MakerNonVIPFee, decimal tFee = TakerNonVIPFee)
    {
        _makerFee = mFee;
        _takerFee = tFee;
    }

    /// <summary>
    /// Gets the order fee associated with the specified order.
    /// </summary>
    /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
    /// containing the security and order</param>
    /// <returns>The cost of the order in a <see cref="CashAmount"/> instance</returns>
    public override OrderFee GetOrderFee(OrderFeeParameters parameters)
    {
        var security = parameters.Security;
        var order = parameters.Order;

        var fee = GetFee(order);

        if(security.Symbol.ID.SecurityType == SecurityType.CryptoFuture)
        {
            var positionValue = security.Holdings.GetQuantityValue(order.AbsoluteQuantity, security.Price);
            return new OrderFee(new CashAmount(positionValue.Amount * fee, positionValue.Cash.Symbol));
        }

        if (order.Direction == OrderDirection.Buy)
        {
            // fees taken in the received currency
            CurrencyPairUtil.DecomposeCurrencyPair(order.Symbol, out var baseCurrency, out _);
            return new OrderFee(new CashAmount(order.AbsoluteQuantity * fee, baseCurrency));
        }

        // get order value in quote currency
        var unitPrice = security.BidPrice;
        if (order.Type == OrderType.Limit)
        {
            // limit order posted to the order book
            unitPrice = ((LimitOrder)order).LimitPrice;
        }

        unitPrice *= security.SymbolProperties.ContractMultiplier;

        return new OrderFee(new CashAmount(
            unitPrice * order.AbsoluteQuantity * fee,
            security.QuoteCurrency.Symbol));
    }

    /// <summary>
    /// Gets the fee factor for the given order
    /// </summary>
    /// <param name="order">The order to get the fee factor for</param>
    /// <returns>The fee factor for the given order</returns>
    protected virtual decimal GetFee(Order order)
    {
        return GetFee(order, _makerFee, _takerFee);
    }

    private static decimal GetFee(Order order, decimal makerFee, decimal takerFee)
    {
        // apply fee factor, currently we do not model 30-day volume, so we use the first tier
        var fee = takerFee;
        var props = order.Properties as BybitOrderProperties;

        if (order.Type == OrderType.Limit && ((props != null && props.PostOnly) || !order.IsMarketable))
        {
            // limit order posted to the order book
            fee = makerFee;
        }

        return fee;
    }
}
