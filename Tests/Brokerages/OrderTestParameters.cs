using System;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// Helper class to abstract test cases from individual order types
    /// </summary>
    public abstract class OrderTestParameters
    {
        public string Symbol { get; private set; }
        public SecurityType SecurityType { get; private set; }

        protected OrderTestParameters(string symbol, SecurityType securityType)
        {
            Symbol = symbol;
            SecurityType = securityType;
        }

        public MarketOrder CreateLongMarketOrder(int quantity)
        {
            return new MarketOrder(Symbol, Math.Abs(quantity), DateTime.Now, type: SecurityType);
        }
        public MarketOrder CreateShortMarketOrder(int quantity)
        {
            return new MarketOrder(Symbol, -Math.Abs(quantity), DateTime.Now, type: SecurityType);
        }

        /// <summary>
        /// Creates a sell order of this type
        /// </summary>
        public abstract Order CreateShortOrder(int quantity);
        /// <summary>
        /// Creates a long order of this type
        /// </summary>
        public abstract Order CreateLongOrder(int quantity);
        /// <summary>
        /// Modifies the order so it is more likely to fill
        /// </summary>
        public abstract bool ModifyOrderToFill(Order order, decimal lastMarketPrice);
        /// <summary>
        /// The status to expect when submitting this order, typically just Submitted,
        /// unless market order, then Filled
        /// </summary>
        public abstract OrderStatus ExpectedStatus { get; }

        /// <summary>
        /// True to continue modifying the order until it is filled, false otherwise
        /// </summary>
        public virtual bool ModifyUntilFilled
        {
            get { return true; }
        }
    }
}