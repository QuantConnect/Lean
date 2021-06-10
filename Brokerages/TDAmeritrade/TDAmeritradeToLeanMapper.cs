using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;
using TDAmeritradeApi.Client.Models;
using TDAmeritradeApi.Client.Models.AccountsAndTrading;
using AccountsAndTrading = TDAmeritradeApi.Client.Models.AccountsAndTrading;

namespace QuantConnect.Brokerages.TDAmeritrade
{
    public static class TDAmeritradeToLeanMapper
    {
        private static readonly TDAmeritradeSymbolMapper _symbolMapper = new TDAmeritradeSymbolMapper();

        public static Symbol GetSymbolFrom(Instrument instrument)
        {
            var securityType = GetSecurityType(instrument.assetType);

            return _symbolMapper.GetLeanSymbol(instrument.symbol, securityType, Market.USA.ToString());
        }

        /// <summary>
        /// Converts a Lean symbol instance to a TD Ameritrade symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The TD Ameritrade symbol</returns>
        public static string GetBrokerageSymbol(Symbol symbol)
        {
            return _symbolMapper.GetBrokerageSymbol(symbol);
        }

        internal static Symbol GetLeanSymbol(string symbol, SecurityType securityType)
        {
            return _symbolMapper.GetLeanSymbol(symbol, securityType, Market.USA);
        }

        /// <summary>
        /// Converts the specified tradier order into a qc order.
        /// The 'task' will have a value if we needed to issue a rest call for the stop price, otherwise it will be null
        /// </summary>
        public static Order ConvertOrder(OrderStrategy orderStrategy)
        {
            Order qcOrder;
            switch (orderStrategy.orderType)
            {
                case AccountsAndTrading.OrderType.LIMIT:
                    qcOrder = new LimitOrder { LimitPrice = orderStrategy.price };
                    break;
                case AccountsAndTrading.OrderType.MARKET:
                    qcOrder = new MarketOrder();
                    break;
                case AccountsAndTrading.OrderType.MARKET_ON_CLOSE:
                    qcOrder = new MarketOnCloseOrder();
                    break;
                case AccountsAndTrading.OrderType.STOP:
                    qcOrder = new StopMarketOrder { StopPrice = orderStrategy.stopPrice.Value };
                    break;
                case AccountsAndTrading.OrderType.STOP_LIMIT:
                    qcOrder = new StopLimitOrder { LimitPrice = orderStrategy.price, StopPrice = orderStrategy.stopPrice.Value };
                    break;
                //case AccountsAndTrading.OrderType.TRAILING_STOP:
                //    qcOrder = new TrailingStopOrder { LimitPrice = orderStrategy.price, StopPrice = orderStrategy.stopPrice.Value };
                //    break;
                //case AccountsAndTrading.OrderType.TRAILING_STOP_LIMIT:
                //    qcOrder = new TrailingStopLimitOrder { LimitPrice = orderStrategy.price, StopPrice = orderStrategy.stopPrice.Value };
                //    break;
                //case AccountsAndTrading.OrderType.NET_CREDIT:
                //case AccountsAndTrading.OrderType.NET_DEBIT:
                //case AccountsAndTrading.OrderType.NET_ZERO:
                case AccountsAndTrading.OrderType.EXERCISE:
                    qcOrder = new OptionExerciseOrder();
                    break;
                default:
                    throw new NotImplementedException($"The TD order type {orderStrategy.orderType} is not implemented.");
            }
            var orderLeg = orderStrategy.orderLegCollection[0];
            qcOrder.Symbol = GetSymbolFrom(orderLeg.instrument);

            qcOrder.Quantity = orderLeg.quantity;
            qcOrder.Status = ConvertStatus(orderStrategy.status.Value);
            qcOrder.BrokerId.Add(orderStrategy.orderId.ToStringInvariant());
            qcOrder.Properties.TimeInForce = ConvertTimeInForce(orderStrategy.duration);
            qcOrder.Id = (int)orderStrategy.orderId;

            qcOrder.Time = orderStrategy.enteredTime.Value;
            return qcOrder;
        }

        private static TimeInForce ConvertTimeInForce(OrderDurationType duration)
        {
            switch (duration)
            {
                case OrderDurationType.GOOD_TILL_CANCEL:
                    return TimeInForce.GoodTilCanceled;
                //case AccountsAndTrading.OrderDurationType.FILL_OR_KILL:
                //    break;
                default:
                    return TimeInForce.Day;
            }
        }

        private static OrderStatus ConvertStatus(OrderStrategyStatusType status)
        {
            switch (status)
            {
                case OrderStrategyStatusType.QUEUED:
                    return OrderStatus.Submitted;
                case OrderStrategyStatusType.PENDING_CANCEL:
                case OrderStrategyStatusType.AWAITING_UR_OUT:
                    return OrderStatus.CancelPending;
                case OrderStrategyStatusType.WORKING:
                    return OrderStatus.PartiallyFilled;
                case OrderStrategyStatusType.REJECTED:
                    return OrderStatus.Invalid;
                case OrderStrategyStatusType.EXPIRED:
                case OrderStrategyStatusType.CANCELED:
                    return OrderStatus.Canceled;
                case OrderStrategyStatusType.PENDING_ACTIVATION:
                case OrderStrategyStatusType.AWAITING_PARENT_ORDER:
                case OrderStrategyStatusType.AWAITING_CONDITION:
                case OrderStrategyStatusType.AWAITING_MANUAL_REVIEW:
                case OrderStrategyStatusType.PENDING_REPLACE:
                    return OrderStatus.New;
                case OrderStrategyStatusType.REPLACED:
                    return OrderStatus.UpdateSubmitted;
                case OrderStrategyStatusType.ACCEPTED:
                case OrderStrategyStatusType.FILLED:
                    return OrderStatus.Filled;
                default:
                    return OrderStatus.Submitted;
            }
        }

