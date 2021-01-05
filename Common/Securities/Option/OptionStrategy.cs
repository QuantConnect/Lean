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

using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Option strategy specification class. Describes option strategy and its parameters for trading.
    /// </summary>
    public class OptionStrategy
    {
        /// <summary>
        /// Option strategy name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Underlying symbol of the strategy
        /// </summary>
        public Symbol Underlying { get; set; }

        /// <summary>
        /// Option strategy legs
        /// </summary>
        public List<OptionLegData> OptionLegs { get; set; } = new List<OptionLegData>();

        /// <summary>
        /// Option strategy underlying legs (usually 0 or 1 legs)
        /// </summary>
        public List<UnderlyingLegData> UnderlyingLegs { get; set; } = new List<UnderlyingLegData>();

        /// <summary>
        /// Defines common properties between <see cref="OptionLegData"/> and <see cref="UnderlyingLegData"/>
        /// </summary>
        public abstract class LegData
        {
            /// <summary>
            /// This is an optional parameter that, if not specified, is resolved by the algorithm's base class.
            /// If the strategy is produced via the <see cref="OptionStrategyMatcher"/>, then this will be populated.
            /// </summary>
            public Symbol Symbol { get; set; }

            /// <summary>
            /// Quantity multiplier used to specify proper scale (and direction) of the leg within the strategy
            /// </summary>
            public int Quantity { get; set; }

            /// <summary>
            /// Type of order that is to be sent to the market on strategy execution
            /// </summary>
            public OrderType OrderType { get; set; }

            /// <summary>
            /// Order limit price of the leg in case limit order is sent to the market on strategy execution
            /// </summary>
            public decimal OrderPrice { get; set; }

            /// <summary>
            /// Invokes the correct handler based on the runtime type.
            /// </summary>
            public abstract void Invoke(Action<UnderlyingLegData> underlyingHandler, Action<OptionLegData> optionHandler);
        }

        /// <summary>
        /// This class is a POCO containing basic data for the option legs of the strategy
        /// </summary>
        public class OptionLegData : LegData
        {
            /// <summary>
            /// Option right (type) of the option leg
            /// </summary>
            public OptionRight Right { get; set; }

            /// <summary>
            /// Expiration date of the leg
            /// </summary>
            public DateTime Expiration { get; set; }

            /// <summary>
            /// Strike price of the leg
            /// </summary>
            public decimal Strike { get; set; }

            /// <summary>
            /// Creates a new instance of <see cref="OptionLegData"/> from the specified parameters
            /// </summary>
            public static OptionLegData Create(int quantity, Symbol symbol, OrderType orderType = OrderType.Market, decimal? orderPrice = null)
            {
                return new OptionLegData
                {
                    Symbol = symbol,
                    Quantity = quantity,
                    OrderType = orderType,
                    Expiration = symbol.ID.Date,
                    OrderPrice = orderPrice ?? 0m,
                    Right = symbol.ID.OptionRight,
                    Strike = symbol.ID.StrikePrice
                };
            }

            /// <summary>
            /// Invokes the <paramref name="optionHandler"/>
            /// </summary>
            public override void Invoke(Action<UnderlyingLegData> underlyingHandler, Action<OptionLegData> optionHandler)
            {
                optionHandler(this);
            }
        }

        /// <summary>
        /// This class is a POCO containing basic data for the underlying leg of the strategy
        /// </summary>
        public class UnderlyingLegData : LegData
        {
            /// <summary>
            /// Creates a new instance of <see cref="UnderlyingLegData"/> for the specified <paramref name="quantity"/> of underlying shares.
            /// </summary>
            public static UnderlyingLegData Create(int quantity, Symbol symbol, OrderType orderType = OrderType.Market, decimal? orderPrice = null)
            {
                var data = Create(quantity, orderType, orderPrice);
                data.Symbol = symbol;
                return data;
            }

            /// <summary>
            /// Creates a new instance of <see cref="UnderlyingLegData"/> for the specified <paramref name="quantity"/> of underlying shares.
            /// </summary>
            public static UnderlyingLegData Create(int quantity, OrderType orderType = OrderType.Market, decimal? orderPrice = null)
            {
                return new UnderlyingLegData
                {
                    Quantity = quantity,
                    OrderType = orderType,
                    OrderPrice = orderPrice ?? 0m
                };
            }

            /// <summary>
            /// Invokes the <paramref name="underlyingHandler"/>
            /// </summary>
            public override void Invoke(Action<UnderlyingLegData> underlyingHandler, Action<OptionLegData> optionHandler)
            {
                underlyingHandler(this);
            }
        }
    }
}
