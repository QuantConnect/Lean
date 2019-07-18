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
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// This fill model is provided because currently the data sourced for Crypto
    /// is limited to one minute snapshots for Quote data. This fill model will
    /// ignore the trade/quote distinction and return the latest pricing information
    /// in order to determine the correct fill price
    /// </summary>
    public class LatestPriceFillModel : ImmediateFillModel
    {
        /// <summary>
        /// Get the minimum and maximum price for this security in the last bar
        /// Ignore the Trade/Quote distinction - fill with the latest pricing information
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
                    return new Prices(endTime, price, 0, 0, 0, 0);
                }

                // If the ask/bid spreads are not available for ticks, try the price
                price = tick.Price;
                if (price != 0m)
                {
                    return new Prices(endTime, price, 0, 0, 0, 0);
                }
            }

            // Get both the last trade and last quote
            // Assume that the security has both a trade and quote subscription
            // This should be true for crypto securities
            var quoteBar = asset.Cache.GetData<QuoteBar>();
            if (quoteBar != null)
            {
                var tradeBar = asset.Cache.GetData<TradeBar>();

                if (tradeBar != null && tradeBar.EndTime > quoteBar.EndTime)
                {
                    // The latest pricing data came from a trade
                    return new Prices(tradeBar);
                }
                else
                {
                    // The latest pricing data came from a quote
                    var bar = direction == OrderDirection.Sell ? quoteBar.Bid : quoteBar.Ask;
                    if (bar != null)
                    {
                        return new Prices(quoteBar.EndTime, bar);
                    }
                }
            }

            return new Prices(endTime, current, open, high, low, close);
        }
    }
}
