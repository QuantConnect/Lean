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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.Securities.CurrencyConversion
{
    /// <summary>
    /// Provides an implementation of <see cref="ICurrencyConversion"/> to find and use multi-leg currency conversions
    /// </summary>
    public class SecurityCurrencyConversion : ICurrencyConversion
    {
        /// <summary>
        /// Class that holds the information of a single step in a multi-leg currency conversion
        /// </summary>
        private class Step
        {
            /// <summary>
            /// The security used in this conversion step
            /// </summary>
            public Security RateSecurity { get; }

            /// <summary>
            /// Whether the price of the security must be inverted in the conversion
            /// </summary>
            public bool Inverted { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Step"/> class
            /// </summary>
            /// <param name="rateSecurity">The security to use in this currency conversion step</param>
            /// <param name="inverted">Whether the price of the security should be inverted in the conversion</param>
            public Step(Security rateSecurity, bool inverted)
            {
                RateSecurity = rateSecurity;
                Inverted = inverted;
            }
        }

        private readonly List<Step> _steps;

        /// <summary>
        /// The currency this conversion converts from
        /// </summary>
        public string SourceCurrency { get; }

        /// <summary>
        /// The currency this conversion converts to
        /// </summary>
        public string DestinationCurrency { get; }

        /// <summary>
        /// The current conversion rate
        /// </summary>
        public decimal ConversionRate { get; private set; }

        /// <summary>
        /// The securities which the conversion rate is based on
        /// </summary>
        public IEnumerable<Security> ConversionRateSecurities => _steps.Select(step => step.RateSecurity);

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityCurrencyConversion"/> class.
        /// This constructor is intentionally private as only <see cref="LinearSearch"/> is supposed to create it.
        /// </summary>
        /// <param name="sourceCurrency">The currency this conversion converts from</param>
        /// <param name="destinationCurrency">The currency this conversion converts to</param>
        /// <param name="steps">The steps between sourceCurrency and destinationCurrency</param>
        private SecurityCurrencyConversion(string sourceCurrency, string destinationCurrency, List<Step> steps)
        {
            SourceCurrency = sourceCurrency;
            DestinationCurrency = destinationCurrency;

            _steps = steps;
        }

        /// <summary>
        /// Updates the internal conversion rate based on the latest data, and returns the new conversion rate
        /// </summary>
        /// <returns>The new conversion rate</returns>
        public decimal Update()
        {
            var newConversionRate = 0m;
            var stepWithDataFound = false;

            _steps.ForEach(step =>
            {
                var lastData = step.RateSecurity.GetLastData();
                if (lastData == null)
                {
                    return;
                }

                var price = lastData.Price;
                if (price == 0m)
                {
                    return;
                }

                if (!stepWithDataFound)
                {
                    newConversionRate = 1m;
                    stepWithDataFound = true;
                }

                if (step.Inverted)
                {
                    newConversionRate /= price;
                }
                else
                {
                    newConversionRate *= price;
                }
            });

            ConversionRate = newConversionRate;
            return ConversionRate;
        }

        /// <summary>
        /// Finds a conversion between two currencies by looking through all available 1 and 2-leg options
        /// </summary>
        /// <param name="sourceCurrency">The currency to convert from</param>
        /// <param name="destinationCurrency">The currency to convert to</param>
        /// <param name="existingSecurities">The securities which are already added to the algorithm</param>
        /// <param name="potentialSymbols">The symbols to consider, may overlap with existingSecurities</param>
        /// <param name="makeNewSecurity">The function to call when a symbol becomes part of the conversion, must return the security that will provide price data about the symbol</param>
        /// <returns>A new <see cref="SecurityCurrencyConversion"/> instance representing the conversion from sourceCurrency to destinationCurrency</returns>
        /// <exception cref="ArgumentException">Thrown when no conversion from sourceCurrency to destinationCurrency can be found</exception>
        public static SecurityCurrencyConversion LinearSearch(
            string sourceCurrency,
            string destinationCurrency,
            IList<Security> existingSecurities,
            IEnumerable<Symbol> potentialSymbols,
            Func<Symbol, Security> makeNewSecurity)
        {
            var allSymbols = existingSecurities.Select(sec => sec.Symbol).Concat(potentialSymbols)
                .Where(x => x.SecurityType == SecurityType.Crypto || x.Value.Length == 6)
                .ToList();

            var securitiesBySymbol = existingSecurities.Aggregate(new Dictionary<Symbol, Security>(),
                (mapping, security) =>
                {
                    if (!mapping.ContainsKey(security.Symbol))
                    {
                        mapping[security.Symbol] = security;
                    }

                    return mapping;
                });

            // Search for 1 leg conversions
            foreach (var symbol in allSymbols)
            {
                if (!symbol.PairContainsCurrency(sourceCurrency))
                {
                    continue;
                }

                if (symbol.CurrencyPairDual(sourceCurrency) != destinationCurrency)
                {
                    continue;
                }

                var steps = new List<Step>(1);

                var inverted = symbol.ComparePair(sourceCurrency, destinationCurrency) ==
                    CurrencyPairUtil.Match.InverseMatch;

                Security existingSecurity;
                if (securitiesBySymbol.TryGetValue(symbol, out existingSecurity))
                {
                    steps.Add(new Step(existingSecurity, inverted));
                }
                else
                {
                    steps.Add(new Step(makeNewSecurity(symbol), inverted));
                }

                return new SecurityCurrencyConversion(sourceCurrency, destinationCurrency, steps);
            }

            // Search for 2 leg conversions
            foreach (var symbol1 in allSymbols)
            {
                if (!symbol1.PairContainsCurrency(sourceCurrency))
                {
                    continue;
                }

                var middleCurrency = symbol1.CurrencyPairDual(sourceCurrency);

                foreach (var symbol2 in allSymbols)
                {
                    if (!symbol2.PairContainsCurrency(middleCurrency))
                    {
                        continue;
                    }

                    if (symbol2.CurrencyPairDual(middleCurrency) != destinationCurrency)
                    {
                        continue;
                    }

                    var steps = new List<Step>(2);

                    string baseCurrency;
                    string quoteCurrency;

                    CurrencyPairUtil.DecomposeCurrencyPair(symbol1, out baseCurrency, out quoteCurrency);

                    // Step 1
                    Security existingSecurity1;
                    if (securitiesBySymbol.TryGetValue(symbol1, out existingSecurity1))
                    {
                        steps.Add(new Step(existingSecurity1, sourceCurrency == quoteCurrency));
                    }
                    else
                    {
                        steps.Add(new Step(makeNewSecurity(symbol1), sourceCurrency == quoteCurrency));
                    }

                    CurrencyPairUtil.DecomposeCurrencyPair(symbol2, out baseCurrency, out quoteCurrency);

                    // Step 2
                    Security existingSecurity2;
                    if (securitiesBySymbol.TryGetValue(symbol2, out existingSecurity2))
                    {
                        steps.Add(new Step(existingSecurity2, middleCurrency == quoteCurrency));
                    }
                    else
                    {
                        steps.Add(new Step(makeNewSecurity(symbol2), middleCurrency == quoteCurrency));
                    }

                    return new SecurityCurrencyConversion(sourceCurrency, destinationCurrency, steps);
                }
            }

            throw new ArgumentException(
                $"No conversion path found between source currency {sourceCurrency} and destination currency {destinationCurrency}");
        }
    }
}
