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

using System;
using System.Collections.Generic;
using System.Linq;

using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Util;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Represents the fill model used to simulate order fills for equities
    /// </summary>
    public class EquityFillModel : FillModel
    {
        /// <summary>
        /// Default limit if touched fill model implementation in base class security.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <remarks>
        ///     There is no good way to model limit orders with OHLC because we never know whether the market has
        ///     gapped past our fill price. We have to make the assumption of a fluid, high volume market.
        ///
        ///     With Limit if Touched orders, whether or not a trigger is surpassed is determined by the high (low)
        ///     of the previous tradebar when making a sell (buy) request. Following the behaviour of
        ///     <see cref="StopLimitFill"/>, current quote information is used when determining fill parameters
        ///     (e.g., price, quantity) as the tradebar containing the incoming data is not yet consolidated.
        ///     This conservative approach, however, can lead to trades not occuring as would be expected when
        ///     compared to future consolidated data.
        /// </remarks>
        public override OrderEvent LimitIfTouchedFill(Security asset, LimitIfTouchedOrder order)
        {
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // Fill only if open or extended
            if (!IsExchangeOpen(asset,
                Parameters.ConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }

            // Get the trade bar that closes after the order time
            var tradeBar = GetBestEffortTradeBar(asset, order.Time);

            // Do not fill on stale data
            if (tradeBar == null) return fill;

            //Check if the limit if touched order was filled:
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //-> 1.2 Buy: If Price below Trigger, Buy:
                    if (tradeBar.Low <= order.TriggerPrice || order.TriggerTouched)
                    {
                        order.TriggerTouched = true;
                        var askCurrent = GetBestEffortAskPrice(asset, order.Time, out var fillMessage);

                        if (askCurrent <= order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = Math.Min(askCurrent, order.LimitPrice);
                            fill.FillQuantity = order.Quantity;
                            fill.Message = fillMessage;
                        }
                    }

                    break;

                case OrderDirection.Sell:
                    //-> 1.2 Sell: If Price above Trigger, Sell:
                    if (tradeBar.High >= order.TriggerPrice || order.TriggerTouched)
                    {
                        order.TriggerTouched = true;
                        var bidCurrent = GetBestEffortBidPrice(asset, order.Time, out var fillMessage);

                        if (bidCurrent >= order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = Math.Max(bidCurrent, order.LimitPrice);
                            fill.FillQuantity = order.Quantity;
                            fill.Message = fillMessage;
                        }
                    }

                    break;
            }
            return fill;
        }

        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled) return fill;

            // Make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            // Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            var fillMessage = string.Empty;

            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //Order [fill]price for a buy market order model is the current security ask price
                    fill.FillPrice = GetBestEffortAskPrice(asset, order.Time, out fillMessage) + slip;
                    break;
                case OrderDirection.Sell:
                    //Order [fill]price for a buy market order model is the current security bid price
                    fill.FillPrice = GetBestEffortBidPrice(asset, order.Time, out fillMessage) - slip;
                    break;
            }

            // assume the order completely filled
            fill.FillQuantity = order.Quantity;
            fill.Message = fillMessage;
            fill.Status = OrderStatus.Filled;
            return fill;
        }

        /// <summary>
        /// Stop fill model implementation for Equity.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <remarks>
        /// A Stop order is an instruction to submit a buy or sell market order
        /// if and when the user-specified stop trigger price is attained or penetrated.
        ///
        /// A Sell Stop order is always placed below the current market price.
        /// We assume a fluid/continuous, high volume market. Therefore, it is filled at the stop trigger price
        /// if the current low price of trades is less than or equal to this price.
        ///
        /// A Buy Stop order is always placed above the current market price.
        /// We assume a fluid, high volume market. Therefore, it is filled at the stop trigger price
        /// if the current high price of trades is greater or equal than this price.
        ///
        /// The continuous market assumption is not valid if the market opens with an unfavorable gap.
        /// In this case, a new bar opens below/above the stop trigger price, and the order is filled with the opening price.
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        public override OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
        {
            // Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            // If cancelled, don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // Make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            // Get the trade bar that closes after the order time
            var tradeBar = GetBestEffortTradeBar(asset, order.Time);

            // Do not fill on stale data
            if (tradeBar == null) return fill;

            switch (order.Direction)
            {
                case OrderDirection.Sell:
                    if (tradeBar.Low <= order.StopPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        fill.FillQuantity = order.Quantity;

                        var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

                        // Unfavorable gap case: if the bar opens below the stop price, fill at open price
                        if (tradeBar.Open <= order.StopPrice)
                        {
                            fill.FillPrice = tradeBar.Open - slip;
                            fill.Message = Messages.EquityFillModel.FilledWithOpenDueToUnfavorableGap(asset, tradeBar);
                            return fill;
                        }

                        fill.FillPrice = order.StopPrice - slip;
                    }
                    break;

                case OrderDirection.Buy:
                    if (tradeBar.High >= order.StopPrice)
                    {
                        fill.Status = OrderStatus.Filled;
                        fill.FillQuantity = order.Quantity;

                        var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

                        // Unfavorable gap case: if the bar opens above the stop price, fill at open price
                        if (tradeBar.Open >= order.StopPrice)
                        {
                            fill.FillPrice = tradeBar.Open + slip;
                            fill.Message = Messages.EquityFillModel.FilledWithOpenDueToUnfavorableGap(asset, tradeBar);
                            return fill;
                        }

                        fill.FillPrice = order.StopPrice + slip;
                    }
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Default stop limit fill model implementation in base class security. (Stop Limit Order Type)
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
            //Default order event to return.
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(
                asset,
                Parameters.ConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }

            //Get the range of prices in the last bar:
            var prices = GetPricesCheckingPythonWrapper(asset, order.Direction);
            var pricesEndTime = prices.EndTime.ConvertToUtc(asset.Exchange.TimeZone);

            // do not fill on stale data
            if (pricesEndTime <= order.Time) return fill;

            //Check if the Stop Order was filled: opposite to a limit order
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    //-> 1.2 Buy Stop: If Price Above Setpoint, Buy:
                    if (prices.High > order.StopPrice || order.StopTriggered)
                    {
                        if (!order.StopTriggered)
                        {
                            order.StopTriggered = true;
                            Parameters.OnOrderUpdated(order);
                        }

                        // Fill the limit order, using closing price of bar:
                        // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                        if (prices.Current < order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = Math.Min(prices.High, order.LimitPrice);
                            // assume the order completely filled
                            fill.FillQuantity = order.Quantity;
                        }
                    }
                    break;

                case OrderDirection.Sell:
                    //-> 1.1 Sell Stop: If Price below setpoint, Sell:
                    if (prices.Low < order.StopPrice || order.StopTriggered)
                    {
                        if (!order.StopTriggered)
                        {
                            order.StopTriggered = true;
                            Parameters.OnOrderUpdated(order);
                        }

                        // Fill the limit order, using minimum price of the bar
                        // Note > Can't use minimum price, because no way to be sure minimum wasn't before the stop triggered.
                        if (prices.Current > order.LimitPrice)
                        {
                            fill.Status = OrderStatus.Filled;
                            fill.FillPrice = Math.Max(prices.Low, order.LimitPrice);
                            // assume the order completely filled
                            fill.FillQuantity = order.Quantity;
                        }
                    }
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Limit fill model implementation for Equity.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <remarks>
        /// A Limit order is an order to buy or sell at a specified price or better.
        /// The Limit order ensures that if the order fills, it will not fill at a price less favorable than your limit price,
        /// but it does not guarantee a fill.
        ///
        /// A Buy Limit order is always placed above the current market price.
        /// We assume a fluid/continuous, high volume market. Therefore, it is filled at the limit price
        /// if the current low price of trades is less than this price.
        ///
        /// A Sell Limit order is always placed below the current market price.
        /// We assume a fluid, high volume market. Therefore, it is filled at the limit price
        /// if the current high price of trades is greater than this price.
        ///
        /// This model does not trigger the limit order when the limit is attained (equals to).
        /// Since the order may not be filled in reality if it is not the top of the order book
        /// (first come, first served), we assume our order is the last in the book with its limit price,
        /// thus it will be filled when the limit price is penetrated.
        ///
        /// The continuous market assumption is not valid if the market opens with a favorable gap.
        /// If the buy/sell limit order is placed below/above the current market price,
        /// the order is filled with the opening price.
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        public override OrderEvent LimitFill(Security asset, LimitOrder order)
        {
            //Initialise;
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            //If its cancelled don't need anymore checks:
            if (order.Status == OrderStatus.Canceled) return fill;

            // make sure the exchange is open before filling -- allow pre/post market fills to occur
            if (!IsExchangeOpen(asset,
                Parameters.ConfigProvider
                    .GetSubscriptionDataConfigs(asset.Symbol)
                    .IsExtendedMarketHours()))
            {
                return fill;
            }

            // Get the trade bar that closes after the order time
            var tradeBar = GetBestEffortTradeBar(asset, order.Time);

            // Do not fill on stale data
            if (tradeBar == null) return fill;

            //-> Valid Live/Model Order:
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    if (tradeBar.Low < order.LimitPrice)
                    {
                        // assume the order completely filled
                        // TODO: Add separate DepthLimited fill partial order quantities based on tick quantity / bar.Volume available.
                        fill.FillQuantity = order.Quantity;
                        fill.Status = OrderStatus.Filled;

                        fill.FillPrice = order.LimitPrice;

                        // Favorable gap case: if the bar opens below the limit price, fill at open price
                        if (tradeBar.Open < order.LimitPrice)
                        {
                            fill.FillPrice = tradeBar.Open;
                            fill.Message = Messages.EquityFillModel.FilledWithOpenDueToFavorableGap(asset, tradeBar);
                            return fill;
                        }
                    }
                    break;
                case OrderDirection.Sell:
                    if (tradeBar.High > order.LimitPrice)
                    {
                        // Assume the order completely filled
                        // TODO: Add separate DepthLimited fill partial order quantities based on tick quantity / bar.Volume available.
                        fill.FillQuantity = order.Quantity;
                        fill.Status = OrderStatus.Filled;

                        fill.FillPrice = order.LimitPrice;

                        // Favorable gap case: if the bar opens above the limit price, fill at open price
                        if (tradeBar.Open > order.LimitPrice)
                        {
                            fill.FillPrice = tradeBar.Open;
                            fill.Message = Messages.EquityFillModel.FilledWithOpenDueToFavorableGap(asset, tradeBar);
                            return fill;
                        }
                    }
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Market on Open Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order)
        {
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled) return fill;

            // MOO should never fill on the same bar or on stale data
            // Imagine the case where we have a thinly traded equity, ASUR, and another liquid
            // equity, say SPY, SPY gets data every minute but ASUR, if not on fill forward, maybe
            // have large gaps, in which case the currentBar.EndTime will be in the past
            // ASUR  | | |      [order]        | | | | | | |
            //  SPY  | | | | | | | | | | | | | | | | | | | |
            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            var endTime = DateTime.MinValue;

            var subscribedTypes = GetSubscribedTypes(asset);

            if (subscribedTypes.Contains(typeof(Tick)))
            {
                var primaryExchangeCode = ((Equity)asset).PrimaryExchange.Code;
                var openTradeTickFlags = (uint)(TradeConditionFlags.OfficialOpen | TradeConditionFlags.OpeningPrints);

                var trades = asset.Cache.GetAll<Tick>()
                    .Where(x => x.TickType == TickType.Trade && asset.Exchange.DateTimeIsOpen(x.Time))
                    .OrderBy(x => x.EndTime).ToList();

                // Get the first valid (non-zero) tick of trade type from an open market
                var tick = trades
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.SaleCondition) &&
                        x.ExchangeCode == primaryExchangeCode &&
                        (x.ParsedSaleCondition & openTradeTickFlags) != 0 &&
                        asset.Exchange.DateTimeIsOpen(x.Time));

                // If there is no OfficialOpen or OpeningPrints in the current list of trades,
                // we will wait for the next up to 1 minute before accepting the last trade without flags
                // We will give priority to trade then use quote to get the timestamp
                // If there are only quotes, we will need to test for the tick type before we assign the fill price
                if (tick == null)
                {
                    var previousOpen = asset.Exchange.Hours
                        .GetMarketHours(asset.LocalTime)
                        .GetMarketOpen(TimeSpan.Zero, false);

                    fill.Message = Messages.EquityFillModel.MarketOnOpenFillNoOfficialOpenOrOpeningPrintsWithinOneMinute;

                    tick = trades.LastOrDefault() ?? asset.Cache.GetAll<Tick>().LastOrDefault();
                    if ((tick?.EndTime.TimeOfDay - previousOpen)?.TotalMinutes < 1)
                    {
                        return fill;
                    }

                    fill.Message += " " + Messages.EquityFillModel.FilledWithLastTickTypeData(tick);
                }

                endTime = tick?.EndTime ?? endTime;

                if (tick?.TickType == TickType.Trade)
                {
                    fill.FillPrice = tick.Price;
                }
            }
            else if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null)
                {
                    // If the order was placed during the bar aggregation, we cannot use its open price
                    if (tradeBar.Time < localOrderTime) return fill;

                    // We need to verify whether the trade data is from the open market.
                    if (tradeBar.Period < Resolution.Hour.ToTimeSpan() && !asset.Exchange.DateTimeIsOpen(tradeBar.Time))
                    {
                        return fill;
                    }

                    endTime = tradeBar.EndTime;
                    fill.FillPrice = tradeBar.Open;
                }
            }
            else
            {
                fill.Message = Messages.EquityFillModel.FilledWithQuoteData(asset);
            }

            if (localOrderTime >= endTime) return fill;

            // if the MOO was submitted during market the previous day, wait for a day to turn over
            // The date of the order and the trade data end time cannot be the same.
            // Note that the security local time can be ahead of the data end time.
            if (asset.Exchange.DateTimeIsOpen(localOrderTime) && localOrderTime.Date == endTime.Date)
            {
                return fill;
            }

            // wait until market open
            // make sure the exchange is open/normal market hours before filling
            if (!IsExchangeOpen(asset, false)) return fill;

            // assume the order completely filled
            fill.FillQuantity = order.Quantity;
            fill.Status = OrderStatus.Filled;

            //Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            var bestEffortMessage = "";

            // If there is no trade information, get the bid or ask, then apply the slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    if (fill.FillPrice == 0)
                    {
                        fill.FillPrice = GetBestEffortAskPrice(asset, order.Time, out bestEffortMessage);
                        fill.Message += bestEffortMessage;
                    }

                    fill.FillPrice += slip;
                    break;
                case OrderDirection.Sell:
                    if (fill.FillPrice == 0)
                    {
                        fill.FillPrice = GetBestEffortBidPrice(asset, order.Time, out bestEffortMessage);
                        fill.Message += bestEffortMessage;
                    }

                    fill.FillPrice -= slip;
                    break;
            }

            return fill;
        }

        /// <summary>
        /// Market on Close Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        public override OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order)
        {
            var utcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);
            var fill = new OrderEvent(order, utcTime, OrderFee.Zero);

            if (order.Status == OrderStatus.Canceled) return fill;

            var localOrderTime = order.Time.ConvertFromUtc(asset.Exchange.TimeZone);
            var nextMarketClose = asset.Exchange.Hours.GetNextMarketClose(localOrderTime, false);

            // wait until market closes after the order time
            if (asset.LocalTime < nextMarketClose)
            {
                return fill;
            }

            var subscribedTypes = GetSubscribedTypes(asset);

            if (subscribedTypes.Contains(typeof(Tick)))
            {
                var primaryExchangeCode = ((Equity)asset).PrimaryExchange.Code;
                var closeTradeTickFlags = (uint)(TradeConditionFlags.OfficialClose | TradeConditionFlags.ClosingPrints);

                var trades = asset.Cache.GetAll<Tick>()
                    .Where(x => x.TickType == TickType.Trade)
                    .OrderBy(x => x.EndTime).ToList();

                // Get the last valid (non-zero) tick of trade type from an close market
                var tick = trades
                    .LastOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x.SaleCondition) &&
                        x.ExchangeCode == primaryExchangeCode
                        && (x.ParsedSaleCondition & closeTradeTickFlags) != 0);

                // If there is no OfficialClose or ClosingPrints in the current list of trades,
                // we will wait for the next up to 1 minute before accepting the last tick without flags
                // We will give priority to trade then use quote to get the timestamp
                // If there are only quotes, we will need to test for the tick type before we assign the fill price
                if (tick == null)
                {
                    tick = trades.LastOrDefault() ?? asset.Cache.GetAll<Tick>().LastOrDefault();
                    if (Parameters.ConfigProvider.GetSubscriptionDataConfigs(asset.Symbol).IsExtendedMarketHours())
                    {
                        fill.Message = Messages.EquityFillModel.MarketOnCloseFillNoOfficialCloseOrClosingPrintsWithinOneMinute;

                        if ((tick?.EndTime - nextMarketClose)?.TotalMinutes < 1)
                        {
                            return fill;
                        }
                    }
                    else
                    {
                        fill.Message = Messages.EquityFillModel.MarketOnCloseFillNoOfficialCloseOrClosingPrintsWithoutExtendedMarketHours;
                    }

                    fill.Message += " " + Messages.EquityFillModel.FilledWithLastTickTypeData(tick);
                }

                if (tick?.TickType == TickType.Trade)
                {
                    fill.FillPrice = tick.Price;
                }
            }
            // make sure the exchange is open/normal market hours before filling
            // It will return true if the last bar opens before the market closes
            else if (!IsExchangeOpen(asset, false))
            {
                return fill;
            }
            else if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                fill.FillPrice = asset.Cache.GetData<TradeBar>()?.Close ?? 0;
            }
            else
            {
                fill.Message = Messages.EquityFillModel.FilledWithQuoteData(asset);
            }

            // Calculate the model slippage: e.g. 0.01c
            var slip = asset.SlippageModel.GetSlippageApproximation(asset, order);

            var bestEffortMessage = "";

            // If there is no trade information, get the bid or ask, then apply the slippage
            switch (order.Direction)
            {
                case OrderDirection.Buy:
                    if (fill.FillPrice == 0)
                    {
                        fill.FillPrice = GetBestEffortAskPrice(asset, order.Time, out bestEffortMessage);
                        fill.Message += bestEffortMessage;
                    }

                    fill.FillPrice += slip;
                    break;
                case OrderDirection.Sell:
                    if (fill.FillPrice == 0)
                    {
                        fill.FillPrice = GetBestEffortBidPrice(asset, order.Time, out bestEffortMessage);
                        fill.Message += bestEffortMessage;
                    }

                    fill.FillPrice -= slip;
                    break;
            }

            // assume the order completely filled
            fill.FillQuantity = order.Quantity;
            fill.Status = OrderStatus.Filled;

            return fill;
        }

        /// <summary>
        /// Get data types the Security is subscribed to
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        protected override HashSet<Type> GetSubscribedTypes(Security asset)
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
        /// This method will try to get the most recent ask price data, so it will try to get tick quote first, then quote bar.
        /// If no quote, tick or bar, is available (e.g. hourly data), use trade data with preference to tick data.
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        /// <param name="orderTime">Time the order was submitted</param>
        /// <param name="message">Information about the best effort, whether prices are stale or need to use trade information</param>
        private decimal GetBestEffortAskPrice(Security asset, DateTime orderTime, out string message)
        {
            message = string.Empty;
            BaseData baseData = null;
            var bestEffortAskPrice = 0m;

            // Define the cut off time to get the best effort bid or ask and whether the price is stale
            var localOrderTime = orderTime.ConvertFromUtc(asset.Exchange.TimeZone);
            var cutOffTime = localOrderTime.Add(-Parameters.StalePriceTimeSpan);

            var subscribedTypes = GetSubscribedTypes(asset);

            List<Tick> ticks = null;
            var isTickSubscribed = subscribedTypes.Contains(typeof(Tick));

            if (isTickSubscribed)
            {
                ticks = asset.Cache.GetAll<Tick>().ToList();

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.AskPrice > 0);
                if (quote != null)
                {
                    if (quote.EndTime >= cutOffTime)
                    {
                        return quote.AskPrice;
                    }

                    baseData = quote;
                    bestEffortAskPrice = quote.AskPrice;
                    message = Messages.EquityFillModel.FilledWithQuoteTickData(asset, quote);
                }
            }

            if (subscribedTypes.Contains(typeof(QuoteBar)))
            {
                var quoteBar = asset.Cache.GetData<QuoteBar>();
                if (quoteBar != null && (baseData == null || quoteBar.EndTime > baseData.EndTime))
                {
                    if (quoteBar.EndTime >= cutOffTime)
                    {
                        return quoteBar.Ask?.Close ?? quoteBar.Close;
                    }

                    baseData = quoteBar;
                    bestEffortAskPrice = quoteBar.Ask?.Close ?? quoteBar.Close;
                    message = Messages.EquityFillModel.FilledWithQuoteBarData(asset, quoteBar);
                }
            }

            if (isTickSubscribed)
            {
                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade);
                if (trade != null && (baseData == null || trade.EndTime > baseData.EndTime))
                {
                    message = Messages.EquityFillModel.FilledWithTradeTickData(asset, trade);

                    if (trade.EndTime >= cutOffTime)
                    {
                        return trade.Price;
                    }

                    baseData = trade;
                    bestEffortAskPrice = trade.Price;
                }
            }

            if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null && (baseData == null || tradeBar.EndTime > baseData.EndTime))
                {
                    message = Messages.EquityFillModel.FilledWithTradeBarData(asset, tradeBar);

                    if (tradeBar.EndTime >= cutOffTime)
                    {
                        return tradeBar.Close;
                    }

                    baseData = tradeBar;
                    bestEffortAskPrice = tradeBar.Close;
                }
            }

            if (baseData != null)
            {
                return bestEffortAskPrice;
            }

            throw new InvalidOperationException(Messages.FillModel.NoMarketDataToGetAskPriceForFilling(asset, subscribedTypes));
        }

        /// <summary>
        /// Get current bid price for subscribed data
        /// This method will try to get the most recent bid price data, so it will try to get tick quote first, then quote bar.
        /// If no quote, tick or bar, is available (e.g. hourly data), use trade data with preference to tick data.
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        /// <param name="orderTime">Time the order was submitted</param>
        /// <param name="message">Information about the best effort, whether prices are stale or need to use trade information</param>
        private decimal GetBestEffortBidPrice(Security asset, DateTime orderTime, out string message)
        {
            message = string.Empty;
            BaseData baseData = null;
            var bestEffortBidPrice = 0m;

            // Define the cut off time to get the best effort bid or ask and whether the price is stale
            var localOrderTime = orderTime.ConvertFromUtc(asset.Exchange.TimeZone);
            var cutOffTime = localOrderTime.Add(-Parameters.StalePriceTimeSpan);

            var subscribedTypes = GetSubscribedTypes(asset);

            List<Tick> ticks = null;
            var isTickSubscribed = subscribedTypes.Contains(typeof(Tick));

            if (isTickSubscribed)
            {
                ticks = asset.Cache.GetAll<Tick>().ToList();

                var quote = ticks.LastOrDefault(x => x.TickType == TickType.Quote && x.BidPrice > 0);
                if (quote != null)
                {
                    if (quote.EndTime >= cutOffTime)
                    {
                        return quote.BidPrice;
                    }

                    baseData = quote;
                    bestEffortBidPrice = quote.BidPrice;
                    message = Messages.EquityFillModel.FilledWithQuoteTickData(asset, quote);
                }
            }

            if (subscribedTypes.Contains(typeof(QuoteBar)))
            {
                var quoteBar = asset.Cache.GetData<QuoteBar>();
                if (quoteBar != null && (baseData == null || quoteBar.EndTime > baseData.EndTime))
                {
                    if (quoteBar.EndTime >= cutOffTime)
                    {
                        return quoteBar.Bid?.Close ?? quoteBar.Close;
                    }

                    baseData = quoteBar;
                    bestEffortBidPrice = quoteBar.Bid?.Close ?? quoteBar.Close;
                    message = Messages.EquityFillModel.FilledWithQuoteBarData(asset, quoteBar);
                }
            }

            if (isTickSubscribed)
            {
                var trade = ticks.LastOrDefault(x => x.TickType == TickType.Trade);
                if (trade != null && (baseData == null || trade.EndTime > baseData.EndTime))
                {
                    message = Messages.EquityFillModel.FilledWithTradeTickData(asset, trade);

                    if (trade.EndTime >= cutOffTime)
                    {
                        return trade.Price;
                    }

                    baseData = trade;
                    bestEffortBidPrice = trade.Price;
                }
            }

            if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();
                if (tradeBar != null && (baseData == null || tradeBar.EndTime > baseData.EndTime))
                {
                    message = Messages.EquityFillModel.FilledWithTradeBarData(asset, tradeBar);

                    if (tradeBar.EndTime >= cutOffTime)
                    {
                        return tradeBar.Close;
                    }

                    baseData = tradeBar;
                    bestEffortBidPrice = tradeBar.Close;
                }
            }

            if (baseData != null)
            {
                return bestEffortBidPrice;
            }

            throw new InvalidOperationException(Messages.FillModel.NoMarketDataToGetBidPriceForFilling(asset, subscribedTypes));
        }

        /// <summary>
        /// Get current trade bar for subscribed data
        /// This method will try to get the most recent trade bar after the order time,
        /// so it will try to get tick trades first to create a trade bar, then trade bar.
        /// </summary>
        /// <param name="asset">Security which has subscribed data types</param>
        /// <param name="orderTime">Time the order was submitted</param>
        /// <returns>
        /// A TradeBar object with the most recent trade information after the order close.
        /// If there is no trade information or it is older than the order, returns null.
        /// </returns>
        private TradeBar GetBestEffortTradeBar(Security asset, DateTime orderTime)
        {
            TradeBar bestEffortTradeBar = null;

            var subscribedTypes = GetSubscribedTypes(asset);

            if (subscribedTypes.Contains(typeof(Tick)))
            {
                var tradeOpen = 0m;
                var tradeHigh = decimal.MinValue;
                var tradeLow = decimal.MaxValue;
                var tradeClose = 0m;
                var tradeVolume = 0m;
                var startTimeUtc = DateTime.MinValue;
                var endTimeUtc = DateTime.MinValue;

                var trades = asset.Cache.GetAll<Tick>().Where(x => x.TickType == TickType.Trade).ToList();
                if (trades.Any())
                {
                    foreach (var trade in trades)
                    {
                        if (tradeOpen == 0)
                        {
                            tradeOpen = trade.Price;
                            startTimeUtc = trade.Time;
                        }

                        tradeHigh = Math.Max(tradeHigh, trade.Price);
                        tradeLow = Math.Min(tradeLow, trade.Price);
                        tradeClose = trade.Price;
                        tradeVolume += trade.Quantity;
                        endTimeUtc = trade.EndTime;
                    }

                    bestEffortTradeBar = new TradeBar(startTimeUtc, asset.Symbol,
                        tradeOpen, tradeHigh, tradeLow, tradeClose, tradeVolume, endTimeUtc - startTimeUtc);
                }
            }
            else if (subscribedTypes.Contains(typeof(TradeBar)))
            {
                bestEffortTradeBar = asset.Cache.GetData<TradeBar>();
            }

            // Do not accept trade information older than the order
            if (bestEffortTradeBar == null ||
                bestEffortTradeBar.EndTime.ConvertToUtc(asset.Exchange.TimeZone) <= orderTime)
            {
                return null;
            }

            return bestEffortTradeBar;
        }

        /// <summary>
        /// This is required due to a limitation in PythonNet to resolved
        /// overriden methods. <see cref="GetPrices"/>
        /// </summary>
        protected override Prices GetPricesCheckingPythonWrapper(Security asset, OrderDirection direction)
        {
            if (PythonWrapper != null)
            {
                var prices = PythonWrapper.GetPricesInternal(asset, direction);
                return new Prices(prices.EndTime, prices.Current, prices.Open, prices.High, prices.Low, prices.Close);
            }
            return GetPrices(asset, direction);
        }

        /// <summary>
        /// Get the minimum and maximum price for this security in the last bar:
        /// </summary>
        /// <param name="asset">Security asset we're checking</param>
        /// <param name="direction">The order direction, decides whether to pick bid or ask</param>
        protected override Prices GetPrices(Security asset, OrderDirection direction)
        {
            var low = asset.Low;
            var high = asset.High;
            var open = asset.Open;
            var close = asset.Close;
            var current = asset.Price;
            var endTime = asset.Cache.GetData()?.EndTime ?? DateTime.MinValue;

            if (direction == OrderDirection.Hold)
            {
                return new Prices(endTime, current, open, high, low, close);
            }

            // Only fill with data types we are subscribed to
            var subscriptionTypes = Parameters.ConfigProvider
                .GetSubscriptionDataConfigs(asset.Symbol)
                .Select(x => x.Type).ToList();
            // Tick
            var tick = asset.Cache.GetData<Tick>();
            if (subscriptionTypes.Contains(typeof(Tick)) && tick != null)
            {
                var price = direction == OrderDirection.Sell ? tick.BidPrice : tick.AskPrice;
                if (price != 0m)
                {
                    return new Prices(tick.EndTime, price, 0, 0, 0, 0);
                }

                // If the ask/bid spreads are not available for ticks, try the price
                price = tick.Price;
                if (price != 0m)
                {
                    return new Prices(tick.EndTime, price, 0, 0, 0, 0);
                }
            }

            // Quote
            var quoteBar = asset.Cache.GetData<QuoteBar>();
            if (subscriptionTypes.Contains(typeof(QuoteBar)) && quoteBar != null)
            {
                var bar = direction == OrderDirection.Sell ? quoteBar.Bid : quoteBar.Ask;
                if (bar != null)
                {
                    return new Prices(quoteBar.EndTime, bar);
                }
            }

            // Trade
            var tradeBar = asset.Cache.GetData<TradeBar>();
            if (subscriptionTypes.Contains(typeof(TradeBar)) && tradeBar != null)
            {
                return new Prices(tradeBar);
            }

            return new Prices(endTime, current, open, high, low, close);
        }
    }
}
