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
using QuantConnect.Orders;
using System.Collections.Generic;
using System.Linq;

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
        /// The canonical Option symbol of the strategy
        /// </summary>
        public Symbol CanonicalOption { get; set; }

        /// <summary>
        /// Underlying symbol of the strategy
        /// </summary>
        public Symbol Underlying { get; set; }

        /// <summary>
        /// Option strategy legs
        /// </summary>
        public List<OptionLegData> OptionLegs { get; set; }

        /// <summary>
        /// Option strategy underlying legs (usually 0 or 1 legs)
        /// </summary>
        public List<UnderlyingLegData> UnderlyingLegs { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="OptionStrategy"/> with the specified parameters
        /// </summary>
        /// <param name="name">The strategy name</param>
        /// <param name="canonicalSymbol">The canonical option symbol</param>
        /// <param name="optionLegs">The option legs data</param>
        /// <param name="underlyingLegs">The underlying legs data</param>
        public OptionStrategy(string name, Symbol canonicalSymbol, List<OptionLegData> optionLegs = null, List<UnderlyingLegData> underlyingLegs = null)
        {
            Name = name;
            CanonicalOption = canonicalSymbol;
            Underlying = canonicalSymbol.Underlying;
            OptionLegs = optionLegs ?? new List<OptionLegData>();
            UnderlyingLegs = underlyingLegs ?? new List<UnderlyingLegData>();

            SetSymbols();
        }

        /// <summary>
        /// Creates a new instance of <see cref="OptionStrategy"/> with default parameters
        /// </summary>
        public OptionStrategy()
        {
            OptionLegs = new List<OptionLegData>();
            UnderlyingLegs = new List<UnderlyingLegData>();
        }

        /// <summary>
        /// Sets the option legs symbols based on the canonical symbol and the leg data. 
        /// If the canonical symbol is not set, it will be created using the underlying symbol.
        /// </summary>
        public void SetSymbols()
        {
            if (CanonicalOption == null)
            {
                if (Underlying == null)
                {
                    // Let's be polite and try to get the underlying symbol from the underlying legs as a last resort
                    var underlyingLeg = UnderlyingLegs.Count > 0 ? UnderlyingLegs[0] : null;
                    if (underlyingLeg == null || underlyingLeg.Symbol == null)
                    {
                        return;
                    }

                    Underlying = underlyingLeg.Symbol;
                }

                CanonicalOption = Symbol.CreateCanonicalOption(Underlying);
            }

            foreach (var optionLeg in OptionLegs.Where(leg => leg.Symbol == null))
            {
                var targetOption = CanonicalOption.ID.Symbol;
                optionLeg.Symbol = Symbol.CreateOption(Underlying, targetOption, Underlying.ID.Market, CanonicalOption.ID.OptionStyle,
                    optionLeg.Right, optionLeg.Strike, optionLeg.Expiration);
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="OptionStrategy"/> with the specified name and legs data.
        /// The method will try to infer the canonical symbol and underlying symbol from the legs data, but they can also be set manually after the strategy creation.
        /// </summary>
        public static OptionStrategy Create(string name, IEnumerable<Leg> legs)
        {
            var underlyingLegs = new List<UnderlyingLegData>();
            var optionLegs = new List<OptionLegData>();
            Symbol canonicalSymbol = null;

            foreach (var leg in legs)
            {
                if (leg is UnderlyingLegData underlyingLeg)
                {
                    underlyingLegs.Add(underlyingLeg);
                }
                else if (leg is OptionLegData optionLeg)
                {
                    optionLegs.Add(optionLeg);

                    if (canonicalSymbol == null)
                    {
                        canonicalSymbol = optionLeg.Symbol.Canonical;
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid leg type: {leg.GetType().FullName}");
                }
            }

            return new OptionStrategy(name, canonicalSymbol, optionLegs, underlyingLegs);
        }

        /// <summary>
        /// This class is a POCO containing basic data for the option legs of the strategy
        /// </summary>
        public class OptionLegData : Leg
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
            public static OptionLegData Create(int quantity, Symbol symbol, decimal? orderPrice = null)
            {
                return new OptionLegData
                {
                    Symbol = symbol,
                    Quantity = quantity,
                    Expiration = symbol.ID.Date,
                    OrderPrice = orderPrice,
                    Right = symbol.ID.OptionRight,
                    Strike = symbol.ID.StrikePrice
                };
            }

            /// <summary>
            /// Returns a string that represents the option leg
            /// </summary>
            public override string ToString()
            {
                return $"Leg: {Quantity}. Right: {Right}. Strike: {Strike}. Expiration: {Expiration:yyyyMMdd}";
            }
        }

        /// <summary>
        /// This class is a POCO containing basic data for the underlying leg of the strategy
        /// </summary>
        public class UnderlyingLegData : Leg
        {
            /// <summary>
            /// Creates a new instance of <see cref="UnderlyingLegData"/> for the specified <paramref name="quantity"/> of underlying shares.
            /// </summary>
            public static UnderlyingLegData Create(int quantity, Symbol symbol, decimal? orderPrice = null)
            {
                var data = Create(quantity, orderPrice);
                data.Symbol = symbol;
                return data;
            }

            /// <summary>
            /// Creates a new instance of <see cref="UnderlyingLegData"/> for the specified <paramref name="quantity"/> of underlying shares.
            /// </summary>
            public static UnderlyingLegData Create(int quantity, decimal? orderPrice = null)
            {
                return new UnderlyingLegData
                {
                    Quantity = quantity,
                    OrderPrice = orderPrice
                };
            }

            /// <summary>
            /// Returns a string that represents the underlying leg.
            /// </summary>
            public override string ToString()
            {
                return Symbol != null ? $"Leg: {Quantity}. {Symbol}" : string.Empty;
            }
        }
    }
}
