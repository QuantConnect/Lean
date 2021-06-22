using System;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models Exante order fees.
    /// According to:
    /// <list type="bullet">
    ///   <item>https://support.exante.eu/hc/en-us/articles/115005873143-Fees-overview-exchange-imposed-fees?source=search</item>
    ///   <item>https://exante.eu/markets/</item>
    /// </list>
    /// </summary>
    public class ExanteFeeModel : FeeModel
    {
        private readonly decimal _forexCommissionRate;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="forexCommissionRate">Commission rate for FX operations</param>
        public ExanteFeeModel(decimal forexCommissionRate)
        {
            _forexCommissionRate = forexCommissionRate;
        }

        /// <inheritdoc />
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            decimal feeResult;
            string feeCurrency;
            switch (security.Type)
            {
                case SecurityType.Forex:
                    var totalOrderValue = order.GetValue(security);
                    feeResult = Math.Abs(_forexCommissionRate * totalOrderValue);
                    feeCurrency = Currencies.USD;
                    break;

                case SecurityType.Equity:
                    var equityFee = ComputeEquityFee("USA", order);
                    feeResult = equityFee.Amount;
                    feeCurrency = equityFee.Currency;
                    break;

                case SecurityType.Option:
                case SecurityType.IndexOption:
                    var optionsFee = ComputeOptionFee("USA", order);
                    feeResult = optionsFee.Amount;
                    feeCurrency = optionsFee.Currency;
                    break;

                case SecurityType.Future:
                case SecurityType.FutureOption:
                    feeResult = 1.5m;
                    feeCurrency = Currencies.USD;
                    break;

                default:
                    // unsupported security type
                    throw new ArgumentException(Invariant($"Unsupported security type: {security.Type}"));
            }

            return new OrderFee(new CashAmount(feeResult, feeCurrency));
        }

        /// <summary>
        /// Computes fee for equity order
        /// </summary>
        /// <param name="exchange">exchange of order</param>
        /// <param name="order">LEAN order</param>
        private static CashAmount ComputeEquityFee(string exchange, Order order)
        {
            switch (exchange)
            {
                case "USA":
                    return new CashAmount(order.AbsoluteQuantity * 0.02m, Currencies.USD);

                default:
                    var rate = 0.05m; // ToDo: clarify the value for different exchanges
                    return new CashAmount(order.AbsoluteQuantity * order.Price * rate, Currencies.USD);
            }
        }

        /// <summary>
        /// Computes fee for option order
        /// </summary>
        /// <param name="exchange">exchange of order</param>
        /// <param name="order">LEAN order</param>
        private static CashAmount ComputeOptionFee(string exchange, Order order)
        {
            switch (exchange)
            {
                case "USA":
                    return new CashAmount(order.AbsoluteQuantity * 1.5m, Currencies.USD);

                default:
                    // ToDo: clarify the value for different exchanges
                    throw new ArgumentException(Invariant($"Unsupported exchange: ${exchange}"));
            }
        }
    }
}
