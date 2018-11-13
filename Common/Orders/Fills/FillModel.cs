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
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Provides a base class for all fill models
    /// </summary>
    public class FillModel : IFillModel
    {
        /// <summary>
        /// Default market fill model for the base security class. Fills at the last traded price.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        [Obsolete("This was left for retro compatibility, see new MarketFill(FillModelContext context)")]
        public virtual OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            // The system will NOT call this method, but the user derivate might
            // through base.xxxxFill(asset, order), in which case SDCProvider will be set
            if (SubscriptionDataConfigProvider != null)
            {
                return MarketFillImplementation(
                    new FillModelContext(
                        asset,
                        order,
                        SubscriptionDataConfigProvider
                    )
                );
            }
            throw new NotImplementedException("Unexpected usage of IFillModel method. " +
                "This was left just for retro compatibility.");
        }

        /// <summary>
        /// Default stop fill model implementation in base class security. (Stop Market Order Type)
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        [Obsolete("This was left for retro compatibility, see new StopMarketFill(FillModelContext context)")]
        public virtual OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
        {
            // The system will NOT call this method, but the user derivate might
            // through base.xxxxFill(asset, order), in which case SDCProvider will be set
            if (SubscriptionDataConfigProvider != null)
            {
                return StopMarketFillImplementation(
                    new FillModelContext(
                        asset,
                        order,
                        SubscriptionDataConfigProvider
                    )
                );
            }
            throw new NotImplementedException("Unexpected usage of IFillModel method. " +
                "This was left just for retro compatibility.");
        }

        /// <summary>
        /// Default stop limit fill model implementation in base class security. (Stop Limit Order Type)
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        [Obsolete("This was left for retro compatibility, see new StopLimitFill(FillModelContext context)")]
        public virtual OrderEvent StopLimitFill(Security asset, StopLimitOrder order)
        {
            // The system will NOT call this method, but the user derivate might
            // through base.xxxxFill(asset, order), in which case SDCProvider will be set
            if (SubscriptionDataConfigProvider != null)
            {
                return StopLimitFillImplementation(
                    new FillModelContext(
                        asset,
                        order,
                        SubscriptionDataConfigProvider
                    )
                );
            }
            throw new NotImplementedException("Unexpected usage of IFillModel method. " +
                "This was left just for retro compatibility.");
        }

        /// <summary>
        /// Default limit order fill model in the base security class.
        /// </summary>
        /// <param name="asset">Security asset we're filling</param>
        /// <param name="order">Order packet to model</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        /// <seealso cref="StopMarketFill(Security, StopMarketOrder)"/>
        /// <seealso cref="MarketFill(Security, MarketOrder)"/>
        [Obsolete("This was left for retro compatibility, see new LimitFill(FillModelContext context)")]
        public virtual OrderEvent LimitFill(Security asset, LimitOrder order)
        {
            // The system will NOT call this method, but the user derivate might
            // through base.xxxxFill(asset, order), in which case SDCProvider will be set
            if (SubscriptionDataConfigProvider != null)
            {
                return LimitFillImplementation(
                    new FillModelContext(
                        asset,
                        order,
                        SubscriptionDataConfigProvider
                    )
                );
            }
            throw new NotImplementedException("Unexpected usage of IFillModel method. " +
                "This was left just for retro compatibility.");
        }

        /// <summary>
        /// Market on Open Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        [Obsolete("This was left for retro compatibility, see new MarketOnOpenFill(FillModelContext context)")]
        public OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order)
        {
            // The system will NOT call this method, but the user derivate might
            // through base.xxxxFill(asset, order), in which case SDCProvider will be set
            if (SubscriptionDataConfigProvider != null)
            {
                return MarketOnOpenFillImplementation(
                    new FillModelContext(
                        asset,
                        order,
                        SubscriptionDataConfigProvider
                    )
                );
            }
            throw new NotImplementedException("Unexpected usage of IFillModel method. " +
                "This was left just for retro compatibility.");
        }

        /// <summary>
        /// Market on Close Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        [Obsolete("This was left for retro compatibility, see new MarketOnCloseFill(FillModelContext context)")]
        public OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order)
        {
            // The system will NOT call this method, but the user derivate might
            // through base.xxxxFill(asset, order), in which case SDCProvider will be set
            if (SubscriptionDataConfigProvider != null)
            {
                return MarketOnCloseFillImplementation(
                    new FillModelContext(
                        asset,
                        order,
                        SubscriptionDataConfigProvider
                    )
                );
            }
            throw new NotImplementedException("Unexpected usage of IFillModel method. " +
                "This was left just for retro compatibility.");
        }
    }
}