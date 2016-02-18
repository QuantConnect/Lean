using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Orders.Fees
{
    public class BitfinexFeeModel : IFeeModel
    {
        public decimal GetOrderFee(Securities.Security security, Order order)
        {
            //todo: test maker fee 0.001m
            //todo: fee scaling with trade size
            decimal divisor = 0.002m;

            if (order.Type == OrderType.Limit && ((((LimitOrder)order).LimitPrice > security.Price && order.Direction == OrderDirection.Sell) ||
            (((LimitOrder)order).LimitPrice < security.Price && order.Direction == OrderDirection.Buy)))
            {
                divisor = 0.001m;
            }
            decimal fee = security.Price * (order.Quantity < 0 ? (order.Quantity * -1) : order.Quantity) * divisor;
            return fee;
        }
    }
}
