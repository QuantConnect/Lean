
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

        private decimal _conversionRate;
        private bool _conversionRateNeedsUpdate;

        /// <summary>
        /// Event fired when the conversion rate is updated
        /// </summary>
        public event EventHandler<decimal> ConversionRateUpdated;

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
        public decimal ConversionRate
        {
            get
            {
                if (_conversionRateNeedsUpdate)
                {
                    var newConversionRate = 1m;
                    var stepWithoutDataFound = false;

                    _steps.ForEach(step =>
                    {
                        if (stepWithoutDataFound)
                        {
                            return;
                        }

                        var lastData = step.RateSecurity.GetLastData();
                        if (lastData == null || lastData.Price == 0m)
                        {
                            newConversionRate = 0m;
                            stepWithoutDataFound = true;
                            return;
                        }

                        if (step.Inverted)
                        {
                            newConversionRate /= lastData.Price;
                        }
                        else
                        {
                            newConversionRate *= lastData.Price;
                        }
                    });

                    _conversionRateNeedsUpdate = false;
                    _conversionRate = newConversionRate;
                    ConversionRateUpdated?.Invoke(this, _conversionRate);
                }

                return _conversionRate;
            }
            set
            {
                if (_conversionRate != value)
                {
                    // only update if there was actually one
                    _conversionRate = value;
                    _conversionRateNeedsUpdate = false;
                    ConversionRateUpdated?.Invoke(this, _conversionRate);

                }
            }
        }

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
        /// Signals an updates to the internal conversion rate based on the latest data.
        /// It will set the conversion rate as potentially outdated so it gets re-calculated.
        /// </summary>
        public void Update()
        {
            _conversionRateNeedsUpdate = true;
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
                .Where(CurrencyPairUtil.IsDecomposable)
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
            foreach (var potentialConversionRateSymbol in allSymbols)
            {
                var leg1Match = potentialConversionRateSymbol.ComparePair(sourceCurrency, destinationCurrency);
                if (leg1Match == CurrencyPairUtil.Match.NoMatch)
                {
                    continue;
                }
                var inverted = leg1Match == CurrencyPairUtil.Match.InverseMatch;

                return new SecurityCurrencyConversion(sourceCurrency, destinationCurrency, new List<Step>(1)
                {
                    CreateStep(potentialConversionRateSymbol, inverted, securitiesBySymbol, makeNewSecurity)
                });
            }

            // Search for 2 leg conversions
            foreach (var potentialConversionRateSymbol1 in allSymbols)
            {
                var middleCurrency = potentialConversionRateSymbol1.CurrencyPairDual(sourceCurrency);
                if (middleCurrency == null)
                {
                    continue;
                }

                foreach (var potentialConversionRateSymbol2 in allSymbols)
                {
                    var leg2Match = potentialConversionRateSymbol2.ComparePair(middleCurrency, destinationCurrency);
                    if (leg2Match == CurrencyPairUtil.Match.NoMatch)
                    {
                        continue;
                    }
                    var secondStepInverted = leg2Match == CurrencyPairUtil.Match.InverseMatch;

                    var steps = new List<Step>(2);

                    // Step 1
                    string baseCurrency;
                    string quoteCurrency;

                    CurrencyPairUtil.DecomposeCurrencyPair(
                        potentialConversionRateSymbol1,
                        out baseCurrency,
                        out quoteCurrency);

                    steps.Add(CreateStep(potentialConversionRateSymbol1,
                        sourceCurrency == quoteCurrency,
                        securitiesBySymbol,
                        makeNewSecurity));

                    // Step 2
                    steps.Add(CreateStep(potentialConversionRateSymbol2,
                        secondStepInverted,
                        securitiesBySymbol,
                        makeNewSecurity));

                    return new SecurityCurrencyConversion(sourceCurrency, destinationCurrency, steps);
                }
            }

            throw new ArgumentException(
                $"No conversion path found between source currency {sourceCurrency} and destination currency {destinationCurrency}");
        }

        /// <summary>
        /// Creates a new step
        /// </summary>
        /// <param name="symbol">The symbol of the step</param>
        /// <param name="inverted">Whether the step is inverted or not</param>
        /// <param name="existingSecurities">The existing securities, which are preferred over creating new ones</param>
        /// <param name="makeNewSecurity">The function to call when a new security must be created</param>
        private static Step CreateStep(
            Symbol symbol,
            bool inverted,
            IDictionary<Symbol, Security> existingSecurities,
            Func<Symbol, Security> makeNewSecurity)
        {
            Security security;
            if (existingSecurities.TryGetValue(symbol, out security))
            {
                return new Step(security, inverted);
            }

            return new Step(makeNewSecurity(symbol), inverted);
        }
    }
}
