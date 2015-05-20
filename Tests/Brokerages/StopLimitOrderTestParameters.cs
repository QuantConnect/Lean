using System;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    public class StopLimitOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;

        public StopLimitOrderTestParameters(string symbol, SecurityType securityType, decimal highLimit, decimal lowLimit)
            : base(symbol, securityType)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
        }

        public override Order CreateShortOrder(int quantity)
        {
            return new StopLimitOrder(Symbol, -Math.Abs(quantity), _lowLimit, _highLimit, DateTime.Now, type: SecurityType);
        }

        public override Order CreateLongOrder(int quantity)
        {
            return new StopLimitOrder(Symbol, Math.Abs(quantity), _highLimit, _lowLimit, DateTime.Now, type: SecurityType);
        }

        public override bool ModifyOrderToFill(Order order, decimal lastMarketPrice)
        {
            var stop = (StopLimitOrder) order;
            var previousStop = stop.StopPrice;
            if (order.Quantity > 0)
            {
                // for stop buys we need to decrease the stop price
                stop.StopPrice = Math.Min(stop.StopPrice, Math.Max(stop.StopPrice/2, Math.Round(lastMarketPrice, 2, MidpointRounding.AwayFromZero)));
            }
            else
            {
                // for stop sells we need to increase the stop price
                stop.StopPrice = Math.Max(stop.StopPrice, Math.Min(stop.StopPrice * 2, Math.Round(lastMarketPrice, 2, MidpointRounding.AwayFromZero)));
            }
            stop.LimitPrice = stop.StopPrice;
            return stop.StopPrice != previousStop;
        }

        public override OrderStatus ExpectedStatus
        {
            // default limit orders will only be submitted, not filled
            get { return OrderStatus.Submitted; }
        }
    }
}