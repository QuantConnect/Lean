using System;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Slippage
{
    /// <summary>
    /// Approximates slippage given available data
    /// </summary>
    public class BitfinexSlippageModel : SpreadSlippageModel
    {

        /// <summary>
        /// Returns a decimal cash slippage approximation on the order.
        /// </summary>
        public override decimal GetSlippageApproximation(Security asset, Order order)
        {
            if (order.Price != 0)
            {
                return base.GetSlippageApproximation(asset, order);
            }
            
            var lastData = asset.GetLastData();
            var lastTick = lastData as Tick;

            // if we have tick data use the spread
            if (lastTick != null)
            {
                if (asset.Price != 0)
                {
                    if (order.Direction == OrderDirection.Buy)
                    {
                        //We're buying, assume slip to Asking Price.
                        return Math.Abs(asset.Price - lastTick.AskPrice);
                    }
                    if (order.Direction == OrderDirection.Sell)
                    {
                        //We're selling, assume slip to the bid price.
                        return Math.Abs(asset.Price - lastTick.BidPrice);
                    }
                }
                else
                {
                    return (lastTick.AskPrice - lastTick.BidPrice) / 2;                
                }
            }

            return 0;                
            
        }
    }
}