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
        /// Symbol properties database to use to get contract multipliers
        /// </summary>
        private static SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

        /// <summary>
        /// Creates a Covered Call strategy that consists of selling one call contract and buying 1 lot of the underlying.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the call option contract</param>
        /// <param name="expiration">The expiration date for the call option contract</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy CoveredCall(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            CheckCanonicalOptionSymbol(canonicalOption, "CoveredCall");
            CheckExpirationDate(expiration, "CoveredCall", nameof(expiration));

            var underlyingQuantity = (int)_symbolPropertiesDatabase.GetSymbolProperties(canonicalOption.ID.Market, canonicalOption,
                canonicalOption.SecurityType, "").ContractMultiplier;

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.CoveredCall.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = strike, Quantity = -1, Expiration = expiration
                    }
                },
                UnderlyingLegs = new List<OptionStrategy.UnderlyingLegData>
                {
                    new OptionStrategy.UnderlyingLegData
                    {
                        Quantity = underlyingQuantity, Symbol = canonicalOption.Underlying
                    }
                }
            };
        }

        /// <summary>
        /// Creates a Protective Call strategy that consists of buying one call contract and selling 1 lot of the underlying.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the call option contract</param>
        /// <param name="expiration">The expiration date for the call option contract</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ProtectiveCall(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            // Since a protective call is an inverted covered call, we can just use the CoveredCall method and invert the legs
            return InvertStrategy(CoveredCall(canonicalOption, strike, expiration), OptionStrategyDefinitions.ProtectiveCall.Name);
        }

        /// <summary>
        /// Creates a Covered Put strategy that consists of selling 1 put contract and 1 lot of the underlying.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the put option contract</param>
        /// <param name="expiration">The expiration date for the put option contract</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy CoveredPut(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            CheckCanonicalOptionSymbol(canonicalOption, "CoveredPut");
            CheckExpirationDate(expiration, "CoveredPut", nameof(expiration));

            var underlyingQuantity = -(int)_symbolPropertiesDatabase.GetSymbolProperties(canonicalOption.ID.Market, canonicalOption,
                canonicalOption.SecurityType, "").ContractMultiplier;

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.CoveredPut.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = strike, Quantity = -1, Expiration = expiration
                    }
                },
                UnderlyingLegs = new List<OptionStrategy.UnderlyingLegData>
                {
                    new OptionStrategy.UnderlyingLegData
                    {
                        Quantity = underlyingQuantity, Symbol = canonicalOption.Underlying
                    }
                }
            };
        }

        /// <summary>
        /// Creates a Protective Put strategy that consists of buying 1 put contract and 1 lot of the underlying.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the put option contract</param>
        /// <param name="expiration">The expiration date for the put option contract</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ProtectivePut(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            // Since a protective put is an inverted covered put, we can just use the CoveredPut method and invert the legs
            return InvertStrategy(CoveredPut(canonicalOption, strike, expiration), OptionStrategyDefinitions.ProtectivePut.Name);
        }

        /// <summary>
        /// Creates a Protective Collar strategy that consists of buying 1 put contract and 1 lot of the underlying.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="callStrike">The strike price for the call option contract</param>
        /// <param name="putStrike">The strike price for the put option contract</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ProtectiveCollar(Symbol canonicalOption, decimal callStrike, decimal putStrike, DateTime expiration)
        {
            if (callStrike < putStrike)
            {
                throw new ArgumentException("ProtectiveCollar: callStrike must be greater than putStrike", $"{nameof(callStrike)}, {nameof(putStrike)}");
            }

            // Since a protective collar is a combination of protective put and covered call
            var coveredCall = CoveredCall(canonicalOption, callStrike, expiration);
            var protectivePut = ProtectivePut(canonicalOption, putStrike, expiration);

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.ProtectiveCollar.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = coveredCall.OptionLegs.Concat(protectivePut.OptionLegs).ToList(),
                UnderlyingLegs = coveredCall.UnderlyingLegs     // only 1 lot of long stock position
            };
        }

        /// <summary>
        /// Creates a Conversion strategy that consists of buying 1 put contract, 1 lot of the underlying and selling 1 call contract.
        /// Put and call must have the same expiration date, underlying (multiplier), and strike price.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the call and put option contract</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy Conversion(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            var strategy = ProtectiveCollar(canonicalOption, strike, strike, expiration);
            strategy.Name = OptionStrategyDefinitions.Conversion.Name;
            return strategy;
        }

        /// <summary>
        /// Creates a Reverse Conversion strategy that consists of buying 1 put contract and 1 lot of the underlying.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the put option contract</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ReverseConversion(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            // Since a reverse conversion is an inverted conversion, we can just use the Conversion method and invert the legs
            return InvertStrategy(Conversion(canonicalOption, strike, expiration), OptionStrategyDefinitions.ReverseConversion.Name);
        }

        /// <summary>
        /// Creates a Naked Call strategy that consists of selling 1 call contract.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the call option contract</param>
        /// <param name="expiration">The expiration date for the call option contract</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy NakedCall(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            CheckCanonicalOptionSymbol(canonicalOption, "NakedCall");
            CheckExpirationDate(expiration, "NakedCall", nameof(expiration));

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.NakedCall.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = strike, Quantity = -1, Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Creates a Naked Put strategy that consists of selling 1 put contract.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the put option contract</param>
        /// <param name="expiration">The expiration date for the put option contract</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy NakedPut(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            CheckCanonicalOptionSymbol(canonicalOption, "NakedPut");
            CheckExpirationDate(expiration, "NakedPut", nameof(expiration));

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.NakedPut.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = strike, Quantity = -1, Expiration = expiration
                    }
                }
            };
        }

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
            CheckCanonicalOptionSymbol(canonicalOption, "BearCallSpread");
            CheckExpirationDate(expiration, "BearCallSpread", nameof(expiration));

            if (leg1Strike >= leg2Strike)
            {
                throw new ArgumentException("BearCallSpread: leg1Strike must be less than leg2Strike", $"{nameof(leg1Strike)}, {nameof(leg2Strike)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.BearCallSpread.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg1Strike, Quantity = -1, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg2Strike, Quantity = 1, Expiration = expiration
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
            CheckCanonicalOptionSymbol(canonicalOption, "BearPutSpread");
            CheckExpirationDate(expiration, "BearPutSpread", nameof(expiration));

            if (leg1Strike <= leg2Strike)
            {
                throw new ArgumentException("BearPutSpread: leg1Strike must be greater than leg2Strike", $"{nameof(leg1Strike)}, {nameof(leg2Strike)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.BearPutSpread.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg1Strike, Quantity = 1,
                        Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg2Strike, Quantity = -1, Expiration = expiration
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
            CheckCanonicalOptionSymbol(canonicalOption, "BullCallSpread");
            CheckExpirationDate(expiration, "BullCallSpread", nameof(expiration));

            if (leg1Strike >= leg2Strike)
            {
                throw new ArgumentException("BullCallSpread: leg1Strike must be less than leg2Strike", $"{nameof(leg1Strike)}, {nameof(leg2Strike)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.BullCallSpread.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg1Strike, Quantity = 1, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = leg2Strike, Quantity = -1, Expiration = expiration
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
            CheckCanonicalOptionSymbol(canonicalOption, "BullPutSpread");
            CheckExpirationDate(expiration, "BullPutSpread", nameof(expiration));

            if (leg1Strike <= leg2Strike)
            {
                throw new ArgumentException("BullPutSpread: leg1Strike must be greater than leg2Strike", $"{nameof(leg1Strike)}, {nameof(leg2Strike)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.BullPutSpread.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg1Strike, Quantity = -1, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = leg2Strike, Quantity = 1,
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
            CheckCanonicalOptionSymbol(canonicalOption, "Straddle");
            CheckExpirationDate(expiration, "Straddle", nameof(expiration));

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.Straddle.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = strike, Quantity = 1,
                        Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = strike, Quantity = 1,
                        Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Creates a Short Straddle strategy that consists of selling a call and a put, both with the same strike price and expiration.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price for the option contracts</param>
        /// <param name="expiration">The expiration date for the option contracts</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ShortStraddle(Symbol canonicalOption, decimal strike, DateTime expiration)
        {
            // Since a short straddle is an inverted straddle, we can just use the Straddle method and invert the legs
            return InvertStrategy(Straddle(canonicalOption, strike, expiration), OptionStrategyDefinitions.ShortStraddle.Name);
        }

        /// <summary>
        /// Method creates new Strangle strategy, that buying a call option and a put option with the same expiration date
        /// The strike price of the call is above the strike of the put.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="callLegStrike">The strike price of the long call</param>
        /// <param name="putLegStrike">The strike price of the long put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy Strangle(
            Symbol canonicalOption,
            decimal callLegStrike,
            decimal putLegStrike,
            DateTime expiration
            )
        {
            CheckCanonicalOptionSymbol(canonicalOption, "Strangle");
            CheckExpirationDate(expiration, "Strangle", nameof(expiration));

            if (callLegStrike <= putLegStrike)
            {
                throw new ArgumentException($"Strangle: {nameof(callLegStrike)} must be greater than {nameof(putLegStrike)}",
                    $"{nameof(callLegStrike)}, {nameof(putLegStrike)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.Strangle.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = callLegStrike, Quantity = 1, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = putLegStrike, Quantity = 1, Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Creates a Short Strangle strategy that consists of selling a call and a put, with the same expiration date and
        /// the call strike being above the put strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="callLegStrike">The strike price of the short call</param>
        /// <param name="putLegStrike">The strike price of the short put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ShortStrangle(Symbol canonicalOption, decimal callLegStrike, decimal putLegStrike, DateTime expiration)
        {
            // Since a short strangle is an inverted strangle, we can just use the Strangle method and invert the legs
            return InvertStrategy(Strangle(canonicalOption, callLegStrike, putLegStrike, expiration), OptionStrategyDefinitions.ShortStrangle.Name);
        }

        /// <summary>
        /// Method creates new Call Butterfly strategy, that consists of two short calls at a middle strike, and one long call each at a lower and upper strike.
        /// The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="higherStrike">The upper strike price of the long call</param>
        /// <param name="middleStrike">The middle strike price of the two short calls</param>
        /// <param name="lowerStrike">The lower strike price of the long call</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy CallButterfly(
            Symbol canonicalOption,
            decimal higherStrike,
            decimal middleStrike,
            decimal lowerStrike,
            DateTime expiration
            )
        {
            CheckCanonicalOptionSymbol(canonicalOption, "CallButterfly");
            CheckExpirationDate(expiration, "CallButterfly", nameof(expiration));

            if (higherStrike <= middleStrike ||
                lowerStrike >= middleStrike ||
                higherStrike - middleStrike != middleStrike - lowerStrike)
            {
                throw new ArgumentException("ButterflyCall: upper and lower strikes must both be equidistant from the middle strike",
                    $"{nameof(higherStrike)}, {nameof(middleStrike)}, {nameof(lowerStrike)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.ButterflyCall.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = higherStrike, Quantity = 1, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = middleStrike, Quantity = -2, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = lowerStrike, Quantity = 1, Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new Butterfly Call strategy that consists of two short calls at a middle strike,
        /// and one long call each at a lower and upper strike.
        /// The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="higherStrike">The upper strike price of the long call</param>
        /// <param name="middleStrike">The middle strike price of the two short calls</param>
        /// <param name="lowerStrike">The lower strike price of the long call</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        /// <remarks>Alias for <see cref="CallButterfly" /></remarks>
        public static OptionStrategy ButterflyCall(Symbol canonicalOption, decimal higherStrike, decimal middleStrike, decimal lowerStrike,
            DateTime expiration)
        {
            return CallButterfly(canonicalOption, higherStrike, middleStrike, lowerStrike, expiration);
        }

        /// <summary>
        /// Creates a new Butterfly Call strategy that consists of two long calls at a middle strike,
        /// and one short call each at a lower and upper strike.
        /// The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="higherStrike">The upper strike price of the short call</param>
        /// <param name="middleStrike">The middle strike price of the two long calls</param>
        /// <param name="lowerStrike">The lower strike price of the short call</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ShortButterflyCall(Symbol canonicalOption, decimal higherStrike, decimal middleStrike, decimal lowerStrike,
            DateTime expiration)
        {
            // Since a short butterfly call is an inverted butterfly call, we can just use the ButterflyCall method and invert the legs
            return InvertStrategy(ButterflyCall(canonicalOption, higherStrike, middleStrike, lowerStrike, expiration),
                OptionStrategyDefinitions.ShortButterflyCall.Name);
        }

        /// <summary>
        /// Method creates new Put Butterfly strategy, that consists of two short puts at a middle strike, and one long put each at a lower and upper strike.
        /// The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="higherStrike">The upper strike price of the long put</param>
        /// <param name="middleStrike">The middle strike price of the two short puts</param>
        /// <param name="lowerStrike">The lower strike price of the long put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy PutButterfly(
            Symbol canonicalOption,
            decimal higherStrike,
            decimal middleStrike,
            decimal lowerStrike,
            DateTime expiration
            )
        {
            CheckCanonicalOptionSymbol(canonicalOption, "PutButterfly");
            CheckExpirationDate(expiration, "PutButterfly", nameof(expiration));

            if (higherStrike <= middleStrike ||
                lowerStrike >= middleStrike ||
                higherStrike - middleStrike != middleStrike - lowerStrike)
            {
                throw new ArgumentException("ButterflyPut: upper and lower strikes must both be equidistant from the middle strike",
                    $"{nameof(higherStrike)}, {nameof(middleStrike)}, {nameof(lowerStrike)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.ButterflyPut.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = higherStrike, Quantity = 1,
                        Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = middleStrike, Quantity = -2,
                        Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = lowerStrike, Quantity = 1,
                        Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Creates a new Butterfly Put strategy that consists of two short puts at a middle strike,
        /// and one long put each at a lower and upper strike.
        /// The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="higherStrike">The upper strike price of the long put</param>
        /// <param name="middleStrike">The middle strike price of the two short puts</param>
        /// <param name="lowerStrike">The lower strike price of the long put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        /// <remarks>Alias for <see cref="PutButterfly" /></remarks>
        public static OptionStrategy ButterflyPut(Symbol canonicalOption, decimal higherStrike, decimal middleStrike, decimal lowerStrike,
            DateTime expiration)
        {
            return PutButterfly(canonicalOption, higherStrike, middleStrike, lowerStrike, expiration);
        }

        /// <summary>
        /// Creates a new Butterfly Put strategy that consists of two long puts at a middle strike,
        /// and one short put each at a lower and upper strike.
        /// The upper and lower strikes must both be equidistant from the middle strike.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="higherStrike">The upper strike price of the short put</param>
        /// <param name="middleStrike">The middle strike price of the two long puts</param>
        /// <param name="lowerStrike">The lower strike price of the short put</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ShortButterflyPut(Symbol canonicalOption, decimal higherStrike, decimal middleStrike, decimal lowerStrike,
            DateTime expiration)
        {
            // Since a short butterfly put is an inverted butterfly put, we can just use the ButterflyPut method and invert the legs
            return InvertStrategy(ButterflyPut(canonicalOption, higherStrike, middleStrike, lowerStrike, expiration),
                OptionStrategyDefinitions.ShortButterflyPut.Name);
        }

        /// <summary>
        /// Creates new Call Calendar Spread strategy which consists of a short and a long call
        /// with the same strikes but with the long call having a further expiration date.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price of the both legs</param>
        /// <param name="nearExpiration">Near expiration date for the short option</param>
        /// <param name="farExpiration">Far expiration date for the long option</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy CallCalendarSpread(Symbol canonicalOption, decimal strike, DateTime nearExpiration, DateTime farExpiration)
        {
            CheckCanonicalOptionSymbol(canonicalOption, "CallCalendarSpread");
            CheckExpirationDate(nearExpiration, "CallCalendarSpread", nameof(nearExpiration));
            CheckExpirationDate(farExpiration, "CallCalendarSpread", nameof(farExpiration));

            if (nearExpiration >= farExpiration)
            {
                throw new ArgumentException("CallCalendarSpread: near expiration must be less than far expiration",
                    $"{nameof(nearExpiration)}, {nameof(farExpiration)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.CallCalendarSpread.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = strike, Quantity = -1, Expiration = nearExpiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = strike, Quantity = 1, Expiration = farExpiration
                    }
                }
            };
        }

        /// <summary>
        /// Creates new Short Call Calendar Spread strategy which consists of a short and a long call
        /// with the same strikes but with the short call having a further expiration date.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price of the both legs</param>
        /// <param name="nearExpiration">Near expiration date for the long option</param>
        /// <param name="farExpiration">Far expiration date for the short option</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ShortCallCalendarSpread(Symbol canonicalOption, decimal strike, DateTime nearExpiration, DateTime farExpiration)
        {
            // Since a short call calendar spread is an inverted call calendar, we can just use the CallCalendarSpread method and invert the legs
            return InvertStrategy(CallCalendarSpread(canonicalOption, strike, nearExpiration, farExpiration),
                OptionStrategyDefinitions.ShortCallCalendarSpread.Name);
        }

        /// <summary>
        /// Creates new Put Calendar Spread strategy which consists of a short and a long put
        /// with the same strikes but with the long put having a further expiration date.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price of the both legs</param>
        /// <param name="nearExpiration">Near expiration date for the short option</param>
        /// <param name="farExpiration">Far expiration date for the long option</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy PutCalendarSpread(Symbol canonicalOption, decimal strike, DateTime nearExpiration, DateTime farExpiration)
        {
            CheckCanonicalOptionSymbol(canonicalOption, "PutCalendarSpread");
            CheckExpirationDate(nearExpiration, "PutCalendarSpread", nameof(nearExpiration));
            CheckExpirationDate(farExpiration, "PutCalendarSpread", nameof(farExpiration));

            if (nearExpiration >= farExpiration)
            {
                throw new ArgumentException("PutCalendarSpread: near expiration must be less than far expiration",
                    $"{nameof(nearExpiration)}, {nameof(farExpiration)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.PutCalendarSpread.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = strike, Quantity = -1, Expiration = nearExpiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = strike, Quantity = 1, Expiration = farExpiration
                    }
                }
            };
        }

        /// <summary>
        /// Creates new Short Put Calendar Spread strategy which consists of a short and a long put
        /// with the same strikes but with the short put having a further expiration date.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="strike">The strike price of the both legs</param>
        /// <param name="nearExpiration">Near expiration date for the long option</param>
        /// <param name="farExpiration">Far expiration date for the short option</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ShortPutCalendarSpread(Symbol canonicalOption, decimal strike, DateTime nearExpiration, DateTime farExpiration)
        {
            // Since a short put calendar spread is an inverted put calendar, we can just use the PutCalendarSpread method and invert the legs
            return InvertStrategy(PutCalendarSpread(canonicalOption, strike, nearExpiration, farExpiration),
                OptionStrategyDefinitions.ShortPutCalendarSpread.Name);
        }

        /// <summary>
        /// Creates a new Iron Condor strategy which consists of a long put, a short put, a short call and a long option,
        /// all with the same expiration date and with increasing strikes prices in the mentioned order.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="longPutStrike">Long put option strike price</param>
        /// <param name="shortPutStrike">Short put option strike price</param>
        /// <param name="shortCallStrike">Short call option strike price</param>
        /// <param name="longCallStrike">Long call option strike price</param>
        /// <param name="expiration">Expiration date for all the options</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy IronCondor(Symbol canonicalOption, decimal longPutStrike, decimal shortPutStrike, decimal shortCallStrike,
            decimal longCallStrike, DateTime expiration)
        {
            CheckCanonicalOptionSymbol(canonicalOption, "IronCondor");
            CheckExpirationDate(expiration, "IronCondor", nameof(expiration));

            if (longPutStrike >= shortPutStrike || shortPutStrike >= shortCallStrike || shortCallStrike >= longCallStrike)
            {
                throw new ArgumentException("IronCondor: strike prices must be in ascending order",
                    $"{nameof(longPutStrike)}, {nameof(shortPutStrike)}, {nameof(shortCallStrike)}, {nameof(longCallStrike)}");
            }

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.IronCondor.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = new List<OptionStrategy.OptionLegData>
                {
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = longPutStrike, Quantity = 1, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Put, Strike = shortPutStrike, Quantity = -1, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = shortCallStrike, Quantity = -1, Expiration = expiration
                    },
                    new OptionStrategy.OptionLegData
                    {
                        Right = OptionRight.Call, Strike = longCallStrike, Quantity = 1, Expiration = expiration
                    }
                }
            };
        }

        /// <summary>
        /// Creates a Box Spread strategy which consists of a long call and a short put (buy side) of the same strikes,
        /// coupled with a short call and a long put (sell side) of higher but same strikes. All options have the same expiry.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="higherStrike">The strike price of the sell side legs</param>
        /// <param name="lowerStrike">The strike price of the buy side legs</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy BoxSpread(Symbol canonicalOption, decimal higherStrike, decimal lowerStrike, DateTime expiration)
        {
            if (higherStrike <= lowerStrike)
            {
                throw new ArgumentException($"BoxSpread: strike prices must be in descending order, {nameof(higherStrike)}, {nameof(lowerStrike)}");
            }

            // It is a combination of a BearPutSpread and a BullCallSpread with the same expiry and strikes
            var bearPutSpread = BearPutSpread(canonicalOption, higherStrike, lowerStrike, expiration);
            var bullCallSpread = BullCallSpread(canonicalOption, lowerStrike, higherStrike, expiration);

            return new OptionStrategy
            {
                Name = OptionStrategyDefinitions.BoxSpread.Name,
                Underlying = canonicalOption.Underlying,
                CanonicalOption = canonicalOption,
                OptionLegs = bearPutSpread.OptionLegs.Concat(bullCallSpread.OptionLegs).ToList()
            };
        }

        /// <summary>
        /// Creates a Short Box Spread strategy which consists of a long call and a short put (buy side) of the same strikes,
        /// coupled with a short call and a long put (sell side) of lower but same strikes. All options have the same expiry.
        /// </summary>
        /// <param name="canonicalOption">Option symbol</param>
        /// <param name="higherStrike">The strike price of the buy side</param>
        /// <param name="lowerStrike">The strike price of the sell side</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>Option strategy specification</returns>
        public static OptionStrategy ShortBoxSpread(Symbol canonicalOption, decimal higherStrike, decimal lowerStrike, DateTime expiration)
        {
            // Since a short box spread is an inverted box spread, we can just use the BoxSpread method and invert the legs
            return InvertStrategy(BoxSpread(canonicalOption, higherStrike, lowerStrike, expiration), OptionStrategyDefinitions.ShortBoxSpread.Name);
        }

        /// <summary>
        /// Checks that canonical option symbol is valid
        /// </summary>
        private static void CheckCanonicalOptionSymbol(Symbol canonicalOption, string strategyName)
        {
            if (!canonicalOption.HasUnderlying || canonicalOption.ID.StrikePrice != 0.0m)
            {
                throw new ArgumentException($"{strategyName}: canonicalOption must contain canonical option symbol", nameof(canonicalOption));
            }
        }

        /// <summary>
        /// Checks that expiration date is valid
        /// </summary>
        private static void CheckExpirationDate(DateTime expiration, string strategyName, string parameterName)
        {
            if (expiration == DateTime.MaxValue || expiration == DateTime.MinValue)
            {
                throw new ArgumentException($"{strategyName}: expiration must contain expiration date", parameterName);
            }
        }

        /// <summary>
        /// Inverts the given strategy by multiplying all legs' quantities by -1 and changing the strategy name.
        /// </summary>
        private static OptionStrategy InvertStrategy(OptionStrategy strategy, string invertedStrategyName)
        {
            strategy.Name = invertedStrategyName;
            foreach (var leg in strategy.OptionLegs.Cast<OptionStrategy.LegData>().Concat(strategy.UnderlyingLegs))
            {
                leg.Quantity *= -1;
            }

            return strategy;
        }
    }
}
