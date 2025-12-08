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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages;

public class dYdXBrokerageModel : DefaultBrokerageModel
{
    /// <summary>
    /// Gets a map of the default markets to be used for each security type
    /// </summary>
    public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets(Market.dYdX);

    /// <summary>
    /// Initializes a new instance of the <see cref="dYdXBrokerageModel"/> class
    /// </summary>
    /// <param name="accountType">The type of account to be modeled, defaults to <see cref="AccountType.Margin"/></param>
    public dYdXBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
    {
        if (accountType != AccountType.Margin)
        {
            throw new ArgumentException("dYdXBrokerageModel only supports margin accounts", nameof(accountType));
        }
    }

    /// <summary>
    /// Provides dYdX fee model
    /// </summary>
    /// <param name="security"></param>
    /// <returns></returns>
    public override IFeeModel GetFeeModel(Security security)
    {
        return security.Type switch
        {
            SecurityType.CryptoFuture => new dYdXFeeModel(),
            _ => base.GetFeeModel(security)
        };
    }

    /// <summary>
    /// Gets a new margin interest rate model for the security
    /// </summary>
    /// <param name="security">The security to get a margin interest rate model for</param>
    /// <returns>The margin interest rate model for this brokerage</returns>
    public override IMarginInterestRateModel GetMarginInterestRateModel(Security security)
    {
        // TODO: Implement dYdX margin interest rate model
        return MarginInterestRateModel.Null;
    }

    /// <summary>
    /// Get the benchmark for this model
    /// </summary>
    /// <param name="securities">SecurityService to create the security with if needed</param>
    /// <returns>The benchmark for this brokerage</returns>
    public override IBenchmark GetBenchmark(SecurityManager securities)
    {
        var symbol = Symbol.Create("BTCUSD", SecurityType.CryptoFuture, Market.dYdX);
        return SecurityBenchmark.CreateInstance(securities, symbol);
        //todo default conversion?
    }

    /// <summary>
    /// Returns true if the brokerage could accept this order update. This takes into account
    /// order type, security type, and order size limits. dYdX can only update inverse, linear, and option orders
    /// </summary>
    /// <param name="security">The security of the order</param>
    /// <param name="order">The order to be updated</param>
    /// <param name="request">The requested update to be made to the order</param>
    /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
    /// <returns>True if the brokerage could update the order, false otherwise</returns>
    public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request,
        out BrokerageMessageEvent message)
    {
        message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
            Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
        return false;
    }

    /// <summary>
    /// Returns true if the brokerage could accept this order. This takes into account
    /// order type, security type, and order size limits.
    /// </summary>
    /// <remarks>
    /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
    /// </remarks>
    /// <param name="security">The security of the order</param>
    /// <param name="order">The order to be processed</param>
    /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
    /// <returns>True if the brokerage could process the order, false otherwise</returns>
    public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
    {
        if (security.Type != SecurityType.CryptoFuture)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));

            return false;
        }

        message = null;
        bool quantityIsValid;

        switch (order)
        {
            case StopLimitOrder:
            case StopMarketOrder:
            case LimitOrder:
            case MarketOrder:
                quantityIsValid = IsValidOrderSize(security, Math.Abs(order.Quantity), out message);
                break;
            default:
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order,
                        [OrderType.StopMarket, OrderType.StopLimit, OrderType.Market, OrderType.Limit]));
                return false;
        }

        return quantityIsValid;
    }

    private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets(string marketName)
    {
        var map = DefaultMarketMap.ToDictionary();
        map[SecurityType.CryptoFuture] = marketName;
        return map.ToReadOnlyDictionary();
    }
}
