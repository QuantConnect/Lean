using System;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    public class MarketOrderTestParameters : OrderTestParameters
    {
        public MarketOrderTestParameters(string symbol, SecurityType securityType)
            : base(symbol, securityType)
        {
        }

        public override Order CreateShortOrder(int quantity)
        {
            return new MarketOrder(Symbol, -Math.Abs(quantity), DateTime.Now, type: SecurityType);
        }

        public override Order CreateLongOrder(int quantity)
        {
            return new MarketOrder(Symbol, Math.Abs(quantity), DateTime.Now, type: SecurityType);
        }

        public override bool ModifyOrderToFill(Order order, decimal lastMarketPrice)
        {
            // NOP
            // market orders should fill without modification
            return false;
        }

        public override OrderStatus ExpectedStatus
        {
            // all market orders should fill
            get { return OrderStatus.Filled; }
        }
    }
}