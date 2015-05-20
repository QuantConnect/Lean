using System;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    public class StopMarketOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;

        public StopMarketOrderTestParameters(string symbol, SecurityType securityType, decimal highLimit, decimal lowLimit)
            : base(symbol, securityType)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
        }

        public override Order CreateShortOrder(int quantity)
        {
            return new StopMarketOrder(Symbol, -Math.Abs(quantity), _lowLimit, DateTime.Now, type: SecurityType);
        }

        public override Order CreateLongOrder(int quantity)
        {
            return new StopMarketOrder(Symbol, Math.Abs(quantity), _highLimit, DateTime.Now, type: SecurityType);
        }

        public override bool ModifyOrderToFill(Order order, decimal lastMarketPrice)
        {
            var stop = (StopMarketOrder)order;
            var previousStop = stop.StopPrice;
            if (order.Quantity > 0)
            {
                // for stop buys we need to decrease the stop price
                stop.StopPrice = Math.Min(stop.StopPrice, Math.Max(stop.StopPrice / 2, lastMarketPrice));
            }
            else
            {
                // for stop sells we need to increase the stop price
                stop.StopPrice = Math.Max(stop.StopPrice, Math.Min(stop.StopPrice * 2, lastMarketPrice));
            }
            return stop.StopPrice != previousStop;
        }

        public override OrderStatus ExpectedStatus
        {
            // default limit orders will only be submitted, not filled
            get { return OrderStatus.Submitted; }
        }
    }
}