using System;
using Exante.Net.Enums;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Exante
{
    public partial class ExanteBrokerage
    {
        private static OrderStatus ConvertOrderStatus(ExanteOrderStatus status)
        {
            switch (status)
            {
                case ExanteOrderStatus.Placing:
                    return OrderStatus.New;

                case ExanteOrderStatus.Pending:
                case ExanteOrderStatus.Working:
                    return OrderStatus.PartiallyFilled;

                case ExanteOrderStatus.Cancelled:
                    return OrderStatus.Canceled;

                case ExanteOrderStatus.Filled:
                    return OrderStatus.Filled;

                case ExanteOrderStatus.Rejected:
                    return OrderStatus.Invalid;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}
