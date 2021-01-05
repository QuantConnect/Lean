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
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Provides methods for creating popular <see cref="OptionStrategy"/> instances.
    /// These strategies can be directly bought and sold via:
    ///     QCAlgorithm.Buy(OptionStrategy strategy, int quantity)
    ///     QCAlgorithm.Sell(OptionStrategy strategy, int quantity)
    ///
    /// See also <see cref="OptionStrategyDefinitions"/>
    /// </summary>
    public static class OptionStrategies
    {
        /// <summary>
        /// Method creates new Bear Call Spread strategy, that consists of two calls with the same expiration but different strikes.
        /// The strike price of the short call is below the strike of the long call. This is a credit spread.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="leg1Strike">The strike price of the short call</param>
        /// <param name="leg2Strike">The strike price of the long call</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy BearCallSpread(
            Symbol canonicalOption,
            decimal leg1Strike,
            decimal leg2Strike,
            DateTime expiration
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("BearCallSpread: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (leg1Strike >= leg2Strike)
            {
                throw new ArgumentException("BearCallSpread: leg1Strike must be less than leg2Strike", "leg1Strike, leg2Strike");
            }

            if (expiration == DateTime.MaxValue ||
                expiration == DateTime.MinValue)
            {
                throw new ArgumentException("BearCallSpread: expiration must contain expiration date", nameof(expiration));
            }

            return new OptionStrategy
            {
                Name = "Bear Call Spread",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg1Strike, Quantity = -1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg2Strike, Quantity = 1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Bear Put Spread strategy, that consists of two puts with the same expiration but different strikes.
        /// The strike price of the short put is below the strike of the long put. This is a debit spread.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="leg1Strike">The strike price of the long put</param>
        /// <param name="leg2Strike">The strike price of the short put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy BearPutSpread(
            Symbol canonicalOption,
            decimal leg1Strike,
            decimal leg2Strike,
            DateTime expiration
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("BearPutSpread: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (leg1Strike <= leg2Strike)
            {
                throw new ArgumentException("BearPutSpread: leg1Strike must be greater than leg2Strike", "leg1Strike, leg2Strike");
            }

            if (expiration == DateTime.MaxValue ||
                expiration == DateTime.MinValue)
            {
                throw new ArgumentException("BearPutSpread: expiration must contain expiration date", nameof(expiration));
            }

            return new OptionStrategy
            {
                Name = "Bear Put Spread",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg1Strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg2Strike, Quantity = -1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Bull Call Spread strategy, that consists of two calls with the same expiration but different strikes.
        /// The strike price of the short call is higher than the strike of the long call. This is a debit spread.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="leg1Strike">The strike price of the long call</param>
        /// <param name="leg2Strike">The strike price of the short call</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy BullCallSpread(
            Symbol canonicalOption,
            decimal leg1Strike,
            decimal leg2Strike,
            DateTime expiration
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("BullCallSpread: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (leg1Strike >= leg2Strike)
            {
                throw new ArgumentException("BullCallSpread: leg1Strike must be less than leg2Strike", "leg1Strike, leg2Strike");
            }

            if (expiration == DateTime.MaxValue ||
                expiration == DateTime.MinValue)
            {
                throw new ArgumentException("BullCallSpread: expiration must contain expiration date", nameof(expiration));
            }

            return new OptionStrategy
            {
                Name = "Bull Call Spread",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg1Strike, Quantity = 1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg2Strike, Quantity = -1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Bull Put Spread strategy, that consists of two puts with the same expiration but different strikes.
        /// The strike price of the short put is above the strike of the long put. This is a credit spread.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="leg1Strike">The strike price of the short put</param>
        /// <param name="leg2Strike">The strike price of the long put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy BullPutSpread(
            Symbol canonicalOption,
            decimal leg1Strike,
            decimal leg2Strike,
            DateTime expiration
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("BullPutSpread: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (leg1Strike <= leg2Strike)
            {
                throw new ArgumentException("BullPutSpread: leg1Strike must be greater than leg2Strike", "leg1Strike, leg2Strike");
            }

            if (expiration == DateTime.MaxValue ||
                expiration == DateTime.MinValue)
            {
                throw new ArgumentException("BullPutSpread: expiration must contain expiration date", nameof(expiration));
            }

            return new OptionStrategy
            {
                Name = "Bull Put Spread",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg1Strike, Quantity = -1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg2Strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Straddle strategy, that is a combination of buying a call and buying a put, both with the same strike price and expiration.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price of the both legs</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy Straddle(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("Straddle: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (expiration == DateTime.MaxValue ||
                expiration == DateTime.MinValue)
            {
                throw new ArgumentException("Straddle: expiration must contain expiration date", nameof(expiration));
            }

            return new OptionStrategy
            {
                Name = "Straddle",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Strangle strategy, that buying a call option and a put option with the same expiration date.
        /// The strike price of the call is above the strike of the put.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="leg1Strike">The strike price of the long call</param>
        /// <param name="leg2Strike">The strike price of the long put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy Strangle(
            Symbol canonicalOption,
            decimal leg1Strike,
            decimal leg2Strike,
            DateTime expiration
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("Strangle: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (leg1Strike <= leg2Strike)
            {
                throw new ArgumentException("Strangle: leg1Strike must be greater than leg2Strike", "leg1Strike, leg2Strike");
            }

            if (expiration == DateTime.MaxValue ||
                expiration == DateTime.MinValue)
            {
                throw new ArgumentException("Strangle: expiration must contain expiration date", nameof(expiration));
            }

            return new OptionStrategy
            {
                Name = "Strangle",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg1Strike, Quantity = 1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg2Strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Call Butterfly strategy, that consists of two short calls at a middle strike, and one long call each at a lower and upper strike.
        /// The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="leg1Strike">The upper strike price of the long call</param>
        /// <param name="leg2Strike">The middle strike price of the two short calls</param>
        /// <param name="leg3Strike">The lower strike price of the long call</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy CallButterfly(
            Symbol canonicalOption,
            decimal leg1Strike,
            decimal leg2Strike,
            decimal leg3Strike,
            DateTime expiration
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("CallButterfly: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (leg1Strike <= leg2Strike ||
                leg3Strike >= leg2Strike ||
                leg1Strike - leg2Strike != leg2Strike - leg3Strike)
            {
                throw new ArgumentException("CallButterfly: upper and lower strikes must both be equidistant from the middle strike", "leg1Strike, leg2Strike, leg3Strike");
            }

            if (expiration == DateTime.MaxValue ||
                expiration == DateTime.MinValue)
            {
                throw new ArgumentException("CallButterfly: expiration must contain expiration date", nameof(expiration));
            }

            return new OptionStrategy
            {
                Name = "Call Butterfly",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg1Strike, Quantity = 1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg2Strike, Quantity = -2,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg3Strike, Quantity = 1,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Put Butterfly strategy, that consists of two short puts at a middle strike, and one long put each at a lower and upper strike.
        /// The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="leg1Strike">The upper strike price of the long put</param>
        /// <param name="leg2Strike">The middle strike price of the two short puts</param>
        /// <param name="leg3Strike">The lower strike price of the long put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy PutButterfly(
            Symbol canonicalOption,
            decimal leg1Strike,
            decimal leg2Strike,
            decimal leg3Strike,
            DateTime expiration
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("PutButterfly: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (leg1Strike <= leg2Strike ||
                leg3Strike >= leg2Strike ||
                leg1Strike - leg2Strike != leg2Strike - leg3Strike)
            {
                throw new ArgumentException("PutButterfly: upper and lower strikes must both be equidistant from the middle strike", "leg1Strike, leg2Strike, leg3Strike");
            }

            if (expiration == DateTime.MaxValue ||
                expiration == DateTime.MinValue)
            {
                throw new ArgumentException("PutButterfly: expiration must contain expiration date", nameof(expiration));
            }

            return new OptionStrategy
            {
                Name = "Put Butterfly",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg1Strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg2Strike, Quantity = -2,
                        OrderType = Orders.OrderType.Market, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg3Strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Call Calendar Spread strategy, that is a short one call option and long a second call option with a more distant expiration.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price of the both legs</param>
        /// <param name="expiration1">Option expiration near date</param>
        /// <param name="expiration2">Option expiration far date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy CallCalendarSpread(
            Symbol canonicalOption,
            decimal strike,
            DateTime expiration1,
            DateTime expiration2
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("CallCalendarSpread: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (expiration1 == DateTime.MaxValue ||
                expiration1 == DateTime.MinValue ||
                expiration2 == DateTime.MaxValue ||
                expiration2 == DateTime.MinValue)
            {
                throw new ArgumentException("CallCalendarSpread: expiration must contain expiration date", "expiration1, expiration2");
            }

            if (expiration1 >= expiration2)
            {
                throw new ArgumentException("CallCalendarSpread: near expiration must be less than far expiration", "expiration1, expiration2");
            }

            return new OptionStrategy
            {
                Name = "Call Calendar Spread",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = strike, Quantity = -1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration1
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration2
                    }
                }
            };
        }

        /// <summary>
        /// Method creates new Put Calendar Spread strategy, that is a short one put option and long a second put option with a more distant expiration.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price of the both legs</param>
        /// <param name="expiration1">Option expiration near date</param>
        /// <param name="expiration2">Option expiration far date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy PutCalendarSpread(
            Symbol canonicalOption,
            decimal strike,
            DateTime expiration1,
            DateTime expiration2
            )
        {
            if (!canonicalOption.HasUnderlying ||
                canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException("PutCalendarSpread: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }

            if (expiration1 == DateTime.MaxValue ||
                expiration1 == DateTime.MinValue ||
                expiration2 == DateTime.MaxValue ||
                expiration2 == DateTime.MinValue)
            {
                throw new ArgumentException("PutCalendarSpread: expiration must contain expiration date", "expiration1, expiration2");
            }

            if (expiration1 >= expiration2)
            {
                throw new ArgumentException("PutCalendarSpread: near expiration must be less than far expiration", "expiration1, expiration2");
            }

            return new OptionStrategy
            {
                Name = "Put Calendar Spread",
                Underlying = canonicalOption.Underlying,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = strike, Quantity = -1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration1
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = strike, Quantity = 1, OrderType = Orders.OrderType.Market,
                        Expiration = expiration2
                    }
                }
            };
        }
    }
}
