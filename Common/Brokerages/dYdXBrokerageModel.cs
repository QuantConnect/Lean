using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Util;

namespace QuantConnect.Brokerages;

public class dYdXBrokerageModel : DefaultBrokerageModel
{
    /// <summary>
    /// Market name
    /// </summary>
    protected virtual string MarketName => Market.dYdX;

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
            SecurityType.Base => base.GetFeeModel(security),
            _ => throw new ArgumentOutOfRangeException(nameof(security), security,
                $"Not supported security type {security.Type}")
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
        var symbol = Symbol.Create("BTCUSD", SecurityType.CryptoFuture, MarketName);
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
    /// Returns true if the order size is large enough for the given security.
    /// </summary>
    /// <param name="security">The security of the order</param>
    /// <param name="orderQuantity">The order quantity</param>
    /// <returns>True if the order size is large enough, false otherwise</returns>
    protected virtual bool IsOrderSizeLargeEnough(Security security, decimal orderQuantity)
    {
        return !security.SymbolProperties.MinimumOrderSize.HasValue ||
               orderQuantity > security.SymbolProperties.MinimumOrderSize;
    }

    private static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets(string marketName)
    {
        var map = DefaultMarketMap.ToDictionary();
        map[SecurityType.CryptoFuture] = marketName;
        return map.ToReadOnlyDictionary();
    }
}
