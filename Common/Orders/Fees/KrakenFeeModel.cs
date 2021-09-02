using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models Kraken order fees
    /// </summary>
    public class KrakenFeeModel : FeeModel
    {
        /// <summary>
        /// We don't use 30 day model, so using only tier1 fees.
        /// https://www.kraken.com/features/fee-schedule#kraken-pro
        /// </summary>
        public const decimal MakerTier1CryptoFee = 0.16m;
        
        /// <summary>
        /// We don't use 30 day model, so using only tier1 fees.
        /// https://www.kraken.com/features/fee-schedule#kraken-pro
        /// </summary>
        public const decimal TakerTier1CryptoFee = 0.26m;
        
        /// <summary>
        /// We don't use 30 day model, so using only tier1 fees.
        /// https://www.kraken.com/features/fee-schedule#stablecoin-fx-pairs
        /// </summary>
        public const decimal Tier1FxFee = 0.2m;

        /// <summary>
        /// Fiats and stablecoins list that have own fee.
        /// </summary>
        public readonly List<string> FxStablecoinList = new() {"CAD", "EUR", "GBP", "JPY", "USD", "USDT", "DAI", "USDC"};
 
        /// <summary>
        /// Get the fee for this order in USD
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order USD</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;
            var unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;

            if (order.Type == OrderType.Limit)
            {
                // limit order posted to the order book
                unitPrice = ((LimitOrder)order).LimitPrice;
            }

            var fee = TakerTier1CryptoFee;

            var props = order.Properties as KrakenOrderProperties;
            
            if (order.Type == OrderType.Limit &&
                (props?.Oflags?.Contains("post") == true || !order.IsMarketable))
            {
                // limit order posted to the order book
                fee = MakerTier1CryptoFee;
            }
            
            return new OrderFee(new CashAmount(
                unitPrice * order.AbsoluteQuantity * fee,
                security.QuoteCurrency.Symbol));
        }
    }
}
