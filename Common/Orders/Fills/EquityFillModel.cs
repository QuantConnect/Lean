/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Implements <see cref="IFillModel"/> for Equity
    /// </summary>
    public class EquityFillModel : FillModel
    {
        /// <summary>
        /// Market fill model for the Equity. Fills at the last bid or ask price.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            // Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled ||            // Orders with canceled status or hold
                order.Direction == OrderDirection.Hold ||          // direction don't need anymore checks
                !IsExchangeOpen(asset, false))   // Exchange need to be open/normal market hours
            {
                return fill;
            }

            // Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            // Define the last bid or ask time to set stale prices message
            var endTime = DateTime.MinValue;

            // Apply slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //Order [fill]price for a buy market order model is the current security ask price
                    fill.FillPrice = GetAskPrice(asset, out endTime) + slip;
                    break;
                case OrderDirection.Sell:
                    //Order [fill]price for a buy market order model is the current security bid price
                    fill.FillPrice = GetBidPrice(asset, out endTime) - slip;
                    break;
            }

            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);

            // If the order is filled on stale (fill-forward) data, set a warning message on the order event
            if (endTime.Add(Parameters.StalePriceTimeSpan) < localOrderTime)
            {
                fill.Message = $"Warning: fill at stale price ({endTime.ToStringInvariant()} {asset.Exchange.TimeZone})";
            }

            // Assume the order is completely filled
            fill.FillQuantity = order.Quantity;
            fill.Status = OrderStatus.Filled;

            return fill;
        }

        /// <summary>
        /// Stop fill model implementation for Equity.
        /// The order is triggered if and when the user-specified stop trigger price is attained or penetrated by trade prices.
        /// Assumes the worse case scenario fill price:
        ///    Buy: highest of the stop trigger price and last ask price
        ///    Sell: lowest of the stop trigger price and last bid price
        /// We model the security price with its trade bar High and Low to account intrabar prices.
        /// https://www1.interactivebrokers.com/en/index.php?f=609
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        public override OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
        {
            // Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled ||            // Orders with canceled status or hold
                order.Direction == OrderDirection.Hold ||          // direction don't need anymore checks
                !IsExchangeOpen(asset, false))   // Exchange need to be open/normal market hours
            {
                return fill;
            }

            // Get the last trade bar since stop orders are triggered by trades
            var lastTradeBar = GetLastTradeBar(asset);
            var endTime = lastTradeBar.EndTime;

            // Do not fill on stale data
            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            if (endTime <= localOrderTime) return fill;

            // Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            // Check if the Stop Order was filled: opposite to a limit order
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    // Buy Stop: If Price Above set point, Buy:
                    if (lastTradeBar.High >= order.StopPrice)
                    {
                        // Assuming worse case scenario fill - fill at highest of the stop & asset ask price.
                        fill.FillPrice = Math.Max(order.StopPrice, GetAskPrice(asset, out endTime) + slip);
                        fill.Status = OrderStatus.Filled;
                    }
                    break;

                case OrderDirection.Sell:
                    // Sell Stop: If Price below set point, Sell:
                    if (lastTradeBar.Low <= order.StopPrice)
                    {
                        // Assuming worse case scenario fill - fill at lowest of the stop & asset bid price.
                        fill.FillPrice = Math.Min(order.StopPrice, GetBidPrice(asset, out endTime) - slip);
                        fill.Status = OrderStatus.Filled;
                    }
                    break;
            }

            if (fill.Status == OrderStatus.Filled)
            {
                // Assume the order is completely filled
                fill.FillQuantity = order.Quantity;
            }

            return fill;
        }

        /// <summary>
        /// Stop-Limit fill model implementation for Equity.
        /// The order is triggered if and when the user-specified stop trigger price is attained or penetrated by trade prices,
        /// and filled when the user-specified limit trigger price is attained or penetrated also by ask and bid prices
        /// Assumes the worse case scenario fill price:
        ///    Buy: highest of the stop trigger price and last trade bar high
        ///    Sell: lowest of the stop trigger price and last trade bar low
        /// We model the security price with its trade bar High and Low to account intrabar prices.
        /// https://www.interactivebrokers.com/en/index.php?f=608
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <remarks>
        ///     There is no good way to model limit orders with OHLC because we never know whether the market has
        ///     gapped past our fill price. We have to make the assumption of a fluid, high volume market.
        ///
        ///     Stop limit orders we also can't be sure of the order of the H - L values for the limit fill. The assumption
        ///     was made the limit fill will be done with closing price of the bar after the stop has been triggered..
        /// </remarks>
        public override OrderEvent StopLimitFill(Security asset, StopLimitOrder order)
        {
            // Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            // Orders with canceled status or hold direction don't need anymore checks
            if (order.Status == OrderStatus.Canceled || order.Direction == OrderDirection.Hold)
            {
                return fill;
            }

            // Make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(
                asset,
                Parameters.ConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }

            // Get the last trade bar since stop limit orders are triggered by trades
            var lastTradeBar = GetLastTradeBar(asset);

            // Do not fill on stale data
            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            if (lastTradeBar.EndTime <= localOrderTime) return fill;

            // Check if the Stop Order was filled: opposite to a limit order
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //-> 1.1 Buy Stop: If Price Above set point, Buy:
                    if (lastTradeBar.High >= order.StopPrice || order.StopTriggered)
                    {
                        order.StopTriggered = true;

                        // Fill the limit order, using closing price of bar:
                        // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                        if (asset.Price <= order.LimitPrice)
                        {
                            fill.FillPrice = Math.Min(lastTradeBar.High, order.LimitPrice);
                            fill.Status = OrderStatus.Filled;
                        }
                    }
                    break;

                case OrderDirection.Sell:
                    //-> 1.2 Sell Stop: If Price below set point, Sell:
                    if (lastTradeBar.Low <= order.StopPrice || order.StopTriggered)
                    {
                        order.StopTriggered = true;

                        // Fill the limit order, using minimum price of the bar
                        // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                        if (asset.Price >= order.LimitPrice)
                        {
                            fill.FillPrice = Math.Max(lastTradeBar.Low, order.LimitPrice);
                            fill.Status = OrderStatus.Filled;
                        }
                    }
                    break;
            }

            if (fill.Status == OrderStatus.Filled)
            {
                // Assume the order is completely filled
                fill.FillQuantity = order.Quantity;
            }

            return fill;
        }

        /// <summary>
        /// Limit fill model implementation for Equity.
        /// The order is filled if and when the user-specified limit trigger price is attained or penetrate by the bid and ask prices
        /// Assumes the worse case scenario fill price:
        /// We model the security price with its bid High and ask Low to account intrabar prices.
        /// The order is filled when the security price reaches the limit price and the worse price is assumed.
        /// https://www.interactivebrokers.com/en/index.php?f=593
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        public override OrderEvent LimitFill(Security asset, LimitOrder order)
        {
            // Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            // Orders with canceled status or hold direction don't need anymore checks
            if (order.Status == OrderStatus.Canceled || order.Direction == OrderDirection.Hold)
            {
                return fill;
            }

            // Make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(
                asset,
                Parameters.ConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }

            // Get the last quote bar since limit orders are triggered by quote bid and ask high and low
            var lastQuoteBar = GetLastQuoteBar(asset);

            // Do not fill on stale data
            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            if (lastQuoteBar.EndTime <= localOrderTime) return fill;

            //-> Valid Live/Model Order:
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //Buy limit seeks lowest price
                    if (lastQuoteBar.Ask.Low <= order.LimitPrice)
                    {
                        // Fill at the worse price this bar or the limit price,
                        // this allows far out of the money limits to be executed properly
                        fill.FillPrice = Math.Min(lastQuoteBar.Ask.High, order.LimitPrice);
                        fill.Status = OrderStatus.Filled;
                    }
                    break;
                case OrderDirection.Sell:
                    //Sell limit seeks highest price possible
                    if (lastQuoteBar.Bid.High >= order.LimitPrice)
                    {
                        // Fill at the worse price this bar or the limit price,
                        // this allows far out of the money limits to be executed properly
                        fill.FillPrice = Math.Max(lastQuoteBar.Bid.Low, order.LimitPrice);
                        fill.Status = OrderStatus.Filled;
                    }
                    break;
            }

            if (fill.Status == OrderStatus.Filled)
            {
                // Assume the order is completely filled
                fill.FillQuantity = order.Quantity;
            }

            return fill;
        }

        /// <summary>
        /// Market on Open fill model for the Equity. Fills with the opening price of the trading session.
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order)
        {
            // Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            // Orders with canceled status or hold direction don't need anymore checks
            if (order.Status == OrderStatus.Canceled || order.Direction == OrderDirection.Hold)
            {
                return fill;
            }

            // Get the last trade bar since MOO are filled at (trade bar) opening price
            var lastTradeBar = GetLastTradeBar(asset);

            // MOO should never fill on the same bar or on stale data
            // Imagine the case where we have a thinly traded equity, ASUR, and another liquid
            // equity, say SPY, SPY gets data every minute but ASUR, if not on fill forward, maybe
            // have large gaps, in which case the currentBar.EndTime will be in the past
            // ASUR  | | |      [order]        | | | | | | |
            //  SPY  | | | | | | | | | | | | | | | | | | | |
            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);

            // Do not fill on stale data
            if (lastTradeBar.EndTime <= localOrderTime) return fill;

            // If the MOO was submitted during market the previous day, wait for a day to turn over
            if (asset.Exchange.DateTimeIsOpen(localOrderTime) && localOrderTime.Date == asset.LocalTime.Date)
            {
                return fill;
            }

            // Wait until market open and
            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            // Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            // Apply slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    fill.FillPrice = lastTradeBar.Open + slip;
                    break;
                case OrderDirection.Sell:
                    fill.FillPrice = lastTradeBar.Open - slip;
                    break;
            }

            // Assume the order is completely filled
            fill.FillQuantity = order.Quantity;
            fill.Status = OrderStatus.Filled;

            return fill;
        }

        /// <summary>
        /// Market on Close fill model for the Equity. Fills with the closing price of the trading session.
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order)
        {
            // Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            // Orders with canceled status or hold direction don't need anymore checks
            if (order.Status == OrderStatus.Canceled || order.Direction == OrderDirection.Hold)
            {
                return fill;
            }

            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            var nextMarketClose = asset.Exchange.Hours.GetNextMarketClose(localOrderTime, false);

            // Wait until market closes after the order time and
            // make sure the exchange is open/normal market hours before filling
            if (asset.LocalTime < nextMarketClose || !IsExchangeOpen(asset, false))
            {
                return fill;
            }

            // Get the last trade bar since MOC are filled at (trade bar) closing price
            var lastTradeBar = GetLastTradeBar(asset);

            // Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            // Apply slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    fill.FillPrice = lastTradeBar.Close + slip;
                    break;
                case OrderDirection.Sell:
                    fill.FillPrice = lastTradeBar.Close - slip;
                    break;
            }

            // Assume the order is completely filled
            fill.FillQuantity = order.Quantity;
            fill.Status = OrderStatus.Filled;

            return fill;
        }

        /// <summary>
        /// Get data types the Security is subscribed to
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        private HashSet<Type> GetSubscribedTypes(Security asset)
        {
            var subscribedTypes = Parameters
                .ConfigProvider
                .GetSubscriptionDataConfigs(asset.Symbol)
                .ToHashSet(x => x.Type);

            if (subscribedTypes.Count == 0)
            {
                throw new InvalidOperationException($"Cannot perform fill for {asset.Symbol} because no data subscription were found.");
            }

            return subscribedTypes;
        }

        /// <summary>
        /// Get current ask price for subscribed data
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        /// <param name="endTime">Timestamp of the most recent data type</param>
        private decimal GetAskPrice(Security asset, out DateTime endTime)
        {
            var subscribedTypes = GetSubscribedTypes(asset);

            var ticks = new List<Tick>();
            var isTickSubscribed = subscribedTypes.Contains(typeof(Tick));

            if (isTickSubscribed)
            {
                ticks = asset.Cache.GetAll<Tick>().ToList();

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.AskPrice != 0);
                if (quote != null)
                {
                    endTime = quote.EndTime;
                    return quote.AskPrice;
                }
            }

            if (subscribedTypes.Contains(typeof(QuoteBar)))
            {
                var quoteBar = asset.Cache.GetData<QuoteBar>();
                if (quoteBar != null)
                {
                    endTime = quoteBar.EndTime;
                    return quoteBar.Ask?.Close ?? quoteBar.Close;
                }
            }

            if (isTickSubscribed)
            {
                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price != 0);
                if (trade != null)
                {
                    endTime = trade.EndTime;
                    return trade.Price;
                }
            }

            var tradeBar = asset.Cache.GetData<TradeBar>();
            if (tradeBar != null)
            {
                endTime = tradeBar.EndTime;
                return tradeBar.Close;
            }

            endTime = asset.LocalTime;
            return asset.AskPrice;
        }

        /// <summary>
        /// Get current bid price for subscribed data
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        /// <param name="endTime">Timestamp of the most recent data type</param>
        private decimal GetBidPrice(Security asset, out DateTime endTime)
        {
            var subscribedTypes = GetSubscribedTypes(asset);

            var ticks = new List<Tick>();
            var isTickSubscribed = subscribedTypes.Contains(typeof(Tick));

            if (isTickSubscribed)
            {
                ticks = asset.Cache.GetAll<Tick>().ToList();

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.BidPrice != 0);
                if (quote != null)
                {
                    endTime = quote.EndTime;
                    return quote.BidPrice;
                }
            }

            if (subscribedTypes.Contains(typeof(QuoteBar)))
            {
                var quoteBar = asset.Cache.GetData<QuoteBar>();
                if (quoteBar != null)
                {
                    endTime = quoteBar.EndTime;
                    return quoteBar.Bid?.Close ?? quoteBar.Close;
                }
            }

            if (isTickSubscribed)
            {
                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.BidPrice != 0);
                if (trade != null)
                {
                    endTime = trade.EndTime;
                    return trade.Price;
                }
            }

            var tradeBar = asset.Cache.GetData<TradeBar>();
            if (tradeBar != null)
            {
                endTime = tradeBar.EndTime;
                return tradeBar.Close;
            }

            endTime = asset.LocalTime;
            return asset.BidPrice;
        }

        /// <summary>
        /// Get current trade bar for subscribed data
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        private TradeBar GetLastTradeBar(Security asset)
        {
            var subscribedTypes = GetSubscribedTypes(asset);

            if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null)
                {
                    return tradeBar;
                }
            }

            if (subscribedTypes.Contains(typeof(Tick)))
            {
                var ticks = asset.Cache.GetAll<Tick>().ToList();

                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price != 0);
                if (trade != null)
                {
                    return new TradeBar(trade.Time, trade.Symbol, trade.Price, trade.Price, trade.Price, trade.Price, trade.Quantity, TimeSpan.Zero);
                }

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.AskPrice != 0 && x.BidPrice != 0);
                if (quote != null)
                {
                    var price = (quote.AskPrice + quote.BidPrice) / 2;
                    return new TradeBar(quote.Time, quote.Symbol, price, price, price, price, 0, TimeSpan.Zero);
                }

            }

            var lastData = asset.GetLastData();
            return new TradeBar(lastData.Time, asset.Symbol, asset.Open, asset.High, asset.Low, asset.Close, asset.Volume, lastData.EndTime - lastData.Time);
        }

        /// <summary>
        /// Get current quote bar for subscribed data
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        private QuoteBar GetLastQuoteBar(Security asset)
        {
            var subscribedTypes = GetSubscribedTypes(asset);

            if (subscribedTypes.Contains(typeof(QuoteBar)))
            {
                var quoteBar = asset.Cache.GetData<QuoteBar>();
                if (quoteBar != null)
                {
                    return quoteBar;
                }
            }

            if (subscribedTypes.Contains(typeof(Tick)))
            {
                var ticks = asset.Cache.GetAll<Tick>().ToList();

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.AskPrice != 0 && x.BidPrice != 0);
                if (quote != null)
                {
                    return new QuoteBar(
                        quote.Time,
                        quote.Symbol,
                        new Bar(quote.BidPrice, quote.BidPrice, quote.BidPrice, quote.BidPrice),
                        quote.BidSize,
                        new Bar(quote.AskPrice, quote.AskPrice, quote.AskPrice, quote.AskPrice),
                        quote.AskSize,
                        TimeSpan.Zero
                    );
                }

                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade && x.Price != 0);
                if (trade != null)
                {
                    var bar = new Bar(trade.Price, trade.Price, trade.Price, trade.Price);
                    return new QuoteBar(trade.Time, trade.Symbol, bar, 0, bar, 0, TimeSpan.Zero);
                }
            }

            var lastData = asset.GetLastData();

            return new QuoteBar(
                lastData.Time,
                asset.Symbol,
                new Bar(asset.Open, asset.High, asset.Low, asset.BidPrice),
                asset.BidSize,
                new Bar(asset.Open, asset.High, asset.Low, asset.AskPrice),
                asset.AskSize,
                lastData.EndTime - lastData.Time
            );
        }
    }
}