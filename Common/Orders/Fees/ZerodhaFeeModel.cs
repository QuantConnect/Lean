using System;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides the default implementation of <see cref="IFeeModel"/>
    /// Refer to https://zerodha.com/brokerage-calculator
    /// </summary>
    public class ZerodhaFeeModel : IFeeModel
    {
        /// <summary>
        /// Gets the order fee associated with the specified order.
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        public OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if (parameters.Security == null)
            {
                return OrderFee.Zero;
            }
            var val = parameters.Order.GetValue(parameters.Security);

            var fee = GetFee(val);
            return new OrderFee(new CashAmount(fee, Currencies.INR));
        }

        private static decimal GetFee(decimal value)
        {
            bool isSell = value < 0;
            value = Math.Abs(value);
            var multiplied = value * 0.0003M;
            var brokerage = (multiplied > 20) ? 20 : Math.Round(multiplied, 2);

            var turnover = Math.Round(value, 2);

            decimal stt_total = 0;
            if (isSell)
            {
                stt_total = Math.Round(value * 0.00025M, 2);
            }

            var exc_trans_charge = Math.Round(turnover * 0.0000325M, 2);
            var cc = 0;


            var stax = Math.Round(0.18M * (brokerage + exc_trans_charge), 2);

            var sebi_charges = Math.Round((turnover * 0.000001M), 2);
            decimal stamp_charges = 0;
            if (!isSell)
            {
                stamp_charges = Math.Round(value * 0.00003M, 2);
            }

            var total_tax = Math.Round(brokerage + stt_total + exc_trans_charge + stamp_charges + cc + stax + sebi_charges, 2);

            return total_tax;
        }

    }
}
