using System;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    public class LimitOrderTestParameters : OrderTestParameters
    {
        private readonly decimal _highLimit;
        private readonly decimal _lowLimit;

        public LimitOrderTestParameters(string symbol, SecurityType securityType, decimal highLimit, decimal lowLimit)
            : base(symbol, securityType)
        {
            _highLimit = highLimit;
            _lowLimit = lowLimit;
        }

        public override Order CreateShortOrder(int quantity)
        {
            return new LimitOrder(Symbol, -Math.Abs(quantity), _highLimit, DateTime.Now, type: SecurityType);
        }

        public override Order CreateLongOrder(int quantity)
        {
            return new LimitOrder(Symbol, Math.Abs(quantity), _lowLimit, DateTime.Now, type: SecurityType);
        }

        public override bool ModifyOrderToFill(Order order, decimal lastMarketPrice)
        {
            // limit orders will process even if they go beyond the market price

            var limit = (LimitOrder) order;
            if (order.Quantity > 0)
            {
                // for limit buys we need to increase the limit price
                limit.LimitPrice *= 2;
            }
            else
            {
                // for limit sells we need to decrease the limit price
                limit.LimitPrice /= 2;
            }
            return true;
        }

        public override OrderStatus ExpectedStatus
        {
            // default limit orders will only be submitted, not filled
            get { return OrderStatus.Submitted; }
        }
    }
}