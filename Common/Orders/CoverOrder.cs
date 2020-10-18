using System;
using QuantConnect.Securities;

namespace QuantConnect.Orders
{
    public class CoverOrder : Order
    {
        public override OrderType Type
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Order Clone()
        {
            throw new NotImplementedException();
        }

        protected override decimal GetValueImpl(Security security)
        {
            throw new NotImplementedException();
        }
    }
}
