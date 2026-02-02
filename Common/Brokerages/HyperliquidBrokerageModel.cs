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
 * Modifications for Hyperliquid DEX support
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

/// <summary>
/// Provides Hyperliquid DEX specific brokerage properties
/// </summary>
/// <remarks>
/// Hyperliquid is a decentralized perpetual futures exchange
/// - Only supports perpetual futures (CryptoFuture)
/// - All contracts settled in USDC
/// - Competitive maker/taker fees: 0.01% maker, 0.035% taker
/// - High leverage available (up to 50x)
/// </remarks>
public class HyperliquidBrokerageModel : DefaultBrokerageModel
{
    /// <summary>
    /// Market name
    /// </summary>
    protected virtual string MarketName => Market.Hyperliquid;

    /// <summary>
    /// Gets a map of the default markets to be used for each security type
    /// </summary>
    public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets(Market.Hyperliquid);

    /// <summary>
    /// Initializes a new instance of the <see cref="HyperliquidBrokerageModel"/> class
    /// </summary>
    /// <param name="accountType">The type of account to be modeled, defaults to <see cref="AccountType.Margin"/></param>
    public HyperliquidBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
    {
    }

    /// <summary>
    /// Gets the maximum leverage for Hyperliquid perpetual futures
    /// </summary>
    /// <param name="security">The security to get leverage for</param>
    /// <returns>Maximum leverage (50x for perpetuals)</returns>
    public override decimal GetLeverage(Security security)
    {
        if (AccountType == AccountType.Cash || security.IsInternalFeed() || security.Type == SecurityType.Base)
        {
            return 1m;
        }

        if (security.Type == SecurityType.CryptoFuture)
        {
            // Hyperliquid supports up to 50x leverage on perpetual futures
            // Note: actual max leverage varies by asset. Conservative default is 50x.
            return 50m;
        }

        return 1m;
    }

    /// <summary>
    /// Gets the fee model for Hyperliquid
    /// </summary>
    /// <param name="security">The security to get fee model for</param>
    /// <returns>The Hyperliquid fee model</returns>
    public override IFeeModel GetFeeModel(Security security)
    {
        return security.Type switch
        {
            SecurityType.CryptoFuture => new HyperliquidFeeModel(),
            SecurityType.Base => base.GetFeeModel(security),
            _ => throw new ArgumentOutOfRangeException(nameof(security), security,
                $"Hyperliquid only supports {SecurityType.CryptoFuture}, got {security.Type}")
        };
    }

    /// <summary>
    /// Gets the margin interest rate model for Hyperliquid
    /// </summary>
    /// <param name="security">The security to get margin interest rate model for</param>
    /// <returns>The margin interest rate model</returns>
    /// <remarks>
    /// Hyperliquid uses a funding rate mechanism for perpetual futures.
    /// This is handled separately from traditional margin interest.
    /// </remarks>
    public override IMarginInterestRateModel GetMarginInterestRateModel(Security security)
    {
        // Perpetual futures use funding rates, not traditional margin interest
        if (security.Type == SecurityType.CryptoFuture &&
            security.Symbol.ID.Date == SecurityIdentifier.DefaultDate)
        {
            // Return the null model which applies no interest
            // Funding rates are handled separately by the exchange
            return MarginInterestRateModel.Null;
        }

        return base.GetMarginInterestRateModel(security);
    }

    /// <summary>
    /// Gets the benchmark for Hyperliquid
    /// </summary>
    /// <param name="securities">SecurityService to create the security with if needed</param>
    /// <returns>The benchmark for this brokerage (BTCUSD perpetual)</returns>
    public override IBenchmark GetBenchmark(SecurityManager securities)
    {
        var symbol = Symbol.Create("BTCUSD", SecurityType.CryptoFuture, MarketName);
        return SecurityBenchmark.CreateInstance(securities, symbol);
    }

    /// <summary>
    /// Returns true if the brokerage can update this order
    /// </summary>
    /// <param name="security">The security of the order</param>
    /// <param name="order">The order to be updated</param>
    /// <param name="request">The requested update to be made to the order</param>
    /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
    /// <returns>True if the brokerage can update the order, false otherwise</returns>
    public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request,
        out BrokerageMessageEvent message)
    {
        // Hyperliquid only supports CryptoFuture order updates
        if (security.Type != SecurityType.CryptoFuture)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
            return false;
        }

        // Can only update open orders
        if (order.Status is not (OrderStatus.New or OrderStatus.PartiallyFilled or OrderStatus.Submitted or OrderStatus.UpdateSubmitted))
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                $"Order with status {order.Status} cannot be modified");
            return false;
        }

        // Validate new quantity if specified
        if (request.Quantity.HasValue && !IsOrderSizeLargeEnough(security, Math.Abs(request.Quantity.Value)))
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.InvalidOrderQuantity(security, request.Quantity.Value));
            return false;
        }

        message = null;
        return true;
    }

    /// <summary>
    /// Returns true if the brokerage can submit this order
    /// </summary>
    /// <param name="security">The security of the order</param>
    /// <param name="order">The order to be processed</param>
    /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
    /// <returns>True if the brokerage can process the order, false otherwise</returns>
    public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
    {
        // Hyperliquid only supports CryptoFuture
        if (security.Type != SecurityType.CryptoFuture && security.Type != SecurityType.Base)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));
            return false;
        }

        message = null;
        bool quantityIsValid;

        // Validate supported order types and quantity
        switch (order)
        {
            case StopLimitOrder:
            case StopMarketOrder:
            case LimitOrder:
            case MarketOrder:
                quantityIsValid = IsOrderSizeLargeEnough(security, Math.Abs(order.Quantity));
                break;
            default:
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order,
                        new[] { OrderType.StopMarket, OrderType.StopLimit, OrderType.Market, OrderType.Limit }));
                return false;
        }

        if (!quantityIsValid)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.InvalidOrderQuantity(security, order.Quantity));
            return false;
        }

        return base.CanSubmitOrder(security, order, out message);
    }

    /// <summary>
    /// Returns true if the order size is large enough for the given security
    /// </summary>
    /// <param name="security">The security of the order</param>
    /// <param name="orderQuantity">The order quantity</param>
    /// <returns>True if the order size is large enough, false otherwise</returns>
    protected virtual bool IsOrderSizeLargeEnough(Security security, decimal orderQuantity)
    {
        return !security.SymbolProperties.MinimumOrderSize.HasValue ||
               orderQuantity > security.SymbolProperties.MinimumOrderSize;
    }

    /// <summary>
    /// Gets the default markets for Hyperliquid
    /// </summary>
    private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets(string marketName)
    {
        var map = DefaultMarketMap.ToDictionary();
        map[SecurityType.CryptoFuture] = marketName;
        return map.ToReadOnlyDictionary();
    }
}
