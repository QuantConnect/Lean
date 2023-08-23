using System;
using System.Collections.Generic;
using QuantConnect.Benchmarks;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages;

/// <summary>
/// 
/// </summary>
public class BybitBrokerageModel : DefaultBrokerageModel
{
    /// <summary>
    /// Market name
    /// </summary>
    protected virtual string MarketName => Market.Bybit;

    /// <summary>
    /// Gets a map of the default markets to be used for each security type
    /// </summary>
    public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets(Market.Bybit);


    public override IFeeModel GetFeeModel(Security security) => new BybitFeeModel();


    public BybitBrokerageModel(AccountType accountType = AccountType.Cash) : base(accountType)
    {
    }

    /// <summary>
    /// Bybit global leverage rule
    /// </summary>
    /// <param name="security"></param>
    /// <returns></returns>
    public override decimal GetLeverage(Security security)
    {
        if (AccountType == AccountType.Cash || security.IsInternalFeed() || security.Type == SecurityType.Base)
        {
            return 1m;
        }

        return security.Symbol.SecurityType == SecurityType.CryptoFuture ? 10 : 10;
    }

    /// <summary>
    /// Get the benchmark for this model
    /// </summary>
    /// <param name="securities">SecurityService to create the security with if needed</param>
    /// <returns>The benchmark for this brokerage</returns>
    public override IBenchmark GetBenchmark(SecurityManager securities)
    {
        var symbol = Symbol.Create("BTCUSDC", SecurityType.Crypto, MarketName);
        return SecurityBenchmark.CreateInstance(securities, symbol);
    }


    public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request,
        out BrokerageMessageEvent message)
    {
        security = security ?? throw new ArgumentNullException(nameof(security));
        order = order ?? throw new ArgumentNullException(nameof(order));
        request = request ?? throw new ArgumentNullException(nameof(request));

        //can only update linear, inverse, and options
        if (security.Type != SecurityType.CryptoFuture)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
        }

        if (order.Status is not (OrderStatus.New or OrderStatus.PartiallyFilled))
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.OrderUpdateNotSupported);
            return false;
        }

        if (request.Quantity.HasValue && !IsOrderSizeLargeEnough(security, Math.Abs(request.Quantity.Value)))
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.InvalidOrderQuantity(security, request.Quantity.Value));
            return false;
        }
        
        message = null;
        return true;
    }

    protected static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets(string marketName)
    {
        var map = DefaultMarketMap.ToDictionary();
        map[SecurityType.Crypto] = marketName;
        return map.ToReadOnlyDictionary();
    }


    public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
    {
        security = security ?? throw new ArgumentNullException(nameof(security));
        order = order ?? throw new ArgumentNullException(nameof(order));

        if (security.Type != SecurityType.Crypto && security.Type != SecurityType.CryptoFuture)
        {
            message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));

            return false;
        }

        message = null;
        var quantityIsValid = true;

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

    protected virtual bool IsOrderSizeLargeEnough(Security security, decimal orderQuantity)
    {
        return !security.SymbolProperties.MinimumOrderSize.HasValue ||
               orderQuantity > security.SymbolProperties.MinimumOrderSize;
    }
}
