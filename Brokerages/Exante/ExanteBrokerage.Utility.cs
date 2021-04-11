using System;
using Exante.Net.Enums;
using Exante.Net.Objects;
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

        private Holding ConvertHolding(ExantePosition position)
        {
            var symbol = _symbolMapper.GetLeanSymbol(position.SymbolId);
            var holding = new Holding
            {
                Symbol = symbol,
                Quantity = position.Quantity,
                CurrencySymbol = Currencies.GetCurrencySymbol(position.Currency),
                Type = symbol.SecurityType
            };

            if (position.AveragePrice != null)
            {
                holding.AveragePrice = position.AveragePrice.Value;
            }

            if (position.PnL != null)
            {
                holding.UnrealizedPnL = position.PnL.Value;
            }

            if (position.Price != null)
            {
                holding.MarketPrice = position.Price.Value;
            }

            return holding;
        }
    }
}