        private static SecurityType GetSecurityType(InstrumentAssetType assetType)
        {
            switch (assetType)
            {
                case InstrumentAssetType.EQUITY:
                case InstrumentAssetType.ETF:
                    return SecurityType.Equity;
                case InstrumentAssetType.OPTION:
                    return SecurityType.Option;
                case InstrumentAssetType.INDEX:
                    return SecurityType.Index;
                default:
                    throw new NotSupportedException($"{assetType} is not supported.");
            }
        }

        public static OrderStrategy ConvertToOrderStrategy(Order order, decimal holdingQuantity)
        {
            var instrumentAssetType = GetInstrumentAssetType(order.SecurityType);

            decimal? stopPrice = null;
            StopType? stopType = null;
            if (order is StopLimitOrder stopLimitOrder)
            {
                stopPrice = stopLimitOrder.StopPrice;
                stopType = StopType.MARK;
            }
            else if (order is StopMarketOrder stopMarketOrder)
            {
                stopPrice = stopMarketOrder.StopPrice;
                stopType = StopType.MARK;
            }

            return new OrderStrategy()
            {
                complexOrderStrategyType = ComplexOrderStrategyType.NONE, //Do not have brokerage create spread have QC do it.
                orderType = GetOrderType(order.Type),
                session = OrderStrategySessionType.NORMAL,
                price = order.Price,
                stopPrice = stopPrice,
                stopType = stopType,
                duration = GetDuration(order.TimeInForce),
                orderStrategyType = GetStrategyType(order),
                orderLegCollection = new OrderLeg[]
                {
                    new OrderLeg()
                    {
                        orderLegType = instrumentAssetType,
                        instruction = GetOrderInstruction(instrumentAssetType, order, holdingQuantity),
                        quantity = order.Quantity,
                        instrument = new Instrument()
                        {
                            symbol = _symbolMapper.GetBrokerageSymbol(order.Symbol),
                            assetType = instrumentAssetType
                        }
                    }
                }
            };
        }

        private static OrderInstructionType GetOrderInstruction(InstrumentAssetType instrumentAssetType, Order order, decimal holdingQuantity)
        {
            if (instrumentAssetType == InstrumentAssetType.OPTION)
            {
                if (order.Direction == OrderDirection.Buy)
                {
                    if (holdingQuantity >= 0)
                        return OrderInstructionType.BUY_TO_OPEN;
                    else
                        return OrderInstructionType.BUY_TO_CLOSE;
                }
                else
                {
                    if (holdingQuantity > 0)
                        return OrderInstructionType.SELL_TO_CLOSE;
                    else
                        return OrderInstructionType.SELL_TO_OPEN;
                }
            }
            else
            {

                if (order.Direction == OrderDirection.Buy)
                {
                    if (holdingQuantity >= 0)
                        return OrderInstructionType.BUY;
                    else
                        return OrderInstructionType.BUY_TO_COVER;
                }
                else
                {
                    if (holdingQuantity > 0)
                        return OrderInstructionType.SELL;
                    else
                        return OrderInstructionType.SELL_SHORT;
                }
            }
        }

        private static InstrumentAssetType GetInstrumentAssetType(SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Equity:
                    return InstrumentAssetType.EQUITY;
                case SecurityType.Option:
                case SecurityType.IndexOption:
                    return InstrumentAssetType.OPTION;
                case SecurityType.Index:
                    return InstrumentAssetType.INDEX;
                default:
                    throw new NotSupportedException($"{securityType} is not supported.");
            }
        }

        private static OrderStrategyType GetStrategyType(Order order)
        {
            if (order is StopLimitOrder stopLimitOrder)
            {
                if (stopLimitOrder.LimitPrice != 0 && stopLimitOrder.StopPrice != 0)
                    return OrderStrategyType.OCO;
            }

            return OrderStrategyType.SINGLE;
        }

        private static OrderDurationType GetDuration(TimeInForce timeInForce)
        {
            if (timeInForce is DayTimeInForce)
                return OrderDurationType.DAY;
            else
                return OrderDurationType.GOOD_TILL_CANCEL;
        }

        private static AccountsAndTrading.OrderType GetOrderType(Orders.OrderType type)
        {
            switch (type)
            {
                case Orders.OrderType.MarketOnOpen:
                case Orders.OrderType.Market:
                    return AccountsAndTrading.OrderType.MARKET;
                case Orders.OrderType.Limit:
                case Orders.OrderType.LimitIfTouched:
                    return AccountsAndTrading.OrderType.LIMIT;
                case Orders.OrderType.StopMarket:
                    return AccountsAndTrading.OrderType.STOP;
                case Orders.OrderType.StopLimit:
                    return AccountsAndTrading.OrderType.STOP_LIMIT;
                case Orders.OrderType.MarketOnClose:
                    return AccountsAndTrading.OrderType.MARKET_ON_CLOSE;
                case Orders.OrderType.OptionExercise:
                    return AccountsAndTrading.OrderType.EXERCISE;
                default:
                    throw new NotSupportedException($"{type} is not supported.");
            }
        }
    }
}
